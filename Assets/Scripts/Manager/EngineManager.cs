using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Threading.Tasks;

public class EngineManager : MonoBehaviour
{
    private IChessEngine engine;
    private Coroutine pending;
    private CancellationTokenSource cancellation;
    private Task<EngineSearchResult> worker;
    [SerializeField, Range(1, 8)] private int maximumDepth = 6;
    [SerializeField, Min(0.01f)] private float thinkTimeSeconds = 1f;
    public bool IsThinking => pending != null;
    public string LastError { get; private set; }
    public EngineSearchResult LastSearch { get; private set; }
    private void Awake() { engine = new SimpleChessEngine(); }

    public void EngineMove()
    {
        var manager = GameManager.Instance;
        if (!isActiveAndEnabled || pending != null || manager == null || manager.Session == null || !manager.Session.CanEngineMove) return;
        LastError = null;
        LastSearch = null;
        cancellation = new CancellationTokenSource();
        pending = StartCoroutine(AITurn(manager, manager.Session, manager.Session.Revision, cancellation));
    }

    public void CancelPendingMove()
    {
        if (pending != null) StopCoroutine(pending);
        pending = null;
        var source = cancellation;
        var task = worker;
        cancellation = null;
        worker = null;
        LastError = null;
        LastSearch = null;
        if (source == null) return;
        source.Cancel();
        if (task == null) source.Dispose();
        else task.ContinueWith(done => { var ignored = done.Exception; source.Dispose(); },
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void OnDisable() { CancelPendingMove(); }

    private IEnumerator AITurn(GameManager manager, GameSession session, int revision, CancellationTokenSource source)
    {
        yield return new WaitForSecondsRealtime(0.1f);
        if (manager == null || manager.Session != session || session.Revision != revision || !session.CanEngineMove)
        {
            CancelPendingMove();
            yield break;
        }
        // Only detached rule data crosses the worker boundary; Unity stays on the main thread.
        var snapshot = session.State.Board.Copy();
        var side = session.State.CurrentPlayer;
        var searchEngine = engine;
        var token = source.Token;
        int depth = maximumDepth;
        var budget = TimeSpan.FromSeconds(thinkTimeSeconds);
        var task = Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            if (searchEngine is IBudgetedChessEngine bounded)
                return bounded.FindBestMove(snapshot, side, depth, budget, token);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var move = searchEngine.GetBestMove(snapshot, side, 3);
            token.ThrowIfCancellationRequested();
            return new EngineSearchResult(move, 3, 0, clock.Elapsed, false);
        }, token);
        worker = task;
        while (!task.IsCompleted) yield return null;
        pending = null;
        worker = null;
        cancellation = null;
        source.Dispose();
        if (manager == null || manager.Session != session || session.Revision != revision || !session.CanEngineMove)
        { var ignored = task.Exception; yield break; }
        if (task.IsFaulted)
        {
            LastError = "AI search failed";
            // This failure is recoverable through Retry; do not trigger Editor Error Pause.
            Debug.LogWarning("AI search failed: " + task.Exception.GetBaseException());
            yield break;
        }
        if (task.IsCanceled) { LastError = "AI search canceled"; yield break; }
        LastSearch = task.Result;
        if (LastSearch == null || !manager.ApplyEngineMove(LastSearch.BestMove, session, revision))
            LastError = "AI returned no legal move";
    }
}
