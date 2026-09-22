using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Run with a graphics device and -testFilter EngineStatusLayoutTests (without -batchmode).
// Captures the real Game view, including the ScreenSpaceOverlay canvas.
public class EngineStatusLayoutTests
{
    private EditorWindow gameView;
    private object group;
    private int originalIndex;
    private bool sizeAdded;

    [UnitySetUp]
    public IEnumerator OpenScene()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            Assert.Ignore("Rendered layout validation requires a graphics device");
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        yield return new EnterPlayMode();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator CloseScene()
    {
        RestoreSize();
        if (EditorApplication.isPlaying) yield return new ExitPlayMode();
    }

    private void SelectSize(int width, int height)
    {
        var editor = typeof(Editor).Assembly;
        var viewType = editor.GetType("UnityEditor.GameView");
        if (gameView == null)
        {
            gameView = EditorWindow.GetWindow(viewType);
            originalIndex = (int)viewType.GetProperty("selectedSizeIndex").GetValue(gameView);
            var sizesType = editor.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var getGroup = sizesType.GetMethod("GetGroup");
            group = getGroup.Invoke(sizes, new[] { Enum.Parse(getGroup.GetParameters()[0].ParameterType, "Standalone") });
        }
        RemoveSize();
        var sizeType = editor.GetType("UnityEditor.GameViewSize");
        var kind = editor.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kind, "FixedResolution"), (object)width, height, "Stage6 validation" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        sizeAdded = true;
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        viewType.GetProperty("selectedSizeIndex").SetValue(gameView, total - 1);
        gameView.Show();
        gameView.Focus();
        gameView.Repaint();
    }

    private void RemoveSize()
    {
        if (!sizeAdded) return;
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { total - 1 });
        sizeAdded = false;
    }

    private void RestoreSize()
    {
        if (gameView == null) return;
        gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView, originalIndex);
        RemoveSize();
    }

    private static Rect ScreenRect(RectTransform transform)
    {
        var corners = new Vector3[4];
        transform.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private static void InsideScreen(Rect rect, string name)
    {
        Assert.GreaterOrEqual(rect.xMin, 0, name);
        Assert.GreaterOrEqual(rect.yMin, 0, name);
        Assert.LessOrEqual(rect.xMax, Screen.width, name);
        Assert.LessOrEqual(rect.yMax, Screen.height, name);
    }

    private static List<RaycastResult> Hits(Vector2 point)
    {
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        return hits;
    }

    private sealed class FailedEngine : IChessEngine
    {
        public Move GetBestMove(Board board, PlayerColor side, int depth) =>
            throw new InvalidOperationException("Expected layout validation failure");
    }

    private static IEnumerator Capture(string name)
    {
        string path = Path.GetFullPath("Validation/Stage7/screens/" + name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        // EditMode's runner accepts null yields even while the scene is playing.
        // CaptureScreenshot schedules the readback after rendering the next frame.
        var requestedAt = DateTime.UtcNow;
        ScreenCapture.CaptureScreenshot(path);
        for (float end = Time.realtimeSinceStartup + 5;
             (!File.Exists(path) || File.GetLastWriteTimeUtc(path) < requestedAt) && Time.realtimeSinceStartup < end;)
            yield return null;
        Assert.IsTrue(File.Exists(path) && File.GetLastWriteTimeUtc(path) >= requestedAt,
            "A fresh Game view screenshot was not saved: " + path);
    }

    [UnityTest]
    public IEnumerator MatchSetupAndBothPerspectivesRemainVisible()
    {
        var manager = GameManager.Instance;
        manager.engine.enabled = false;
        foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(800, 600), new Vector2Int(540, 960) })
        {
            SelectSize(size.x, size.y);
            for (int i = 0; i < 10; i++) yield return null;
            manager.ShowSetup();
            yield return null;
            Assert.IsFalse(manager.CanHumanMove);
            double remaining = manager.Session.WhiteSeconds;
            yield return null;
            Assert.AreEqual(remaining, manager.Session.WhiteSeconds);
            Canvas.ForceUpdateCanvases();
            var setup = manager.GetComponentsInChildren<RectTransform>().Single(r => r.name == "Start screen");
            foreach (var button in setup.GetComponentsInChildren<Button>())
            {
                var rect = ScreenRect((RectTransform)button.transform);
                InsideScreen(rect, button.name);
                Assert.AreEqual(button, Hits(rect.center).First().gameObject.GetComponentInParent<Button>(), "Setup button is obstructed");
            }
            var centerHit = Hits(Camera.main.WorldToScreenPoint(Vector3.zero)).First();
            Assert.IsTrue(centerHit.gameObject.transform.IsChildOf(setup), "Setup must block the board");
            yield return Capture(size.x + "x" + size.y + "-setup");
            foreach (var side in new[] { PlayerColor.White, PlayerColor.Black })
            {
                manager.StartMatch(10, 5, side);
                for (int i = 0; i < 5; i++) yield return null;
                var analysis = manager.GetComponent<PositionAnalysis>();
                for (float end = Time.realtimeSinceStartup + 5; analysis.Busy && Time.realtimeSinceStartup < end;) yield return null;
                Assert.IsTrue(analysis.Score.HasValue, "Current initial position must produce a completed evaluation");
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.AreEqual(side == PlayerColor.Black, manager.board.Flipped);
                var allPieces = UnityEngine.Object.FindObjectsByType<Chessman>(FindObjectsSortMode.None);
                float ownY = allPieces.Where(p => manager.state.Board[p.Pos].Color == side).Average(p => p.transform.position.y);
                float otherY = allPieces.Where(p => manager.state.Board[p.Pos].Color != side).Average(p => p.transform.position.y);
                Assert.Less(ownY, otherY);
                foreach (var piece in allPieces)
                    Assert.IsFalse(Hits(Camera.main.WorldToScreenPoint(piece.transform.position)).Any(h => h.gameObject.GetComponent<Graphic>() != null), "HUD blocks a piece in " + side + " perspective");
                foreach (var name in new[] { "Opponent card", "Player card", "Evaluation bar", "Evaluation number badge", "New game" })
                {
                    var rect = manager.GetComponentsInChildren<RectTransform>().Single(r => r.name == name && r.GetComponent<Image>() != null);
                    InsideScreen(ScreenRect(rect), name);
                }
                foreach (var text in manager.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.ForceMeshUpdate();
                    Assert.IsFalse(text.isTextOverflowing, text.name);
                }
                yield return Capture(size.x + "x" + size.y + "-" + side);
                MatchSettingsTests.Play(manager.Session, 6, 4, 4, 4);
                MatchSettingsTests.Play(manager.Session, 1, 3, 3, 3);
                MatchSettingsTests.Play(manager.Session, 4, 4, 3, 3);
                manager.board.RedrawPiecesFromBoard();
                yield return null;
                var cardName = side == PlayerColor.White ? "Player card" : "Opponent card";
                var capturedCard = manager.GetComponentsInChildren<RectTransform>().Single(r => r.name == cardName);
                var icons = capturedCard.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Captured piece")).ToArray();
                Assert.AreEqual(1, icons.Length);
                Assert.NotNull(icons[0].sprite);
                Assert.IsTrue(capturedCard.GetComponentsInChildren<TextMeshProUGUI>().Any(t => t.text == "+1"));
                for (float end = Time.realtimeSinceStartup + 5; analysis.Busy && Time.realtimeSinceStartup < end;) yield return null;
                yield return null;
                yield return Capture(size.x + "x" + size.y + "-" + side + "-capture");
            }
        }
    }

    [UnityTest]
    public IEnumerator MainScene_RenderedStatusAndControlsAtDesktopResolutions()
    {
        var manager = GameManager.Instance;
        var status = manager.GetComponent<EngineStatusUI>();
        var label = manager.GetComponentsInChildren<TextMeshProUGUI>().Single(t => t.name == "Turn status");
        var strip = (RectTransform)label.transform.parent;
        var pause = UIManager.Instance.pauseUI.pause_button;
        var retry = manager.GetComponentsInChildren<Button>(true).Single(b => b.name == "Retry AI");
        foreach (var size in new[] { new Vector2Int(800, 600), new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1024, 768), new Vector2Int(2560, 1080) })
        {
            SelectSize(size.x, size.y);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(size.x, Screen.width);
            Assert.AreEqual(size.y, Screen.height);
            manager.RestartGame();
            yield return null;
            Canvas.ForceUpdateCanvases();
            label.ForceMeshUpdate();
            string prefix = size.x + "x" + size.y;
            yield return Capture(prefix + "-turn");
            InsideScreen(ScreenRect(strip), "Status strip");
            InsideScreen(ScreenRect((RectTransform)pause.transform), "Pause button");
            Assert.IsFalse(ScreenRect(strip).Overlaps(ScreenRect((RectTransform)pause.transform)), "Pause overlaps status");
            Assert.IsFalse(label.isTextOverflowing, "Turn text overflows");
            // All 64 board centers remain below the strip, and receive no overlay UI raycasts.
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    Vector2 world = manager.board.origin + new Vector2(col, 7 - row) * manager.board.cellSize;
                    Vector2 screen = Camera.main.WorldToScreenPoint(world);
                    Assert.IsFalse(ScreenRect(strip).Contains(screen), "Status covers a square");
                    Assert.IsFalse(Hits(screen).Any(h => h.gameObject.GetComponent<Graphic>() != null), "UI intercepts a board square");
                }
            Assert.IsTrue(Hits(ScreenRect((RectTransform)pause.transform).center).Any(h => h.gameObject.GetComponentInParent<Button>() == pause), "Pause cannot receive clicks");
            pause.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Paused", status.DisplayText);
            var hudCanvas = manager.GetComponentsInChildren<Canvas>().Single(c => c.name == "Match interface");
            Assert.Less(hudCanvas.sortingOrder, UIManager.Instance.pauseUI.GetComponentInParent<Canvas>().sortingOrder,
                "Evaluation bar and HUD must render behind the pause dialog");
            var continueButton = UIManager.Instance.pauseUI.continue_button;
            Assert.AreEqual(continueButton, Hits(ScreenRect((RectTransform)continueButton.transform).center).First().gameObject.GetComponentInParent<Button>());
            yield return Capture(prefix + "-paused");
            UIManager.Instance.pauseUI.continue_button.onClick.Invoke();
            typeof(EngineManager).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager.engine, new FailedEngine());
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Expected layout validation failure"));
            manager.MakeMove(manager.state.AllLegalMovesFor(PlayerColor.White).First());
            for (float end = Time.realtimeSinceStartup + 5; !status.RetryVisible && Time.realtimeSinceStartup < end;) yield return null;
            Assert.IsTrue(status.RetryVisible);
            label.ForceMeshUpdate();
            Assert.IsFalse(label.isTextOverflowing, "Failure text overflows");
            InsideScreen(ScreenRect((RectTransform)retry.transform), "Retry button");
            Assert.IsTrue(Hits(ScreenRect((RectTransform)retry.transform).center).Any(h => h.gameObject == retry.gameObject), "Retry cannot receive clicks");
            yield return Capture(prefix + "-retry");
        }
    }
}
