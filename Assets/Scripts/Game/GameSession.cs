using System.Linq;

// A session owns player requests and invalidates callbacks from earlier turns/games.
public sealed class GameSession
{
    public GameState State { get; private set; }
    public int Revision { get; private set; }
    public bool PromotionPending { get; private set; }
    public bool Paused { get; private set; }
    private Position promotionFrom, promotionTo;
    public bool CanHumanMove => !Paused && !PromotionPending && !State.IsGameOver() && State.CurrentPlayer == PlayerColor.White;
    public bool CanEngineMove => !Paused && !PromotionPending && !State.IsGameOver() && State.CurrentPlayer == PlayerColor.Black;

    public GameSession(GameState state) { State = state; }

    public void Restart(GameState state)
    {
        State = state;
        Revision++;
        PromotionPending = false;
        Paused = false;
        promotionFrom = promotionTo = null;
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
        State.MakeMove(move);
        Revision++;
        return true;
    }

    private static bool Inside(Position p) => p != null && (uint)p.row < 8 && (uint)p.column < 8;
}
