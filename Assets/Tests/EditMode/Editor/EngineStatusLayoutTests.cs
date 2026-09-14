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
        string path = Path.GetFullPath("Validation/Stage6/screens/" + name + ".png");
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
            Assert.IsTrue(Hits(ScreenRect((RectTransform)pause.transform).center).Any(h => h.gameObject == pause.gameObject), "Pause cannot receive clicks");
            pause.onClick.Invoke();
            yield return null;
            Assert.AreEqual("Paused", status.DisplayText);
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
