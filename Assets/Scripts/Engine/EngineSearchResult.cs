using System;
using System.Threading;

public interface IBudgetedChessEngine : IChessEngine
{
    EngineSearchResult FindBestMove(Board board, PlayerColor side, int maxDepth, TimeSpan timeLimit, CancellationToken token);
}

public sealed class EngineSearchResult
{
    public Move BestMove { get; }
    public int CompletedDepth { get; }
    public long Nodes { get; }
    public TimeSpan Elapsed { get; }
    public bool TimedOut { get; }
    public int? EvaluationCp { get; }
    public EngineSearchResult(Move move, int depth, long nodes, TimeSpan elapsed, bool timedOut, int? evaluationCp = null)
    { BestMove = move; CompletedDepth = depth; Nodes = nodes; Elapsed = elapsed; TimedOut = timedOut; EvaluationCp = evaluationCp; }
}
