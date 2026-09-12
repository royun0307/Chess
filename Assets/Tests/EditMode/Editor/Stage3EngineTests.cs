using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class Stage3EngineTests
{
    private readonly SimpleChessEngine engine = new SimpleChessEngine();
    private static Position P(string s) => new Position(8 - (s[1] - '0'), s[0] - 'a');
    private static Piece PieceOf(PieceType type, PlayerColor color) => type switch
    {
        PieceType.Pawn => new Pawn(color), PieceType.Bishop => new Bishop(color),
        PieceType.Knight => new Knight(color), PieceType.Rook => new Rook(color),
        PieceType.Queen => new Queen(color), PieceType.King => new King(color),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
    private int Call(string name, params object[] args) => (int)typeof(SimpleChessEngine)
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(engine, args);
    private int Search(Board b, PlayerColor color, int depth = 0) => Call("Search", b, depth, -1000000, 1000000, color);
    private int Q(Board b, PlayerColor color, int depth, int alpha = -1000000, int beta = 1000000) =>
        Call("Quiescence", b, alpha, beta, color, depth);
    private static Board BoardOf(params string[] placements)
    {
        var board = new Board();
        foreach (string entry in placements)
        {
            var color = char.IsUpper(entry[0]) ? PlayerColor.White : PlayerColor.Black;
            var type = char.ToLowerInvariant(entry[0]) switch
            { 'p' => PieceType.Pawn, 'b' => PieceType.Bishop, 'n' => PieceType.Knight,
              'r' => PieceType.Rook, 'q' => PieceType.Queen, 'k' => PieceType.King,
              _ => throw new ArgumentException(entry) };
            board[P(entry.Substring(1))] = PieceOf(type, color);
            board[P(entry.Substring(1))].hasMoved = true;
        }
        return board;
    }
    private static Board Mirror(Board b)
    {
        var copy = new Board();
        foreach (var pos in b.PiecePositions())
        {
            var p = b[pos]; var other = PieceOf(p.Type, p.Color.Opponent());
            other.hasMoved = p.hasMoved; copy[7 - pos.row, pos.column] = other;
        }
        foreach (var color in new[] { PlayerColor.White, PlayerColor.Black })
        {
            var skip = b.GetPawnSkipPosition(color);
            if (skip != null) copy.SetPawnSkipPosition(color.Opponent(), new Position(7 - skip.row, skip.column));
        }
        return copy;
    }

    [TestCase(PieceType.Pawn, 100)] [TestCase(PieceType.Knight, 320)]
    [TestCase(PieceType.Bishop, 330)] [TestCase(PieceType.Rook, 500)]
    [TestCase(PieceType.Queen, 900)] [TestCase(PieceType.King, 100000)]
    public void MaterialValuesMatchNamedPiecesForBothColors(PieceType type, int expected)
    {
        var b = new Board(); b[P("d4")] = PieceOf(type, PlayerColor.White);
        Assert.AreEqual(expected, Call("EvaluateMaterial", b));
        b[P("d4")] = PieceOf(type, PlayerColor.Black);
        Assert.AreEqual(-expected, Call("EvaluateMaterial", b));
    }

    [TestCase(PieceType.Pawn, "a2", 5)] [TestCase(PieceType.Pawn, "a7", 50)]
    [TestCase(PieceType.King, "g1", 30)] [TestCase(PieceType.King, "g8", -40)]
    [TestCase(PieceType.Rook, "a7", 5)] [TestCase(PieceType.Knight, "d4", 20)]
    public void PieceSquareTablesUseWhiteFirstRankAtTableRowZero(PieceType type, string square, int expected)
    {
        var pos = P(square);
        Assert.AreEqual(expected, Call("GetPST", PieceOf(type, PlayerColor.White), pos.row, pos.column));
        Assert.AreEqual(expected, Call("GetPST", PieceOf(type, PlayerColor.Black), 7 - pos.row, pos.column));
    }

    [Test]
    public void PassedPawnBonusIncreasesTowardPromotionAndMirrors()
    {
        var nearHome = BoardOf("Pa2"); var advanced = BoardOf("Pa6");
        Assert.AreEqual(10, Call("EvaluatePawnStructure", nearHome)); // -15 isolated +20 +1*5
        Assert.AreEqual(30, Call("EvaluatePawnStructure", advanced)); // -15 isolated +20 +5*5
        Assert.AreEqual(-30, Call("EvaluatePawnStructure", Mirror(advanced)));
    }

    [Test]
    public void PassedPawnChecksEnemyPawnsAheadOnAdjacentFiles()
    {
        // a4/b6 stop each other; b2 is behind white and already passed it.
        var ahead = BoardOf("Pa4", "pb6"); var behind = BoardOf("Pa4", "pb2");
        Assert.AreEqual(0, Call("EvaluatePawnStructure", ahead));
        Assert.AreEqual(-15, Call("EvaluatePawnStructure", behind));
        Assert.AreEqual(15, Call("EvaluatePawnStructure", Mirror(behind)));
    }

    [Test]
    public void EvaluationIsZeroInitiallyAndNegatesOnColorRankMirror()
    {
        Assert.AreEqual(0, Call("EvaluateStatic", Board.Initial()));
        Assert.AreEqual(0, Call("Evaluate", Board.Initial()));
        var b = BoardOf("Kg1", "Ra1", "Bc4", "Ne5", "Pa6", "Pb2", "kg8", "rh8", "nf6", "pd5");
        Assert.AreEqual(-Call("EvaluateStatic", b), Call("EvaluateStatic", Mirror(b)));
        Assert.AreEqual(-Call("Evaluate", b), Call("Evaluate", Mirror(b)));
    }

    [Test]
    public void CaptureOrderingUsesPawnVictimAndAttackerValues()
    {
        var b = BoardOf("Ke1", "ke8", "Pe4", "qd5");
        var capture = new NormalMove(P("e4"), P("d5"));
        Assert.AreEqual(18900, Call("ScoreMoveMVVLVA", b, capture));
        Assert.AreEqual(18900, Call("ScoreTactical", b, capture));
        b[P("d5")] = new Pawn(PlayerColor.Black);
        var ep = new Enpassant(P("e5"), P("d6")); b[P("e5")] = new Pawn(PlayerColor.White);
        Assert.AreEqual(10900, Call("ScoreMoveMVVLVA", b, ep));
        Assert.AreEqual(10900, Call("ScoreTactical", b, ep));
    }

    [TestCase(false)] [TestCase(true)]
    public void TerminalPositionsAreRecognizedAtEveryDepthAndBeforeWindowCutoff(bool mirror)
    {
        var mate = BoardOf("ka8", "Qb7", "Kc6");
        var stale = BoardOf("ka8", "Qc7", "Kc6");
        var color = mirror ? PlayerColor.White : PlayerColor.Black;
        if (mirror) { mate = Mirror(mate); stale = Mirror(stale); }
        Assert.IsTrue(mate.IsInCheck(color)); Assert.IsFalse(stale.IsInCheck(color));
        Assert.IsFalse(new GameState(color, mate).HasAnyLegalMove(color));
        Assert.IsFalse(new GameState(color, stale).HasAnyLegalMove(color));
        foreach (int depth in new[] { -1, 0, 1 })
        {
            Assert.AreEqual(mirror ? -999999 : 999999, Search(mate, color, depth));
            Assert.AreEqual(0, Search(stale, color, depth));
            Assert.AreEqual(mirror ? -999999 : 999999, Q(mate, color, depth));
            Assert.AreEqual(0, Q(stale, color, depth));
        }
        Assert.AreEqual(mirror ? -999999 : 999999, Q(mate, color, 0, 900000, 900001));
        Assert.AreEqual(0, Q(stale, color, 8, -900001, -900000));
        Assert.IsNull(engine.GetBestMove(mate, color, 1));
        Assert.IsNull(engine.GetBestMove(stale, color, 1));
    }

    [TestCase(false)] [TestCase(true)]
    public void InsufficientMaterialIsDrawAtSearchAndQuiescenceLeaves(bool mirror)
    {
        var b = BoardOf("Ka1", "Bc4", "kh8"); if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        Assert.AreEqual(0, Search(b, color, 0)); Assert.AreEqual(0, Search(b, color, 2));
        Assert.AreEqual(0, Q(b, color, 0)); Assert.AreEqual(0, Q(b, color, 8));
    }

    [TestCase(false, 0)] [TestCase(true, 0)] [TestCase(false, 2)] [TestCase(true, 2)]
    public void InCheckQuiescenceMustPlayQuietEvasionEvenAtLimit(bool mirror, int depth)
    {
        // Only legal move is Ka1-b1: rook a8 checks a1 and king c3 controls b2.
        var b = BoardOf("Ka1", "Ph2", "kc3", "ra8");
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        Assert.IsTrue(b.IsInCheck(color));
        var legal = new GameState(color, b).AllLegalMovesFor(color).ToArray();
        Assert.AreEqual(1, legal.Length); Assert.AreEqual(MoveType.Normal, legal[0].Type);
        Assert.IsNull(b[legal[0].ToPos]);
        var child = new GameState(color, b.Copy()); child.MakeMoveForTraining(legal[0]);
        // Independent one-evasion oracle: this child has no tactical continuation.
        var replies = child.AllLegalMovesFor(child.CurrentPlayer).ToArray();
        Assert.IsFalse(replies.Any(m => !child.Board.IsEmpty(m.ToPos) || m is PawnPromotion || m is Enpassant));
        int expected = Call("EvaluateStatic", child.Board);
        Assert.AreEqual(expected, Q(b, color, depth));
        int stand = Call("EvaluateStatic", b);
        // A window that would incorrectly accept stand-pat must still search the evasion.
        int narrow = mirror ? Q(b, color, depth, stand, stand + 1) : Q(b, color, depth, stand - 1, stand);
        Assert.AreEqual(expected, narrow);
    }

    [TestCase(false)] [TestCase(true)]
    public void QuiescenceFindsQuietInterposition(bool mirror)
    {
        // Ra2 is the sole legal evasion of the a-file rook check.
        var b = BoardOf("Ka1", "Rg2", "kc3", "ra8", "bh7");
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        var moves = new GameState(color, b).AllLegalMovesFor(color).ToArray();
        Assert.IsTrue(b.IsInCheck(color)); Assert.AreEqual(1, moves.Length);
        Assert.AreEqual(PieceType.Rook, b[moves[0].FromPos].Type);
        Assert.IsNull(b[moves[0].ToPos]);
        var child = new GameState(color, b.Copy()); child.MakeMoveForTraining(moves[0]);
        Assert.AreEqual(Call("EvaluateStatic", child.Board), Q(b, color, 0));
    }

    [TestCase(false)] [TestCase(true)]
    public void BestMoveFindsMateInOneAndLeavesInputUntouched(bool mirror)
    {
        var b = BoardOf("Kc6", "Qb6", "ka8");
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        string before = new StateString(color, b).ToString();
        var pieces = b.PiecePositions().Select(p => (p, b[p], b[p].hasMoved)).ToArray();
        var move = engine.GetBestMove(b, color, 1);
        Assert.NotNull(move);
        var next = new GameState(color, b.Copy()); next.MakeMove(move);
        Assert.AreEqual(EndReason.Checkmate, next.Result?.EndReason); Assert.AreEqual(color, next.Result.Winner);
        Assert.AreEqual(before, new StateString(color, b).ToString());
        foreach (var (p, piece, moved) in pieces) { Assert.AreSame(piece, b[p]); Assert.AreEqual(moved, b[p].hasMoved); }
    }

    [TestCase(false)] [TestCase(true)]
    public void BestMoveCapturesFreeQueenAtDepthOne(bool mirror)
    {
        var b = BoardOf("Ka1", "Rd1", "kh8", "qd5");
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        var move = engine.GetBestMove(b, color, 1);
        Assert.AreEqual(PieceType.Queen, b[move.ToPos]?.Type);
        Assert.AreEqual(PieceType.Rook, b[move.FromPos].Type);
    }

    [Test]
    public void PromotionOrderingValuesQueenAboveRookBishopAndKnight()
    {
        var b = BoardOf("Ka1", "kh8", "Pb7");
        foreach (var pair in new[] { (PieceType.Queen, 900), (PieceType.Rook, 500), (PieceType.Bishop, 330), (PieceType.Knight, 320) })
        {
            var move = new PawnPromotion(P("b7"), P("b8"), pair.Item1);
            Assert.AreEqual(8000 + pair.Item2, Call("ScoreMoveMVVLVA", b, move));
            Assert.AreEqual(20000 + pair.Item2, Call("ScoreTactical", b, move));
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void QuiescenceRejectsLosingCaptureAfterRecapture(bool mirror)
    {
        var b = BoardOf("Ka1", "Rd1", "kh8", "pd5", "rd8");
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        // Rxd5? Rxd5 loses a rook for a pawn; staying put is better in this quiet node.
        Assert.AreEqual(Call("EvaluateStatic", b), Q(b, color, 2));
    }

    [TestCase(false)] [TestCase(true)]
    public void QuiescenceIncludesNonCapturePromotion(bool mirror)
    {
        var b = BoardOf("Ka1", "kh6", "Pb7");
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        int sign = mirror ? -1 : 1;
        Assert.Greater(sign * Q(b, color, 1), sign * Call("EvaluateStatic", b) + 500);
    }

    [TestCase(false)] [TestCase(true)]
    public void QuiescenceIncludesEnPassantCapture(bool mirror)
    {
        var b = BoardOf("Ka1", "kh8", "Pe5", "pd5");
        b.SetPawnSkipPosition(PlayerColor.Black, P("d6"));
        if (mirror) b = Mirror(b);
        var color = mirror ? PlayerColor.Black : PlayerColor.White;
        int sign = mirror ? -1 : 1;
        Assert.Greater(sign * Q(b, color, 1), sign * Call("EvaluateStatic", b) + 50);
    }

    [Test]
    public void EngineSuccessorExpiresEnPassantButPreservesNewDoubleStep()
    {
        // Covers the shared successor helper used at root, Search and Quiescence.
        var b = BoardOf("Ke1", "ke8", "Pe5", "pd5", "Pa2");
        b[P("a2")].hasMoved = false; b.SetPawnSkipPosition(PlayerColor.Black, P("d6"));
        var helper = typeof(SimpleChessEngine).GetMethod("NextBoard", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(helper, "Engine successors must use the shared GameState transition.");
        var next = (Board)helper.Invoke(null, new object[] { b, new DoublePawn(P("a2"), P("a4")), PlayerColor.White });
        Assert.IsNull(next.GetPawnSkipPosition(PlayerColor.Black));
        Assert.AreEqual(P("a3"), next.GetPawnSkipPosition(PlayerColor.White));
        Assert.AreEqual(P("d6"), b.GetPawnSkipPosition(PlayerColor.Black));
        Assert.IsNull(b.GetPawnSkipPosition(PlayerColor.White));
    }
}
