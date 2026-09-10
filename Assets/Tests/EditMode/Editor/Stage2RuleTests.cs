using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class Stage2RuleTests
{
    private static Position P(string square) => new Position(8 - (square[1] - '0'), square[0] - 'a');
    private static int Counter(GameState state) => (int)typeof(GameState).GetField("no_capture_or_pawn_moves", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(state);
    private static Dictionary<string, int> History(GameState state) => (Dictionary<string, int>)typeof(GameState).GetField("state_history", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(state);

    // Test-only FEN reader: explicit rights, pawn home ranks and EP target, independent of StateString.
    private static GameState Fen(string fen)
    {
        var fields = fen.Split(' ');
        var board = new Board();
        int row = 0, col = 0;
        foreach (char c in fields[0])
        {
            if (c == '/') { row++; col = 0; continue; }
            if (char.IsDigit(c)) { col += c - '0'; continue; }
            var color = char.IsUpper(c) ? PlayerColor.White : PlayerColor.Black;
            Piece piece = char.ToLowerInvariant(c) switch
            {
                'p' => new Pawn(color), 'n' => new Knight(color), 'b' => new Bishop(color),
                'r' => new Rook(color), 'q' => new Queen(color), 'k' => new King(color),
                _ => throw new ArgumentException(fen)
            };
            piece.hasMoved = piece.Type != PieceType.Pawn || row != (color == PlayerColor.White ? 6 : 1);
            board[row, col++] = piece;
        }
        foreach (char right in fields[2].Where(c => c != '-'))
        {
            int rank = char.IsUpper(right) ? 7 : 0;
            board[rank, 4].hasMoved = false;
            board[rank, char.ToLowerInvariant(right) == 'k' ? 7 : 0].hasMoved = false;
        }
        var player = fields[1] == "w" ? PlayerColor.White : PlayerColor.Black;
        if (fields[3] != "-") board.SetPawnSkipPosition(player.Opponent(), P(fields[3]));
        return new GameState(player, board);
    }

    private static Move Find(GameState state, string from, string to, PieceType promotion = PieceType.Queen) =>
        state.LegalMoveForPiece(P(from)).Single(m => m.ToPos == P(to) && (!(m is PawnPromotion p) || p.GetPromotionPieceType() == promotion));
    private static void Play(GameState state, string from, string to, bool training = false)
    {
        var move = Find(state, from, to);
        if (training) state.MakeMoveForTraining(move); else state.MakeMove(move);
    }

    [TestCase(false)] [TestCase(true)]
    public void EnPassantExpiresAfterOneReply(bool training)
    {
        var s = Fen("4k3/3p4/8/4P3/8/8/8/4K3 b - -");
        Play(s, "d7", "d5", training);
        Assert.IsTrue(s.LegalMoveForPiece(P("e5")).Any(m => m.Type == MoveType.EnPassant));
        Play(s, "e1", "f1", training);
        Assert.IsNull(s.Board.GetPawnSkipPosition(PlayerColor.Black));
        Play(s, "e8", "f8", training);
        Assert.IsFalse(s.LegalMoveForPiece(P("e5")).Any(m => m.Type == MoveType.EnPassant));
        Assert.IsNull(s.Board.GetPawnSkipPosition(PlayerColor.Black));
    }

    [TestCase("4k3/8/8/3pP3/8/8/8/4K3 w - d6", "e5", "d6", "d5", false)]
    [TestCase("4k3/8/8/8/3Pp3/8/8/4K3 b - d3", "e4", "d3", "d4", false)]
    [TestCase("4k3/8/8/3pP3/8/8/8/K3R3 w - d6", "e5", "d6", "d5", true)]
    public void EnPassantCaptureAndDiscoveredCheck(string fen, string from, string to, string captured, bool check)
    {
        var s = Fen(fen);
        var move = Find(s, from, to);
        Assert.AreEqual(MoveType.EnPassant, move.Type);
        s.MakeMove(move);
        Assert.IsNull(s.Board[P(captured)]);
        Assert.IsNull(s.Board[P(from)]);
        Assert.AreEqual(PieceType.Pawn, s.Board[P(to)].Type);
        Assert.AreEqual(check, s.Board.IsInCheck(s.CurrentPlayer));
        Assert.AreEqual(0, Counter(s));
    }

    [TestCase("k3r3/8/8/3pP3/8/8/8/4K3 w - d6", "e5")]
    [TestCase("k7/8/8/r4pPK/8/8/8/8 w - f6", "g5")]
    [TestCase("4k3/8/8/8/3Pp3/8/8/K3R3 b - d3", "e4")]
    public void EnPassantCannotExposeOwnKing(string fen, string from)
    {
        var s = Fen(fen);
        Assert.IsFalse(s.LegalMoveForPiece(P(from)).Any(m => m.Type == MoveType.EnPassant));
        Assert.IsFalse(s.Board.CanCaptureEnPassant(s.CurrentPlayer));
        Assert.IsTrue(new StateString(s.CurrentPlayer, s.Board).ToString().EndsWith(" -"));
    }

    [TestCase(PlayerColor.White, MoveType.CastleKS)] [TestCase(PlayerColor.White, MoveType.CastleQS)]
    [TestCase(PlayerColor.Black, MoveType.CastleKS)] [TestCase(PlayerColor.Black, MoveType.CastleQS)]
    public void CastleBothColorsAndSides(PlayerColor color, MoveType type)
    {
        var s = Fen("r3k2r/8/8/8/8/8/8/R3K2R " + (color == PlayerColor.White ? "w" : "b") + " KQkq -");
        var move = s.AllLegalMovesFor(color).Single(m => m.Type == type);
        int row = color == PlayerColor.White ? 7 : 0;
        int kingCol = type == MoveType.CastleKS ? 6 : 2, rookCol = type == MoveType.CastleKS ? 5 : 3;
        s.MakeMove(move);
        Assert.AreEqual(PieceType.King, s.Board[row, kingCol].Type);
        Assert.AreEqual(PieceType.Rook, s.Board[row, rookCol].Type);
        Assert.AreEqual(color, s.Board[row, rookCol].Color);
        Assert.IsTrue(s.Board[row, kingCol].hasMoved && s.Board[row, rookCol].hasMoved);
        Assert.IsNull(s.Board[row, 4]);
        Assert.IsNull(s.Board[row, type == MoveType.CastleKS ? 7 : 0]);
        Assert.IsFalse(s.Board.CastleRightKS(color) || s.Board.CastleRightQS(color));
        Assert.AreEqual(1, Counter(s));
    }

    [TestCase(PlayerColor.White, false)] [TestCase(PlayerColor.White, true)]
    [TestCase(PlayerColor.Black, false)] [TestCase(PlayerColor.Black, true)]
    public void CastleForbiddenConditions(PlayerColor color, bool queenSide)
    {
        int row = color == PlayerColor.White ? 7 : 0, other = 7 - row;
        int rookCol = queenSide ? 0 : 7, transit = queenSide ? 3 : 5, destination = queenSide ? 2 : 6;
        var type = queenSide ? MoveType.CastleQS : MoveType.CastleKS;
        foreach (string condition in new[] { "kingMoved", "rookMoved", "missing", "enemyRook", "blocked", "checked", "transit", "destination", "wrongRank", "wrongFile" })
        {
            var b = new Board();
            var king = new King(color);
            var rook = new Rook(color);
            var from = new Position(row, 4);
            b[from] = king; b[row, rookCol] = rook; b[other, 4] = new King(color.Opponent());
            switch (condition)
            {
                case "kingMoved": king.hasMoved = true; break;
                case "rookMoved": rook.hasMoved = true; break;
                case "missing": b[row, rookCol] = null; break;
                case "enemyRook": b[row, rookCol] = new Rook(color.Opponent()); break;
                case "blocked": b[row, queenSide ? 1 : 5] = new Knight(color); break;
                case "checked": b[other, 4] = new Rook(color.Opponent()); b[other, 0] = new King(color.Opponent()); break;
                case "transit": b[other, transit] = new Rook(color.Opponent()); break;
                case "destination": b[other, destination] = new Rook(color.Opponent()); break;
                case "wrongRank": b[from] = null; b[row, rookCol] = null; from = new Position(row == 7 ? 6 : 1, 4); b[from] = king; b[from.row, rookCol] = rook; break;
                case "wrongFile": b[from] = null; from = new Position(row, 3); b[from] = king; break;
            }
            var s = new GameState(color, b);
            Assert.IsFalse(s.LegalMoveForPiece(from).Any(m => m.Type == type), condition);
        }
    }

    [TestCase(PlayerColor.White)] [TestCase(PlayerColor.Black)]
    public void QueenSideCastleAllowsAttackedRookAndBFile(PlayerColor color)
    {
        int row = color == PlayerColor.White ? 7 : 0, other = 7 - row;
        var b = new Board();
        b[row, 4] = new King(color); b[row, 0] = new Rook(color);
        b[other, 7] = new King(color.Opponent());
        b[other, 0] = new Rook(color.Opponent()); b[other, 1] = new Rook(color.Opponent());
        var s = new GameState(color, b);
        Assert.IsTrue(s.LegalMoveForPiece(new Position(row, 4)).Any(m => m.Type == MoveType.CastleQS));
    }

    [TestCase("e1", "f1")] [TestCase("h1", "h2")]
    public void MovingKingOrRookBackDoesNotRestoreRightsOrRepeatInitialPosition(string from, string to)
    {
        var s = Fen("4k3/8/8/8/8/8/8/R3K2R w KQ -");
        string initial = new StateString(s.CurrentPlayer, s.Board).ToString();
        Play(s, from, to); Play(s, "e8", "f8"); Play(s, to, from); Play(s, "f8", "e8");
        Assert.IsFalse(s.Board.CastleRightKS(PlayerColor.White));
        Assert.AreNotEqual(initial, new StateString(s.CurrentPlayer, s.Board).ToString());
        Assert.AreEqual(1, History(s)[initial]);
    }

    [TestCase(PlayerColor.White)] [TestCase(PlayerColor.Black)]
    public void PawnDoubleStepRequiresHomeRankAndClearPath(PlayerColor color)
    {
        int home = color == PlayerColor.White ? 6 : 1, dir = color == PlayerColor.White ? -1 : 1;
        foreach (int row in new[] { home, home + dir, home + 2 * dir })
        {
            var b = new Board(); var p = new Pawn(color); b[row, 3] = p;
            Assert.AreEqual(row == home, p.GetMoves(new Position(row, 3), b).Any(m => m.Type == MoveType.DoublePawn));
            b[row + dir, 3] = new Knight(color);
            Assert.IsFalse(p.GetMoves(new Position(row, 3), b).Any(m => m.Type == MoveType.DoublePawn));
        }
    }

    [TestCase(PlayerColor.White, false)] [TestCase(PlayerColor.White, true)]
    [TestCase(PlayerColor.Black, false)] [TestCase(PlayerColor.Black, true)]
    public void AllFourPromotionsIncludingCaptures(PlayerColor color, bool capture)
    {
        var b = new Board(); int row = color == PlayerColor.White ? 1 : 6, end = color == PlayerColor.White ? 0 : 7;
        b[7, 7] = new King(PlayerColor.White); b[0, 7] = new King(PlayerColor.Black);
        b[row, 1] = new Pawn(color);
        if (capture) b[end, 0] = new Rook(color.Opponent());
        var s = new GameState(color, b); var to = new Position(end, capture ? 0 : 1);
        var moves = s.LegalMoveForPiece(new Position(row, 1)).Where(m => m.ToPos == to).Cast<PawnPromotion>().ToArray();
        CollectionAssert.AreEquivalent(new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight }, moves.Select(m => m.GetPromotionPieceType()));
        foreach (var move in moves)
        {
            var copy = b.Copy(); Assert.IsTrue(move.Execute(copy));
            Assert.AreEqual(move.GetPromotionPieceType(), copy[to].Type);
            Assert.AreEqual(color, copy[to].Color); Assert.IsTrue(copy[to].hasMoved);
            Assert.IsNull(copy[row, 1]);
        }
    }

    [Test]
    public void ExpiredSkipSquareOccupiedByOwnKingDoesNotCreateFalseCheck()
    {
        var s = Fen("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -");
        Play(s, "g2", "g4"); Play(s, "h4", "g3");
        Assert.IsFalse(s.IsInCheck(PlayerColor.White));
        Assert.AreEqual(14, s.AllLegalMovesFor(PlayerColor.White).Count());
        // Board.Copy/Move.Execute를 사용하는 탐색 경로에서도 오래된 표시에 영향받지 않는다.
        s.Board.SetPawnSkipPosition(PlayerColor.White, P("g3"));
        Assert.IsFalse(s.IsInCheck(PlayerColor.White));
        Assert.AreEqual(14, s.AllLegalMovesFor(PlayerColor.White).Count());
        Assert.IsFalse(new Enpassant(P("f4"), P("g3")).IsLegal(s.Board));
    }

    [TestCase("missing")] [TestCase("ownPawn")] [TestCase("notPawn")] [TestCase("occupied")]
    public void EnPassantRequiresEmptyTargetAndAdjacentEnemyPawn(string condition)
    {
        var s = Fen("4k3/8/8/3pP3/8/8/8/4K3 w - d6");
        if (condition == "missing") s.Board[P("d5")] = null;
        if (condition == "ownPawn") s.Board[P("d5")] = new Pawn(PlayerColor.White);
        if (condition == "notPawn") s.Board[P("d5")] = new Knight(PlayerColor.Black);
        if (condition == "occupied") s.Board[P("d6")] = new Knight(PlayerColor.White);
        Assert.IsFalse(s.LegalMoveForPiece(P("e5")).Any(m => m.Type == MoveType.EnPassant));
        Assert.IsFalse(s.Board.CanCaptureEnPassant(PlayerColor.White));
    }

    [Test]
    public void OccupiedOldSkipSquareStillAllowsOrdinaryPawnCapture()
    {
        var s = Fen("4k3/8/3n4/3pP3/8/8/8/4K3 w - d6");
        Assert.AreEqual(MoveType.Normal, Find(s, "e5", "d6").Type);
        Assert.IsFalse(s.Board.CanCaptureEnPassant(PlayerColor.White));
    }

    [TestCase(PlayerColor.White)] [TestCase(PlayerColor.Black)]
    public void CastlingRightsRequireMatchingKingAndRookColors(PlayerColor color)
    {
        var b = Board.Initial(); int row = color == PlayerColor.White ? 7 : 0;
        b[row, 0] = new Rook(color.Opponent()); b[row, 7] = new Rook(color.Opponent());
        Assert.IsFalse(b.CastleRightKS(color) || b.CastleRightQS(color));
        b[row, 0] = new Rook(color); b[row, 7] = new Rook(color); b[row, 4] = new King(color.Opponent());
        Assert.IsFalse(b.CastleRightKS(color) || b.CastleRightQS(color));
    }

    [Test]
    public void CopyPreservesAllPiecesAndSeparatesMutableState()
    {
        var b = Board.Initial(); b[7, 0].hasMoved = true;
        b.SetPawnSkipPosition(PlayerColor.Black, P("d6"));
        b.SetPawnSkipPosition(PlayerColor.White, P("e3"));
        var c = b.Copy();
        foreach (var pos in b.PiecePositions())
        {
            Assert.AreNotSame(b[pos], c[pos]); Assert.AreEqual(b[pos].Type, c[pos].Type);
            Assert.AreEqual(b[pos].Color, c[pos].Color); Assert.AreEqual(b[pos].hasMoved, c[pos].hasMoved);
            c[pos].hasMoved = !b[pos].hasMoved;
            Assert.AreNotEqual(b[pos].hasMoved, c[pos].hasMoved);
        }
        Assert.AreEqual(P("d6"), c.GetPawnSkipPosition(PlayerColor.Black));
        c.SetPawnSkipPosition(PlayerColor.Black, null); b.SetPawnSkipPosition(PlayerColor.White, null);
        Assert.AreEqual(P("d6"), b.GetPawnSkipPosition(PlayerColor.Black));
        Assert.AreEqual(P("e3"), c.GetPawnSkipPosition(PlayerColor.White));
        c[0, 0] = null; b[7, 7] = null;
        Assert.NotNull(b[0, 0]); Assert.NotNull(c[7, 7]);
    }

    [Test]
    public void ThreefoldCountsInitialPositionAndSideToMove()
    {
        var s = new GameState(PlayerColor.White, Board.Initial());
        for (int cycle = 0; cycle < 2; cycle++)
        {
            Play(s, "g1", "f3"); Play(s, "g8", "f6"); Play(s, "f3", "g1"); Play(s, "f6", "g8");
            if (cycle == 0) Assert.IsFalse(s.IsGameOver());
        }
        Assert.AreEqual(EndReason.ThreefoldRepetition, s.Result.EndReason);
        Assert.AreEqual(8, Counter(s));
        Assert.AreNotEqual(new StateString(PlayerColor.White, s.Board).ToString(), new StateString(PlayerColor.Black, s.Board).ToString());
    }

    [Test]
    public void RepetitionKeyIncludesOnlyLegalEnPassantAndCastlingRights()
    {
        var s = Fen("4k3/8/8/3pP3/8/8/8/4K3 w - d6");
        string withEp = new StateString(s.CurrentPlayer, s.Board).ToString();
        Assert.IsTrue(withEp.EndsWith(" d6"));
        s.Board.SetPawnSkipPosition(PlayerColor.Black, null);
        Assert.AreNotEqual(withEp, new StateString(s.CurrentPlayer, s.Board).ToString());
        var b = Board.Initial(); string rights = new StateString(PlayerColor.White, b).ToString();
        b[7, 7].hasMoved = true;
        Assert.AreNotEqual(rights, new StateString(PlayerColor.White, b).ToString());
        var noEp = Fen("4k3/8/8/3p4/8/8/8/4K3 w - d6");
        Assert.IsTrue(new StateString(noEp.CurrentPlayer, noEp.Board).ToString().EndsWith(" -"));
    }

    [TestCase(false)] [TestCase(true)]
    public void CountersResetOnPawnMoveAndCapture(bool training)
    {
        var s = Fen("4k3/1p6/8/8/8/8/P7/1R2K3 w - -");
        Play(s, "e1", "f1", training); Play(s, "e8", "f8", training);
        Assert.AreEqual(2, Counter(s));
        Play(s, "a2", "a4", training); Assert.AreEqual(0, Counter(s));
        Play(s, "f8", "g8", training); Play(s, "b1", "b3", training); Play(s, "g8", "h8", training);
        Assert.AreEqual(3, Counter(s));
        Play(s, "b3", "b7", training); Assert.AreEqual(0, Counter(s));
        Assert.AreEqual(training ? 0 : 1, History(s).Count);
    }

    [Test]
    public void FiftyMoveThresholdIsOneHundredHalfMoves()
    {
        var s = Fen("4k3/8/8/8/8/8/8/R3K3 w - -");
        typeof(GameState).GetField("no_capture_or_pawn_moves", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(s, 98);
        Play(s, "a1", "a2"); Assert.IsFalse(s.IsGameOver()); Assert.AreEqual(99, Counter(s));
        Play(s, "e8", "f8"); Assert.AreEqual(100, Counter(s));
        Assert.AreEqual(EndReason.FiftyMoveRule, s.Result.EndReason);
    }

    [TestCase("r3k2r/8/8/8/8/8/8/R3K2R w KQkq -", "e1", "g1")]
    [TestCase("4k3/8/8/3pP3/8/8/8/4K3 w - d6", "e5", "d6")]
    [TestCase("4k3/1P6/8/8/8/8/8/4K3 w - -", "b7", "b8")]
    [TestCase("4k3/8/8/8/8/8/P7/4K3 w - -", "a2", "a4")]
    public void TrainingAndGameShareBoardTurnAndCounterTransition(string fen, string from, string to)
    {
        var normal = Fen(fen); var training = Fen(fen);
        Play(normal, from, to); Play(training, from, to, true);
        Assert.AreEqual(new StateString(normal.CurrentPlayer, normal.Board).ToString(), new StateString(training.CurrentPlayer, training.Board).ToString());
        Assert.AreEqual(normal.CurrentPlayer, training.CurrentPlayer); Assert.AreEqual(Counter(normal), Counter(training));
        Assert.IsNull(training.Result);
    }

    [Test]
    public void TrainingStillSkipsRepetitionAndTerminalAdjudication()
    {
        var s = new GameState(PlayerColor.White, Board.Initial());
        for (int i = 0; i < 2; i++)
        { Play(s, "g1", "f3", true); Play(s, "g8", "f6", true); Play(s, "f3", "g1", true); Play(s, "f6", "g8", true); }
        Assert.IsNull(s.Result); Assert.AreEqual(1, History(s).Values.Single()); Assert.AreEqual(8, Counter(s));
        var mate = Fen("k7/1PK5/8/8/8/8/8/8 w - -");
        Play(mate, "b7", "b8", true); Assert.IsNull(mate.Result);
        Assert.IsTrue(mate.IsInCheck(PlayerColor.Black)); Assert.IsFalse(mate.HasAnyLegalMove(PlayerColor.Black));
    }

    // Published perft positions: initial, Kiwipete, rook/pawn ending, promotion/castling position.
    // Perft counts legal move trees, ignoring repetition/50-move/insufficient-material adjudication.
    [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq -", 3, 8902L)]
    [TestCase("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -", 3, 97862L)]
    [TestCase("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - -", 4, 43238L)]
    [TestCase("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq -", 3, 9467L)]
    public void KnownPositionPerft(string fen, int depth, long expected)
    {
        var s = Fen(fen); string before = new StateString(s.CurrentPlayer, s.Board).ToString();
        Assert.AreEqual(expected, Perft(s.Board, s.CurrentPlayer, depth));
        Assert.AreEqual(before, new StateString(s.CurrentPlayer, s.Board).ToString());
    }

    private static long Perft(Board board, PlayerColor color, int depth)
    {
        if (depth == 0) return 1;
        var state = new GameState(color, board); long nodes = 0;
        foreach (var move in state.AllLegalMovesFor(color))
        {
            var child = new GameState(color, board.Copy());
            child.MakeMoveForTraining(move);
            nodes += Perft(child.Board, child.CurrentPlayer, depth - 1);
        }
        return nodes;
    }
}
