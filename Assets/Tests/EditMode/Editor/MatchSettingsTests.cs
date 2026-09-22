using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;

public class MatchSettingsTests
{
    public static void Play(GameSession session, int fromRow, int fromCol, int toRow, int toCol)
    {
        var move = session.State.LegalMoveForPiece(new Position(fromRow, fromCol)).First(m => m.ToPos == new Position(toRow, toCol));
        Assert.IsTrue(session.CanHumanMove ? session.TryHumanMove(move) : session.TryEngineMove(move, session.Revision));
    }

    [Test]
    public void CapturesTrackActualPieces_IncludeEnPassant_AndClearOnRestart()
    {
        var s = new GameSession(new GameState(PlayerColor.White, Board.Initial()));
        Play(s, 6, 4, 4, 4);
        Play(s, 1, 0, 2, 0);
        Play(s, 4, 4, 3, 4);
        Play(s, 1, 3, 3, 3);
        Play(s, 3, 4, 2, 3);
        CollectionAssert.AreEqual(new[] { PieceType.Pawn }, s.CapturedBy(PlayerColor.White));
        Assert.IsEmpty(s.CapturedBy(PlayerColor.Black));
        s.Restart(new GameState(PlayerColor.White, Board.Initial()));
        Assert.IsEmpty(s.CapturedBy(PlayerColor.White));
        Play(s, 6, 4, 4, 4);
        Play(s, 1, 3, 3, 3);
        Play(s, 4, 4, 3, 3);
        CollectionAssert.AreEqual(new[] { PieceType.Pawn }, s.CapturedBy(PlayerColor.White));
        Assert.AreEqual(1, MatchUI.Material(s.State.Board, PlayerColor.White) - MatchUI.Material(s.State.Board, PlayerColor.Black));
    }

    [Test]
    public void BlackPlayer_EngineOpens_ClocksAndIncrementFollowMover()
    {
        var s = new GameSession(new GameState(PlayerColor.White, Board.Initial()));
        s.Configure(PlayerColor.Black, 60, 2);
        Assert.IsTrue(s.CanEngineMove);
        Assert.IsFalse(s.CanHumanMove);
        s.AdvanceClock(4);
        Assert.IsTrue(s.TryEngineMove(s.State.AllLegalMovesFor(PlayerColor.White).First(), s.Revision));
        Assert.AreEqual(58, s.WhiteSeconds);
        Assert.AreEqual(60, s.BlackSeconds);
        Assert.IsTrue(s.CanHumanMove);
        s.AdvanceClock(3);
        Assert.IsTrue(s.TryHumanMove(s.State.AllLegalMovesFor(PlayerColor.Black).First()));
        Assert.AreEqual(59, s.BlackSeconds);
        Assert.IsFalse(s.TryHumanMove(s.State.AllLegalMovesFor(PlayerColor.White).First()));
        Assert.AreEqual(58, s.WhiteSeconds);
    }

    [Test]
    public void PauseFreezesClock_TimeoutRejectsLateMove()
    {
        var s = new GameSession(new GameState(PlayerColor.White, Board.Initial()));
        s.Configure(PlayerColor.White, 10, 2);
        s.SetPaused(true);
        s.AdvanceClock(100);
        Assert.AreEqual(10, s.WhiteSeconds);
        s.SetPaused(false);
        var move = s.State.AllLegalMovesFor(PlayerColor.White).First();
        Assert.IsTrue(s.AdvanceClock(11));
        Assert.AreEqual(0, s.WhiteSeconds);
        Assert.AreEqual(PlayerColor.Black, s.State.Result.Winner);
        Assert.AreEqual(EndReason.Timeout, s.State.Result.EndReason);
        Assert.IsFalse(s.TryHumanMove(move));
    }

    [Test]
    public void PromotionChoiceConsumesTime_ExpiredChoiceCannotApply()
    {
        var b = new Board();
        b[new Position(0, 0)] = new King(PlayerColor.Black);
        b[new Position(7, 4)] = new King(PlayerColor.White);
        b[new Position(1, 1)] = new Pawn(PlayerColor.White);
        var s = new GameSession(new GameState(PlayerColor.White, b));
        s.Configure(PlayerColor.White, 1, 2);
        Assert.IsTrue(s.BeginPromotion(new Position(1, 1), new Position(0, 1)));
        int revision = s.Revision;
        Assert.IsTrue(s.AdvanceClock(1));
        Assert.IsFalse(s.CompletePromotion(PieceType.Queen, revision));
        Assert.AreEqual(PlayerColor.None, s.State.Result.Winner, "Bare king cannot win on time");
    }

    [Test]
    public void MaterialAndEvaluationAreDistinct_CompletedSearchExportsScore()
    {
        Assert.AreEqual(39, MatchUI.Material(Board.Initial(), PlayerColor.White));
        Assert.AreEqual("01:00", MatchUI.ClockText(59.1));
        Assert.AreEqual("00:00", MatchUI.ClockText(0));
        Assert.AreEqual("+1.25", MatchUI.ScoreText(125));
        Assert.AreEqual("-M2", MatchUI.ScoreText(-999996));
        var result = new SimpleChessEngine().FindBestMove(Board.Initial(), PlayerColor.White, 1, TimeSpan.FromSeconds(10), CancellationToken.None);
        Assert.AreEqual(1, result.CompletedDepth);
        Assert.IsTrue(result.EvaluationCp.HasValue);
        var expired = new SimpleChessEngine().FindBestMove(Board.Initial(), PlayerColor.White, 1, TimeSpan.Zero, CancellationToken.None);
        Assert.IsNull(expired.EvaluationCp, "Unfinished depth must not publish a score");
    }
}
