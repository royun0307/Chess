using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

public class GameFlowIntegrationTests
{
    private sealed class CountingEngine : IChessEngine
    {
        public int Calls;
        public Move GetBestMove(Board board, PlayerColor player, int depth)
        {
            Calls++;
            return new GameState(player, board).AllLegalMovesFor(player).FirstOrDefault();
        }
    }

    [UnityTest]
    public IEnumerator MainScene_CancelsOldAIAndSchedulesOnlyOneReply()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
        var manager = GameManager.Instance;
        var fake = new CountingEngine();
        typeof(EngineManager).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager.engine, fake);
        manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());
        manager.engine.EngineMove();
        manager.RestartGame();
        for (float until = Time.realtimeSinceStartup + 0.3f; Time.realtimeSinceStartup < until;) yield return null;
        Assert.AreEqual(0, fake.Calls, "Restart must cancel the queued AI search.");
        Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());
        manager.engine.EngineMove();
        manager.engine.EngineMove();
        for (float until = Time.realtimeSinceStartup + 0.3f; Time.realtimeSinceStartup < until;) yield return null;
        Assert.AreEqual(1, fake.Calls, "Repeated scheduling must produce one reply.");
        Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());
        manager.SetPaused(true);
        for (float until = Time.realtimeSinceStartup + 0.3f; Time.realtimeSinceStartup < until;) yield return null;
        Assert.AreEqual(1, fake.Calls);
        manager.SetPaused(false);
        for (float until = Time.realtimeSinceStartup + 0.3f; Time.realtimeSinceStartup < until;) yield return null;
        Assert.AreEqual(2, fake.Calls);
        Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        var promotionBoard = new Board();
        promotionBoard[new Position(0,0)] = new King(PlayerColor.Black);
        promotionBoard[new Position(1,2)] = new King(PlayerColor.White);
        promotionBoard[new Position(1,1)] = new Pawn(PlayerColor.White);
        manager.state = new GameState(PlayerColor.White, promotionBoard);
        manager.Session.Restart(manager.state);
        manager.board.board = promotionBoard;
        manager.board.RedrawPiecesFromBoard();
        Assert.IsTrue(manager.BeginPromotion(new Position(1,1), new Position(0,1)));
        manager.engine.EngineMove();
        for (float until = Time.realtimeSinceStartup + 0.3f; Time.realtimeSinceStartup < until;) yield return null;
        Assert.AreEqual(2, fake.Calls, "AI must wait for the promotion choice.");
        var button = UIManager.Instance.promotionUI.quenn_image.GetComponent<UnityEngine.UI.Button>();
        button.onClick.Invoke();
        button.onClick.Invoke();
        for (float until = Time.realtimeSinceStartup + 0.3f; Time.realtimeSinceStartup < until;) yield return null;
        Assert.AreEqual(2, fake.Calls, "Promotion mate must not schedule AI.");
        Assert.AreEqual(EndReason.Checkmate, manager.state.Result.EndReason);
        Assert.IsTrue(UIManager.Instance.resultUI.gameObject.activeSelf);
        Assert.IsFalse(UIManager.Instance.promotionUI.gameObject.activeSelf);
        Assert.IsFalse(manager.CanHumanMove);
        manager.RestartGame();
        Assert.IsFalse(UIManager.Instance.resultUI.gameObject.activeSelf);
        Assert.IsTrue(manager.CanHumanMove);
        yield return new ExitPlayMode();
    }
}
