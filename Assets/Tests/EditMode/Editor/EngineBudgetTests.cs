using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

public class EngineBudgetTests
{
    [Test]
    public void ZeroBudget_ReturnsLegalFallbackWithoutChangingBoard()
    {
        var board = Board.Initial();
        var piece = board[6, 0];
        var result = new SimpleChessEngine().FindBestMove(board, PlayerColor.White, 6, TimeSpan.Zero, CancellationToken.None);
        Assert.IsTrue(result.TimedOut);
        Assert.AreEqual(0, result.CompletedDepth);
        Assert.IsTrue(new GameState(PlayerColor.White, board).AllLegalMovesFor(PlayerColor.White).Any(m => m.FromPos == result.BestMove.FromPos && m.ToPos == result.BestMove.ToPos));
        Assert.AreSame(piece, board[6, 0]);
        Assert.AreEqual(20, new GameState(PlayerColor.White, board).AllLegalMovesFor(PlayerColor.White).Count());
    }

    [Test]
    public void CompletedDepth_MatchesSynchronousSearch()
    {
        var engine = new SimpleChessEngine();
        var expected = engine.GetBestMove(Board.Initial(), PlayerColor.White, 1);
        var result = engine.FindBestMove(Board.Initial(), PlayerColor.White, 1, TimeSpan.FromSeconds(10), CancellationToken.None);
        Assert.AreEqual(1, result.CompletedDepth);
        Assert.IsFalse(result.TimedOut);
        Assert.AreEqual(expected.FromPos, result.BestMove.FromPos);
        Assert.AreEqual(expected.ToPos, result.BestMove.ToPos);
        Assert.Greater(result.Nodes, 0);
    }

    [Test]
    public void SmallBudget_StopsDeepSearch()
    {
        var result = new SimpleChessEngine().FindBestMove(Board.Initial(), PlayerColor.White, 64, TimeSpan.FromMilliseconds(30), CancellationToken.None);
        Assert.IsTrue(result.TimedOut);
        Assert.NotNull(result.BestMove);
        Assert.Less(result.Elapsed.TotalSeconds, 3);
        TestContext.WriteLine($"Depth={result.CompletedDepth}, nodes={result.Nodes}, elapsed={result.Elapsed.TotalMilliseconds}ms");
    }

    [Test]
    public void Cancellation_DoesNotReturnPartialMove()
    {
        using (var source = new CancellationTokenSource())
        {
            source.Cancel();
            Assert.Throws<OperationCanceledException>(() => new SimpleChessEngine().FindBestMove(Board.Initial(), PlayerColor.White, 6, TimeSpan.FromSeconds(1), source.Token));
        }
    }

    [Test]
    public void RunningSearch_ObservesCancellation()
    {
        using (var source = new CancellationTokenSource())
        {
            var task = Task.Run(() => new SimpleChessEngine().FindBestMove(Board.Initial(), PlayerColor.White, 64, TimeSpan.FromSeconds(10), source.Token));
            source.CancelAfter(30);
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }
    }
}
