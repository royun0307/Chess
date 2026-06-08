using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class ChessRuleTests
{
    private static Position P(int row, int col)
    {
        return new Position(row, col);
    }

    private static void InvokeCheckForGameOver(GameState state)
    {
        MethodInfo method = typeof(GameState).GetMethod(
            "CheckForGameOver",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        Assert.NotNull(method, "GameState.CheckForGameOver() private 메서드를 찾지 못했습니다.");
        method.Invoke(state, null);
    }

    [Test]
    public void InitialBoard_WhiteHas20LegalMoves()
    {
        Board board = Board.Initial();
        GameState state = new GameState(PlayerColor.White, board);

        int whileMoveCount = state.AllLegalMovesFor(PlayerColor.White).Count();

        Assert.AreEqual(20, whileMoveCount);
    }

    [Test]
    public void EnPassant_RemovesCapturedPawn()
    {
        Board board = new Board();

        board[P(7, 4)] = new King(PlayerColor.White);
        board[P(0, 4)] = new King(PlayerColor.Black);

        board[P(3, 4)] = new Pawn(PlayerColor.White); // e5에 white pawn 위치
        board[P(3, 5)] = new Pawn(PlayerColor.Black); // f5에 black pawn 위치

        board.SetPawnSkipPosition(PlayerColor.Black, P(2, 5));

        Move move = new Enpassant(P(3, 4), P(2, 5));

        Assert.IsTrue(move.IsLegal(board));

        move.Execute(board);

        Assert.IsNull(board[P(3, 5)], "앙파상으로 잡힌 검은 폰이 제거되어야 합니다.");
        Assert.IsNull(board[P(3, 4)], "기존 위치는 비어야 합니다.");
        Assert.NotNull(board[P(2, 5)], "백 폰이 앙파상 도착 위치로 이동해야 합니다.");
        Assert.AreEqual(PieceType.Pawn, board[P(2, 5)].Type);
        Assert.AreEqual(PlayerColor.White, board[P(2, 5)].Color);
    }

    [Test]
    public void CastlingKingSide_IsLegalWhenPathClear()
    {
        Board board = Board.Initial();

        board[P(7, 5)] = null; // f1 white bishop 제거
        board[P(7, 6)] = null; // g1 white knight 제거

        GameState state = new GameState(PlayerColor.White, board);

        bool hasKingSideCastle = state
            .LegalMoveForPiece(P(7, 4))
            .Any(move =>
                move.Type == MoveType.CastleKS &&
                move.ToPos.row == 7 &&
                move.ToPos.column == 6
            );

        Assert.IsTrue(hasKingSideCastle);
    }

    [Test]
    public void Promotion_ChangesPawnToQueen()
    {
        Board board = new Board();

        board[P(7, 4)] = new King(PlayerColor.White);
        board[P(0, 4)] = new King(PlayerColor.Black);

        board[P(1, 0)] = new Pawn(PlayerColor.White); // a7 white pawn 위치

        Move move = new PawnPromotion(P(1, 0), P(0, 0), PieceType.Queen);

        move.Execute(board);

        Assert.IsNull(board[P(1, 0)], "승급 전 폰 위치는 비어야 합니다.");
        Assert.NotNull(board[P(0, 0)], "승급 위치에 새 기물이 있어야 합니다.");
        Assert.AreEqual(PieceType.Queen, board[P(0, 0)].Type);
        Assert.AreEqual(PlayerColor.White, board[P(0, 0)].Color);
        Assert.IsTrue(board[P(0, 0)].hasMoved);
    }

    [Test]
    public void Check_IsDetectedWhenKingAttacked()
    {
        Board board = new Board();

        board[P(7, 4)] = new King(PlayerColor.White); // e1 white king 위치
        board[P(0, 0)] = new King(PlayerColor.Black);
        board[P(0, 4)] = new Rook(PlayerColor.Black); // e8 black rook 위치

        Assert.IsTrue(board.IsInCheck(PlayerColor.White));
    }

    [Test]
    public void Checkmate_IsDetected()
    {
        Board board = new Board();

        board[P(0, 0)] = new King(PlayerColor.Black);  // a8 black king 위치
        board[P(1, 1)] = new Queen(PlayerColor.White); // b7 white queen 위치
        board[P(2, 2)] = new King(PlayerColor.White);  // c6 white king 위치

        GameState state = new GameState(PlayerColor.Black, board);

        InvokeCheckForGameOver(state);

        Assert.IsTrue(state.IsGameOver());
        Assert.NotNull(state.Result);
        Assert.AreEqual(PlayerColor.White, state.Result.Winner);
        Assert.AreEqual(EndReason.Checkmate, state.Result.EndReason);
    }

    [Test]
    public void Stalemate_IsDetected()
    {
        Board board = new Board();

        board[P(0, 0)] = new King(PlayerColor.Black);  // a8 black king 위치
        board[P(1, 2)] = new Queen(PlayerColor.White); // c7 white queen 위치
        board[P(2, 2)] = new King(PlayerColor.White);  // c6 white king 위치

        GameState state = new GameState(PlayerColor.Black, board);

        Assert.IsFalse(board.IsInCheck(PlayerColor.Black));

        InvokeCheckForGameOver(state);

        Assert.IsTrue(state.IsGameOver());
        Assert.NotNull(state.Result);
        Assert.AreEqual(PlayerColor.None, state.Result.Winner);
        Assert.AreEqual(EndReason.Stalemate, state.Result.EndReason);
    }
}
