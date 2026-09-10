using System.Linq;
using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class GameSessionTests
{
    private static Position P(int r, int c) => new Position(r, c);
    private static GameSession Initial() => new GameSession(new GameState(PlayerColor.White, Board.Initial()));
    private static Move First(GameSession s) => s.State.AllLegalMovesFor(s.State.CurrentPlayer).First();
    private static GameSession Promotion(bool mate = false)
    {
        var board = new Board();
        board[P(0, 0)] = new King(PlayerColor.Black);
        board[P(mate ? 1 : 7, mate ? 2 : 4)] = new King(PlayerColor.White);
        board[P(1, 1)] = new Pawn(PlayerColor.White);
        return new GameSession(new GameState(PlayerColor.White, board));
    }

    [Test] public void PromotionWaitsForChoiceAndAppliesOnlyOnce()
    {
        var s = Promotion();
        Assert.IsTrue(s.BeginPromotion(P(1,1), P(0,1)));
        int revision = s.Revision;
        Assert.IsFalse(s.CanHumanMove);
        Assert.IsFalse(s.CanEngineMove);
        Assert.AreEqual(PlayerColor.White, s.State.CurrentPlayer);
        Assert.AreEqual(PieceType.Pawn, s.State.Board[P(1,1)].Type);
        Assert.IsTrue(s.CompletePromotion(PieceType.Queen, revision));
        Assert.AreEqual(PieceType.Queen, s.State.Board[P(0,1)].Type);
        Assert.AreEqual(PlayerColor.Black, s.State.CurrentPlayer);
        Assert.IsTrue(s.CanEngineMove);
        Assert.IsFalse(s.CompletePromotion(PieceType.Rook, revision));
    }

    [Test] public void PromotionMateBlocksBothPlayers()
    {
        var s = Promotion(true);
        Assert.IsTrue(s.BeginPromotion(P(1,1), P(0,1)));
        Assert.IsTrue(s.CompletePromotion(PieceType.Queen, s.Revision));
        Assert.AreEqual(EndReason.Checkmate, s.State.Result.EndReason);
        Assert.IsFalse(s.CanHumanMove);
        Assert.IsFalse(s.CanEngineMove);
        Assert.IsFalse(s.TryEngineMove(null, s.Revision));
    }

    [Test] public void RestartInvalidatesPromotionCallback()
    {
        var s = Promotion();
        s.BeginPromotion(P(1,1), P(0,1));
        int old = s.Revision;
        s.Restart(new GameState(PlayerColor.White, Board.Initial()));
        Assert.IsFalse(s.CompletePromotion(PieceType.Queen, old));
        Assert.IsTrue(s.CanHumanMove);
        Assert.AreEqual(32, s.State.Board.PiecePositions().Count());
    }

    [Test] public void RestartInvalidatesEngineEvenAfterNewGameReachesBlackTurn()
    {
        var s = Initial();
        s.TryHumanMove(First(s));
        var oldMove = First(s);
        int old = s.Revision;
        s.Restart(new GameState(PlayerColor.White, Board.Initial()));
        s.TryHumanMove(First(s));
        Assert.IsFalse(s.TryEngineMove(oldMove, old));
        Assert.AreEqual(PlayerColor.Black, s.State.CurrentPlayer);
    }

    [Test] public void HumanCannotMoveDuringEngineTurnAndEngineResultIsSingleUse()
    {
        var s = Initial();
        Assert.IsTrue(s.TryHumanMove(First(s)));
        var move = First(s);
        int revision = s.Revision;
        Assert.IsFalse(s.TryHumanMove(move));
        Assert.IsTrue(s.TryEngineMove(move, revision));
        Assert.IsFalse(s.TryEngineMove(move, revision));
        Assert.IsTrue(s.CanHumanMove);
    }

    [Test] public void InvalidRequestsDoNotChangePosition()
    {
        var s = Initial();
        var before = new StateString(s.State.CurrentPlayer, s.State.Board).ToString();
        Assert.IsFalse(s.TryHumanMove(null));
        Assert.IsFalse(s.TryHumanMove(new NormalMove(P(-1,0), P(4,0))));
        Assert.IsFalse(s.TryHumanMove(new NormalMove(P(6,0), P(3,0))));
        Assert.IsFalse(s.TryHumanMove(new NormalMove(P(1,0), P(2,0))));
        Assert.AreEqual(before, new StateString(s.State.CurrentPlayer, s.State.Board).ToString());
    }

    [Test] public void InvalidPromotionChoiceLeavesChoicePending()
    {
        var s = Promotion();
        s.BeginPromotion(P(1,1), P(0,1));
        Assert.IsFalse(s.CompletePromotion(PieceType.King, s.Revision));
        Assert.IsTrue(s.PromotionPending);
        Assert.IsTrue(s.CompletePromotion(PieceType.Knight, s.Revision));
        Assert.AreEqual(PieceType.Knight, s.State.Board[P(0,1)].Type);
    }

    [Test] public void PauseInvalidatesPendingEngineAndResumeAllowsNewRequest()
    {
        var s = Initial();
        s.TryHumanMove(First(s));
        var move = First(s);
        int old = s.Revision;
        Assert.IsTrue(s.SetPaused(true));
        Assert.IsFalse(s.CanEngineMove);
        Assert.IsFalse(s.TryEngineMove(move, old));
        Assert.IsTrue(s.SetPaused(false));
        Assert.IsFalse(s.TryEngineMove(move, old));
        Assert.IsTrue(s.TryEngineMove(move, s.Revision));
    }

    [Test] public void PauseCannotDismissPromotion()
    {
        var s = Promotion();
        s.BeginPromotion(P(1,1), P(0,1));
        Assert.IsFalse(s.SetPaused(true));
        Assert.IsTrue(s.PromotionPending);
    }

    [Test] public void StalemateMoveBlocksFurtherRequests()
    {
        var board = new Board();
        board[P(0,0)] = new King(PlayerColor.Black);
        board[P(2,2)] = new King(PlayerColor.White);
        board[P(2,1)] = new Queen(PlayerColor.White);
        var s = new GameSession(new GameState(PlayerColor.White, board));
        Assert.IsTrue(s.TryHumanMove(new NormalMove(P(2,1), P(1,2))));
        Assert.AreEqual(EndReason.Stalemate, s.State.Result.EndReason);
        Assert.IsFalse(s.TryHumanMove(FirstWhite(board)));
        Assert.IsFalse(s.TryEngineMove(null, s.Revision));
    }
    private static Move FirstWhite(Board board) => new NormalMove(P(2,2), P(3,2));

    [Test] public void PromotionButtonDoesNotDismissResultAndConsumesCallback()
    {
        var uiObject = new GameObject("UI test");
        var promotionObject = new GameObject("Promotion test");
        try
        {
            var ui = uiObject.AddComponent<UIManager>();
            var promotion = promotionObject.AddComponent<PromotionUI>();
            ui.promotionUI = promotion;
            int calls = 0;
            promotion.select_promotion += type => { calls++; ui.ChangeState(UIState.Result); };
            var click = typeof(PromotionUI).GetMethod("OnClickQuennPromotion", BindingFlags.NonPublic | BindingFlags.Instance);
            click.Invoke(promotion, null);
            click.Invoke(promotion, null);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(UIState.Result, typeof(UIManager).GetField("currentState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui));
        }
        finally { Object.DestroyImmediate(promotionObject); Object.DestroyImmediate(uiObject); }
    }
}
