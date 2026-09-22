using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Each score belongs to one exact session revision. Analysis never makes a move.
public sealed class PositionAnalysis : MonoBehaviour
{
    private CancellationTokenSource source;
    private Task<EngineSearchResult> task;
    private GameSession session;
    private int revision = -1;
    private int? score;
    private int depth;
    // UI may read after a move/restart but before this component's next Update.
    private bool IsCurrent
    {
        get
        {
            var manager = GameManager.Instance;
            return isActiveAndEnabled && manager != null && !manager.SetupVisible &&
                session != null && manager.Session == session && revision == session.Revision &&
                !session.Paused && !session.State.IsGameOver();
        }
    }
    public int? Score => IsCurrent ? score : null;
    public int Depth => IsCurrent ? depth : 0;
    public bool Busy => task != null;

    private void Update()
    {
        var manager = GameManager.Instance;
        var current = manager?.Session;
        if (current == null || manager.SetupVisible || current.Paused || current.State.IsGameOver())
        {
            Cancel();
            revision = -1;
            score = null;
            depth = 0;
            return;
        }
        if (current != session || revision != current.Revision)
        {
            Cancel();
            session = current;
            revision = current.Revision;
            score = null;
            depth = 0;
            var snapshot = current.State.Board.Copy();
            var side = current.State.CurrentPlayer;
            source = new CancellationTokenSource();
            var token = source.Token;
            task = Task.Run(() => new SimpleChessEngine().FindBestMove(snapshot, side, 4, TimeSpan.FromSeconds(0.35), token), token);
        }
        if (task == null || !task.IsCompleted) return;
        if (task.Status == TaskStatus.RanToCompletion)
        {
            score = task.Result.EvaluationCp;
            depth = task.Result.CompletedDepth;
        }
        else { var ignored = task.Exception; }
        task = null;
        source.Dispose();
        source = null;
    }

    private void Cancel()
    {
        var oldSource = source;
        var oldTask = task;
        source = null;
        task = null;
        if (oldSource == null) return;
        oldSource.Cancel();
        if (oldTask == null) oldSource.Dispose();
        else oldTask.ContinueWith(t => { var ignored = t.Exception; oldSource.Dispose(); }, TaskScheduler.Default);
    }

    private void OnDisable() { Cancel(); revision = -1; score = null; depth = 0; }
}
