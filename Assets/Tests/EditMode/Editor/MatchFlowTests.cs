using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MatchFlowTests
{
    private sealed class FirstMoveEngine : IChessEngine
    {
        public Move GetBestMove(Board board, PlayerColor side, int depth) => new GameState(side, board).AllLegalMovesFor(side).First();
    }

    [UnityTest]
    public IEnumerator SetupButtons_StartBlackGame_MoveOnFlippedBoard_ThenTimeout()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
        var manager = GameManager.Instance;
        Assert.IsTrue(manager.SetupVisible);
        Assert.IsFalse(manager.CanHumanMove);
        Assert.IsFalse(manager.engine.IsThinking);
        typeof(EngineManager).GetField("engine", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager.engine, new FirstMoveEngine());
        var buttons = manager.GetComponentsInChildren<Button>();
        buttons.Single(b => b.name == ">" && ((RectTransform)b.transform).anchoredPosition.y == -65).onClick.Invoke();
        buttons.Single(b => b.name == ">" && ((RectTransform)b.transform).anchoredPosition.y == 85).onClick.Invoke();
        buttons.Single(b => b.name == ">" && ((RectTransform)b.transform).anchoredPosition.y == 10).onClick.Invoke();
        buttons.Single(b => b.name == "START GAME").onClick.Invoke();
        Assert.IsFalse(manager.SetupVisible);
        Assert.AreEqual(6, manager.InitialMinutes);
        Assert.AreEqual(4, manager.IncrementSeconds);
        Assert.IsTrue(manager.board.Flipped);
        for (float end = Time.realtimeSinceStartup + 5; !manager.CanHumanMove && Time.realtimeSinceStartup < end;) yield return null;
        Assert.IsTrue(manager.CanHumanMove, "AI must open as White when player chooses Black");
        Assert.AreEqual(PlayerColor.Black, manager.state.CurrentPlayer);
        yield return null; // Wait for the previous board views' deferred destruction.
        MoveBlackPiece(manager);
        manager.engine.CancelPendingMove();
        manager.Session.Configure(PlayerColor.Black, 0.01, 4);
        for (float end = Time.realtimeSinceStartup + 2; !manager.state.IsGameOver() && Time.realtimeSinceStartup < end;) yield return null;
        Assert.AreEqual(EndReason.Timeout, manager.state.Result.EndReason);
        Assert.AreEqual(PlayerColor.Black, manager.state.Result.Winner);
        Assert.IsTrue(UIManager.Instance.resultUI.gameObject.activeSelf);
        Assert.IsFalse(manager.engine.IsThinking);
        yield return new ExitPlayMode();
    }

    private static void MoveBlackPiece(GameManager manager)
    {
        Assert.NotNull(manager);
        Assert.NotNull(manager.state);
        Assert.NotNull(manager.state.Board);
        var move = manager.state.AllLegalMovesFor(PlayerColor.Black).First(m => m.Type != MoveType.PawnPromotion);
        var piece = Object.FindObjectsByType<Chessman>(FindObjectsSortMode.None).Single(p => p.Pos == move.FromPos);
        manager.board.OnClickChessman(piece);
        var plate = manager.board.move_plates[move.ToPos.row, move.ToPos.column];
        Assert.IsTrue(plate.gameObject.activeSelf);
        Assert.AreEqual(manager.board.origin.x + (7 - move.ToPos.column) * manager.board.cellSize, plate.transform.position.x, 0.001);
        Assert.AreEqual(manager.board.origin.y + move.ToPos.row * manager.board.cellSize, plate.transform.position.y, 0.001);
        manager.board.OnClickMovePlate(plate);
        Assert.AreEqual(PlayerColor.White, manager.state.CurrentPlayer);
        Assert.AreEqual(PlayerColor.Black, manager.state.Board[move.ToPos].Color);
    }
}
