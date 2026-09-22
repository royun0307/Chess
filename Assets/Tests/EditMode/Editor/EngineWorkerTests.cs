using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class EngineWorkerTests
{
    private sealed class RetryEngine : IChessEngine
    {
        public bool Fail = true;
        public Move GetBestMove(Board board, PlayerColor side, int depth)
        {
            if (Fail) throw new InvalidOperationException("Expected worker failure");
            return new GameState(side, board).AllLegalMovesFor(side).First();
        }
    }

    [UnityTest]
    public IEnumerator MainScene_FailedSearchCanBeRetried()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
        var manager = GameManager.Instance;
        manager.StartMatch(5, 3, PlayerColor.White);
        var fake = new RetryEngine();
        typeof(EngineManager).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager.engine, fake);
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("InvalidOperationException: Expected worker failure"));
        manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());
        for (float end = Time.realtimeSinceStartup + 3; manager.engine.IsThinking && Time.realtimeSinceStartup < end;) yield return null;
        yield return null;
        Assert.NotNull(manager.engine.LastError);
        for (float end = Time.realtimeSinceStartup + 1; !manager.GetComponent<EngineStatusUI>().RetryVisible && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsTrue(manager.GetComponent<EngineStatusUI>().RetryVisible,
            $"text={manager.GetComponent<EngineStatusUI>().DisplayText}, canEngine={manager.Session.CanEngineMove}, error={manager.engine.LastError}, active={manager.GetComponent<EngineStatusUI>().isActiveAndEnabled}, editorPaused={UnityEditor.EditorApplication.isPaused}");
        fake.Fail = false;
        manager.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == "Retry AI").onClick.Invoke();
        for (float end = Time.realtimeSinceStartup + 3; manager.engine.IsThinking && Time.realtimeSinceStartup < end;) yield return null;
        yield return null;
        Assert.IsNull(manager.engine.LastError);
        Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        for (float end = Time.realtimeSinceStartup + 1; manager.GetComponent<EngineStatusUI>().RetryVisible && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsFalse(manager.GetComponent<EngineStatusUI>().RetryVisible);
        yield return new ExitPlayMode();
    }

    private sealed class BlockingEngine : IBudgetedChessEngine
    {
        public int ThreadId;
        public readonly ManualResetEventSlim Started = new ManualResetEventSlim();
        public readonly ManualResetEventSlim Stopped = new ManualResetEventSlim();
        public Move GetBestMove(Board board, PlayerColor side, int depth) => throw new NotSupportedException();
        public EngineSearchResult FindBestMove(Board board, PlayerColor side, int depth, TimeSpan limit, CancellationToken token)
        {
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            Started.Set();
            try { token.WaitHandle.WaitOne(5000); token.ThrowIfCancellationRequested(); return null; }
            finally { Stopped.Set(); }
        }
    }

    [UnityTest]
    public IEnumerator MainScene_SearchRunsOffMainThreadAndPauseCancelsIt()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
        var manager = GameManager.Instance;
        manager.StartMatch(5, 3, PlayerColor.White);
        var fake = new BlockingEngine();
        typeof(EngineManager).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager.engine, fake);
        manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());
        for (float end = Time.realtimeSinceStartup + 3; !fake.Started.IsSet && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsTrue(fake.Started.IsSet);
        Assert.AreNotEqual(Thread.CurrentThread.ManagedThreadId, fake.ThreadId);
        for (int i = 0; i < 5; i++) yield return null;
        Assert.IsTrue(manager.engine.IsThinking);
        Assert.AreEqual("AI thinking...", manager.GetComponent<EngineStatusUI>().DisplayText);
        int revision = manager.Session.Revision;
        manager.SetPaused(true);
        for (float end = Time.realtimeSinceStartup + 3; !fake.Stopped.IsSet && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsTrue(fake.Stopped.IsSet);
        Assert.IsFalse(manager.engine.IsThinking);
        Assert.AreEqual(revision + 1, manager.Session.Revision);
        Assert.AreEqual(PlayerColor.Black, manager.state.CurrentPlayer);
        manager.RestartGame();
        yield return null;
        Assert.AreEqual("Your turn (White)", manager.GetComponent<EngineStatusUI>().DisplayText);
        yield return new ExitPlayMode();
    }
}
