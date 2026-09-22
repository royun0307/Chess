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
using UnityEngine.UI;

public class MatchLifecycleTests
{
    [UnitySetUp]
    public IEnumerator OpenScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
        GameManager.Instance.engine.enabled = false;
        GameManager.Instance.StartMatch(5, 3, PlayerColor.White);
    }

    [UnityTearDown]
    public IEnumerator CloseScene() { yield return new ExitPlayMode(); }

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    private static void UpdateAnalysis(PositionAnalysis analysis) =>
        typeof(PositionAnalysis).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(analysis, null);

    // Inject a controlled completion at the worker boundary, without a timing-dependent search.
    private static CancellationToken Install(PositionAnalysis analysis, Task<EngineSearchResult> task)
    {
        analysis.enabled = false;
        analysis.enabled = true;
        var source = new CancellationTokenSource();
        Set(analysis, "source", source);
        Set(analysis, "task", task);
        Set(analysis, "session", GameManager.Instance.Session);
        Set(analysis, "revision", GameManager.Instance.Session.Revision);
        return source.Token;
    }

    private static EngineSearchResult Score(int value) => new EngineSearchResult(null, 2, 1, TimeSpan.Zero, false, value);

    [UnityTest]
    public IEnumerator Analysis_HidesPreviousPositionImmediatelyAfterRestartAndDisable()
    {
        var manager = GameManager.Instance;
        var analysis = manager.GetComponent<PositionAnalysis>();
        Install(analysis, Task.FromResult(Score(725)));
        UpdateAnalysis(analysis);
        Assert.AreEqual(725, analysis.Score);
        manager.StartMatch(2, 0, PlayerColor.Black);
        Assert.IsNull(analysis.Score, "Restart must invalidate the score before the next Update");
        Assert.AreEqual(0, analysis.Depth);
        Install(analysis, Task.FromResult(Score(-350)));
        UpdateAnalysis(analysis);
        Assert.AreEqual(-350, analysis.Score);
        analysis.enabled = false;
        Assert.IsNull(analysis.Score, "Disabled analysis must not leave a usable old score");
        Assert.AreEqual(0, analysis.Depth);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Analysis_LateCompletionCannotReplaceNewGameScore()
    {
        var manager = GameManager.Instance;
        var analysis = manager.GetComponent<PositionAnalysis>();
        var old = new TaskCompletionSource<EngineSearchResult>();
        var token = Install(analysis, old.Task);
        manager.StartMatch(3, 1, PlayerColor.Black);
        UpdateAnalysis(analysis);
        Assert.IsTrue(token.IsCancellationRequested);
        Install(analysis, Task.FromResult(Score(-125)));
        UpdateAnalysis(analysis);
        old.SetResult(Score(900));
        yield return null;
        Assert.AreEqual(-125, analysis.Score);
        Assert.AreEqual(2, analysis.Depth);
        manager.SetPaused(true);
        Assert.IsNull(analysis.Score, "Pause invalidates the published score immediately");
        yield return null;
        Assert.IsFalse(analysis.Busy);
        manager.SetPaused(false);
        for (float end = Time.realtimeSinceStartup + 5; !analysis.Score.HasValue && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsTrue(analysis.Score.HasValue, "Resume must request a fresh analysis");
    }

    [UnityTest]
    public IEnumerator RestartAndSetup_PreserveSettingsFreezeClockAndRejectOldEngineMove()
    {
        var manager = GameManager.Instance;
        manager.StartMatch(2, 7, PlayerColor.Black);
        var oldMove = manager.state.AllLegalMovesFor(PlayerColor.White).First();
        int revision = manager.Session.Revision;
        manager.SetPaused(true);
        double remaining = manager.Session.WhiteSeconds;
        yield return null;
        yield return null;
        Assert.AreEqual(remaining, manager.Session.WhiteSeconds);
        UIManager.Instance.pauseUI.restart_button.onClick.Invoke();
        Assert.AreEqual(PlayerColor.Black, manager.Session.HumanColor);
        Assert.AreEqual(120, manager.Session.WhiteSeconds);
        Assert.AreEqual(120, manager.Session.BlackSeconds);
        Assert.AreEqual(7, manager.Session.IncrementSeconds);
        Assert.IsTrue(manager.board.Flipped);
        Assert.IsFalse(manager.ApplyEngineMove(oldMove, manager.Session, revision));
        manager.GetComponentsInChildren<Button>().Single(b => b.name == "New game").onClick.Invoke();
        remaining = manager.Session.WhiteSeconds;
        yield return null;
        Assert.IsTrue(manager.SetupVisible);
        Assert.AreEqual(remaining, manager.Session.WhiteSeconds);
        Assert.IsFalse(manager.ApplyEngineMove(oldMove, manager.Session, manager.Session.Revision));
        Assert.IsFalse(manager.CanHumanMove);
    }

    [UnityTest]
    public IEnumerator LateHumanAndEngineCallbacks_CannotBeatClockExpiry()
    {
        var manager = GameManager.Instance;
        var move = manager.state.AllLegalMovesFor(PlayerColor.White).First();
        manager.Session.Configure(PlayerColor.White, 1, 7);
        Set(manager, "clockStamp", Time.realtimeSinceStartupAsDouble - 2);
        manager.MakeMove(move);
        Assert.AreEqual(EndReason.Timeout, manager.state.Result.EndReason);
        Assert.NotNull(manager.state.Board[move.FromPos]);
        Assert.AreEqual(0, manager.Session.WhiteSeconds);
        Assert.IsTrue(UIManager.Instance.resultUI.gameObject.activeSelf);
        manager.StartMatch(1, 7, PlayerColor.Black);
        move = manager.state.AllLegalMovesFor(PlayerColor.White).First();
        manager.Session.Configure(PlayerColor.Black, 1, 7);
        Set(manager, "clockStamp", Time.realtimeSinceStartupAsDouble - 2);
        Assert.IsFalse(manager.ApplyEngineMove(move, manager.Session, manager.Session.Revision));
        Assert.AreEqual(EndReason.Timeout, manager.state.Result.EndReason);
        Assert.AreEqual(PlayerColor.Black, manager.state.Result.Winner);
        Assert.NotNull(manager.state.Board[move.FromPos]);
        yield return null;
    }
}
