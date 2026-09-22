using System.Linq;

// A session owns player requests and invalidates callbacks from earlier turns/games.
public sealed class GameSession
{
    public GameState State { get; private set; }
    public int Revision { get; private set; }
    public bool PromotionPending { get; private set; }
    public bool Paused { get; private set; }
    public PlayerColor HumanColor { get; private set; } = PlayerColor.White;
    public double WhiteSeconds { get; private set; }
    public double BlackSeconds { get; private set; }
    public double IncrementSeconds { get; private set; }
    public bool ClockEnabled { get; private set; }
    private Position promotionFrom, promotionTo;
    private readonly System.Collections.Generic.List<PieceType> capturedByWhite = new();
    private readonly System.Collections.Generic.List<PieceType> capturedByBlack = new();
    public System.Collections.Generic.IReadOnlyList<PieceType> CapturedBy(PlayerColor player) =>
        player == PlayerColor.White ? capturedByWhite : capturedByBlack;
    public bool CanHumanMove => !Paused && !PromotionPending && !State.IsGameOver() && State.CurrentPlayer == HumanColor;
    public bool CanEngineMove => !Paused && !PromotionPending && !State.IsGameOver() && State.CurrentPlayer == HumanColor.Opponent();

    public void Configure(PlayerColor humanColor, double seconds, double increment)
    {
        if (humanColor != PlayerColor.White && humanColor != PlayerColor.Black) throw new System.ArgumentOutOfRangeException(nameof(humanColor));
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 ||
            double.IsNaN(increment) || double.IsInfinity(increment) || increment < 0) throw new System.ArgumentOutOfRangeException(nameof(seconds));
        HumanColor = humanColor;
        WhiteSeconds = BlackSeconds = seconds;
        IncrementSeconds = increment;
        ClockEnabled = seconds > 0;
    }

    public double Remaining(PlayerColor color) => color == PlayerColor.White ? WhiteSeconds : BlackSeconds;

    // Promotion selection is part of the player's turn and consumes time.
    public bool AdvanceClock(double seconds)
    {
        if (!ClockEnabled || Paused || State.IsGameOver() || seconds <= 0 || double.IsNaN(seconds)) return false;
        if (State.CurrentPlayer == PlayerColor.White) WhiteSeconds = System.Math.Max(0, WhiteSeconds - seconds);
        else BlackSeconds = System.Math.Max(0, BlackSeconds - seconds);
        if (Remaining(State.CurrentPlayer) > 0) return false;
        State.EndOnTime();
        PromotionPending = false;
        Revision++;
        return true;
    }

    public GameSession(GameState state) { State = state; }

    public void Restart(GameState state)
    {
        State = state;
        Revision++;
        PromotionPending = false;
        Paused = false;
        promotionFrom = promotionTo = null;
        capturedByWhite.Clear();
        capturedByBlack.Clear();
    }

    public bool SetPaused(bool paused)
    {
        if (PromotionPending || State.IsGameOver() || Paused == paused) return false;
        Paused = paused;
        Revision++;
        return true;
    }

    public bool BeginPromotion(Position from, Position to)
    {
        if (!CanHumanMove || !Inside(from) || !Inside(to) ||
            !State.LegalMoveForPiece(from).Any(m => m.Type == MoveType.PawnPromotion && m.ToPos == to)) return false;
        promotionFrom = from;
        promotionTo = to;
        PromotionPending = true;
        Revision++;
        return true;
    }

    public bool CompletePromotion(PieceType type, int revision)
    {
        if (!PromotionPending || revision != Revision || State.IsGameOver()) return false;
        var move = LegalMove(new PawnPromotion(promotionFrom, promotionTo, type));
        if (move == null) return false;
        PromotionPending = false;
        return Apply(move);
    }

    public bool TryHumanMove(Move request)
    {
        if (!CanHumanMove || request == null || request.Type == MoveType.PawnPromotion) return false;
        return Apply(LegalMove(request));
    }

    public bool TryEngineMove(Move request, int revision)
    {
        if (!CanEngineMove || Revision != revision) return false;
        return Apply(LegalMove(request));
    }

    private Move LegalMove(Move request)
    {
        if (request == null || !Inside(request.FromPos) || !Inside(request.ToPos)) return null;
        return State.LegalMoveForPiece(request.FromPos).FirstOrDefault(m =>
            m.ToPos == request.ToPos && m.Type == request.Type &&
            (!(m is PawnPromotion promotion) || request is PawnPromotion requested &&
                promotion.GetPromotionPieceType() == requested.GetPromotionPieceType()));
    }

    private bool Apply(Move move)
    {
        if (move == null) return false;
        var mover = State.CurrentPlayer;
        var captured = move.Type == MoveType.EnPassant
            ? State.Board[new Position(move.FromPos.row, move.ToPos.column)] : State.Board[move.ToPos];
        State.MakeMove(move);
        if (captured != null)
            (mover == PlayerColor.White ? capturedByWhite : capturedByBlack).Add(captured.Type);
        if (ClockEnabled)
        {
            if (mover == PlayerColor.White) WhiteSeconds += IncrementSeconds;
            else BlackSeconds += IncrementSeconds;
        }
        Revision++;
        return true;
    }

    private static bool Inside(Position p) => p != null && (uint)p.row < 8 && (uint)p.column < 8;
}
