using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class EngineLifecycleTests
{
    [UnitySetUp]
    public IEnumerator OpenScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
        GameManager.Instance.StartMatch(5, 3, PlayerColor.White);
    }

    [UnityTearDown]
    public IEnumerator CloseScene()
    {
        if (GameManager.Instance != null) GameManager.Instance.engine.CancelPendingMove();
        yield return new ExitPlayMode();
    }

    // Gates make cancellation races reproducible without depending on engine speed.
    private sealed class CooperativeEngine : IBudgetedChessEngine
    {
        public readonly ManualResetEventSlim Started = new ManualResetEventSlim();
        public readonly ManualResetEventSlim Canceled = new ManualResetEventSlim();
        public Move GetBestMove(Board board, PlayerColor side, int depth) => throw new NotSupportedException();
        public EngineSearchResult FindBestMove(Board board, PlayerColor side, int depth, TimeSpan budget, CancellationToken token)
        {
            Started.Set();
            if (!token.WaitHandle.WaitOne(10000)) throw new TimeoutException("Cancellation was not requested");
            Canceled.Set();
            token.ThrowIfCancellationRequested();
            return null;
        }
    }

    private sealed class LegacyEngine : IChessEngine
    {
        public readonly ManualResetEventSlim Started = new ManualResetEventSlim();
        public readonly ManualResetEventSlim Release = new ManualResetEventSlim();
        public bool Fail;
        public Move GetBestMove(Board board, PlayerColor side, int depth)
        {
            var move = new GameState(side, board).AllLegalMovesFor(side).First();
            Started.Set();
            if (!Release.Wait(10000)) throw new TimeoutException("Test did not release legacy engine");
            if (Fail) throw new InvalidOperationException("Late failure from canceled legacy engine");
            return move;
        }
    }

    private static void SetEngine(GameManager manager, IChessEngine engine) =>
        typeof(EngineManager).GetField("engine", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager.engine, engine);

    private static Task Worker(GameManager manager) =>
        (Task)typeof(EngineManager).GetField("worker", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager.engine);

    private static IEnumerator Until(Func<bool> condition, string message)
    {
        for (float end = Time.realtimeSinceStartup + 5; !condition() && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsTrue(condition(), message);
    }

    private static void HumanMove(GameManager manager) =>
        manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());

    [UnityTest]
    public IEnumerator Restart_CancelsRunningSearchAndRestoresInitialBoard()
    {
        var manager = GameManager.Instance;
        var fake = new CooperativeEngine();
        SetEngine(manager, fake);
        HumanMove(manager);
        yield return Until(() => fake.Started.IsSet, "Worker did not start");
        var task = Worker(manager);
        int revision = manager.Session.Revision;
        manager.RestartGame();
        yield return Until(() => task.IsCompleted, "Restart did not finish cancellation");
        Assert.IsTrue(fake.Canceled.IsSet);
        Assert.IsTrue(task.IsCanceled);
        Assert.IsFalse(manager.engine.IsThinking);
        Assert.AreEqual(revision + 1, manager.Session.Revision);
        Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        Assert.AreEqual(20, manager.state.AllLegalMovesFor(PlayerColor.White).Count());
        Assert.IsNull(manager.engine.LastSearch);
        Assert.IsNull(manager.engine.LastError);
    }

    [UnityTest]
    public IEnumerator Disable_CancelsRunningSearchAndReenableResumesOnce()
    {
        var manager = GameManager.Instance;
        var fake = new CooperativeEngine();
        SetEngine(manager, fake);
        HumanMove(manager);
        yield return Until(() => fake.Started.IsSet, "Worker did not start");
        var task = Worker(manager);
        int revision = manager.Session.Revision;
        manager.engine.enabled = false;
        yield return Until(() => task.IsCompleted, "Disable did not finish cancellation");
        Assert.IsTrue(fake.Canceled.IsSet);
        Assert.IsTrue(task.IsCanceled);
        Assert.IsFalse(manager.engine.IsThinking);
        Assert.AreEqual(revision, manager.Session.Revision);
        var replacement = new LegacyEngine();
        replacement.Release.Set();
        SetEngine(manager, replacement);
        manager.engine.enabled = true;
        yield return Until(() => manager.state.CurrentPlayer == PlayerColor.White, "Reenabled engine left the game stuck on Black");
        Assert.AreEqual(revision + 1, manager.Session.Revision, "Exactly one reply must be applied");
        Assert.IsNull(manager.engine.LastError);
    }

    [UnityTest]
    public IEnumerator Restart_DiscardsLateLegacyResultWhileNewSearchIsRunning()
    {
        var manager = GameManager.Instance;
        var old = new LegacyEngine();
        var current = new LegacyEngine();
        try
        {
            SetEngine(manager, old);
            HumanMove(manager);
            yield return Until(() => old.Started.IsSet, "Old worker did not start");
            var abandoned = Worker(manager);
            manager.RestartGame();
            SetEngine(manager, current);
            HumanMove(manager);
            yield return Until(() => current.Started.IsSet, "New worker did not start");
            int revision = manager.Session.Revision;
            var active = Worker(manager);
            old.Release.Set();
            yield return Until(() => abandoned.IsCompleted, "Legacy worker did not return");
            for (int i = 0; i < 5; i++) yield return null;
            Assert.IsTrue(abandoned.IsCanceled);
            Assert.AreSame(active, Worker(manager));
            Assert.IsTrue(manager.engine.IsThinking);
            Assert.AreEqual(revision, manager.Session.Revision, "Old move is still legal here but must not be applied");
            Assert.AreEqual(PlayerColor.Black, manager.state.CurrentPlayer);
            Assert.IsNull(manager.engine.LastSearch);
            Assert.IsNull(manager.engine.LastError);
            current.Release.Set();
            yield return Until(() => !manager.engine.IsThinking, "Current worker did not apply its reply");
            Assert.AreEqual(revision + 1, manager.Session.Revision);
            Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        }
        finally
        {
            old?.Release.Set();
            current?.Release.Set();
            if (manager != null) manager.engine.CancelPendingMove();
        }
    }

    [UnityTest]
    public IEnumerator Disable_DiscardsLateLegacyExceptionWithoutRetryError()
    {
        var manager = GameManager.Instance;
        var old = new LegacyEngine { Fail = true };
        try
        {
            SetEngine(manager, old);
            HumanMove(manager);
            yield return Until(() => old.Started.IsSet, "Legacy worker did not start");
            var abandoned = Worker(manager);
            int revision = manager.Session.Revision;
            manager.engine.enabled = false;
            old.Release.Set();
            yield return Until(() => abandoned.IsCompleted, "Legacy worker did not fault");
            Assert.IsTrue(abandoned.IsFaulted);
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(revision, manager.Session.Revision);
            Assert.IsNull(manager.engine.LastError);
            Assert.IsFalse(manager.GetComponent<EngineStatusUI>().RetryVisible);
            LogAssert.NoUnexpectedReceived();
            manager.RestartGame();
        }
        finally
        {
            old?.Release.Set();
            if (manager != null) manager.engine.CancelPendingMove();
        }
    }
}
