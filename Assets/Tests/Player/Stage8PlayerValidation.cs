using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Compiled only by Stage8ValidationBuild. Drives real managers and rendered UI,
// but invokes pointer handlers rather than pretending to be operating-system input.
public sealed class Stage8PlayerValidation : MonoBehaviour
{
    [Serializable] private sealed class Report
    {
        public string result = "Running";
        public float dpi;
        public double elapsedSeconds, soakSeconds;
        public int moves, restarts, completedGames;
        public long managedBytesBefore, managedBytesAfter;
        public List<string> checks = new();
        public List<string> errors = new();
    }
    private readonly Report report = new();
    private string output;
    private double started;
    private GameManager manager;
    private readonly System.Random random = new(8128);

    private static string Argument(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
    }

    private IEnumerator Start()
    {
        output = Path.GetFullPath(Argument("-stage8-output", Path.Combine(Application.dataPath, "../Reports")));
        Directory.CreateDirectory(output);
        Application.runInBackground = true;
        Application.logMessageReceived += OnLog;
        started = Time.realtimeSinceStartupAsDouble;
        report.dpi = Screen.dpi;
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            object next = null;
            try
            {
                if (Time.realtimeSinceStartupAsDouble - started > 1200) throw new Exception("Validation watchdog expired");
                if (!stack.Peek().MoveNext()) { stack.Pop(); continue; }
                next = stack.Peek().Current;
            }
            catch (Exception e) { report.errors.Add(e.ToString()); break; }
            if (next is IEnumerator nested) stack.Push(nested);
            else yield return next;
        }
        report.result = report.errors.Count == 0 ? "Passed" : "Failed";
        Save();
        Application.logMessageReceived -= OnLog;
        Application.Quit(report.errors.Count == 0 ? 0 : 1);
    }

    private void OnLog(string message, string trace, LogType kind)
    {
        if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
            report.errors.Add(message + "\n" + trace);
    }
    private void Save()
    {
        report.elapsedSeconds = Time.realtimeSinceStartupAsDouble - started;
        File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
    }
    private void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private void Passed(string message) { report.checks.Add(message); Save(); }
    private IEnumerator Until(Func<bool> condition, string message, float timeout = 10)
    {
        double end = Time.realtimeSinceStartupAsDouble + timeout;
        while (!condition() && Time.realtimeSinceStartupAsDouble < end) yield return null;
        Check(condition(), message);
    }
    private IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        string path = Path.Combine(output, name + ".png");
        DateTime stamp = DateTime.UtcNow;
        ScreenCapture.CaptureScreenshot(path);
        yield return Until(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) >= stamp, "Screenshot not updated: " + name);
    }
    private static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private void Click(Button button)
    {
        Canvas.ForceUpdateCanvases();
        Rect rect = Bounds((RectTransform)button.transform);
        Check(rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= Screen.width && rect.yMax <= Screen.height,
            "Button outside screen: " + button.name);
        var pointer = new PointerEventData(EventSystem.current) { position = rect.center, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Button obstructed: " + button.name);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private void CheckModal(BaseUI ui)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var graphic in ui.GetComponentsInChildren<Graphic>())
        {
            var rect = Bounds(graphic.rectTransform);
            Check(rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= Screen.width && rect.yMax <= Screen.height,
                $"Modal graphic outside screen: {ui.name}/{graphic.name} {rect} at {Screen.width}x{Screen.height}");
        }
        foreach (var text in ui.GetComponentsInChildren<TextMeshProUGUI>())
        { text.ForceMeshUpdate(); Check(!text.isTextOverflowing, "Modal text overflows: " + text.name + " / " + text.text); }
    }

    private IEnumerator Run()
    {
        for (int i = 0; i < 60; i++) yield return null;
        manager = GameManager.Instance;
        Check(manager != null && manager.SetupVisible, "Initial setup missing");
        double clock = manager.Session.WhiteSeconds;
        yield return new WaitForSecondsRealtime(1);
        Check(manager.Session.WhiteSeconds == clock, "Setup clock ran");
        var buttons = manager.GetComponentsInChildren<Button>();
        foreach (float y in new[] { 85f, 10f, -65f })
            Click(buttons.Single(b => b.name == ">" && ((RectTransform)b.transform).anchoredPosition.y == y));
        Click(buttons.Single(b => b.name == "START GAME"));
        yield return Until(() => manager.CanHumanMove, "White AI opening stalled");
        Check(manager.HumanColor == PlayerColor.Black && manager.board.Flipped && manager.InitialMinutes == 6 && manager.IncrementSeconds == 4,
            "Settings did not apply");
        yield return Capture("black-opening");
        Click(UIManager.Instance.pauseUI.pause_button);
        clock = manager.Session.BlackSeconds;
        yield return new WaitForSecondsRealtime(1);
        Check(manager.Session.BlackSeconds == clock, "Paused clock ran");
        Click(UIManager.Instance.pauseUI.restart_button);
        yield return Until(() => manager.CanHumanMove, "Restarted Black game stalled");
        Check(manager.HumanColor == PlayerColor.Black && manager.InitialMinutes == 6 && manager.IncrementSeconds == 4, "Restart lost settings");
        Passed("Setup UI, Black opening, pause clock, restart with settings");

        manager.engine.enabled = false;
        foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(800,600), new Vector2Int(640,480), new Vector2Int(540,960), new Vector2Int(1920,1080) })
        {
            Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
            yield return Until(() => Screen.width == size.x && Screen.height == size.y, "Resolution not applied: " + size);
            for (int i = 0; i < 5; i++) yield return null;
            manager.StartMatch(5, 3, PlayerColor.White);
            yield return null;
            Click(UIManager.Instance.pauseUI.pause_button);
            yield return null;
            yield return Capture(size.x + "x" + size.y + "-pause");
            CheckModal(UIManager.Instance.pauseUI);
            Click(UIManager.Instance.pauseUI.continue_button);
            foreach (var side in new[] { PlayerColor.White, PlayerColor.Black })
            {
                PromotionPosition(side, 60);
                yield return null;
                int row = side == PlayerColor.White ? 1 : 6;
                Check(manager.BeginPromotion(new Position(row,1), new Position(side == PlayerColor.White ? 0 : 7,1)), "Promotion did not open");
                yield return null;
                yield return Capture(size.x + "x" + size.y + "-" + side + "-promotion");
                CheckModal(UIManager.Instance.promotionUI);
                Click(UIManager.Instance.promotionUI.quenn_image.GetComponent<Button>());
                yield return null;
                Check(manager.state.Result?.EndReason == EndReason.Checkmate && manager.state.Result.Winner == side, "Promotion mate failed");
                CheckModal(UIManager.Instance.resultUI);
                yield return Capture(size.x + "x" + size.y + "-" + side + "-result");
                Click(UIManager.Instance.resultUI.restart_button);
                Check(!manager.state.IsGameOver() && manager.HumanColor == side, "Result restart failed");
            }
            Passed("Modal layout and promotion mate/restart at " + size);
        }
        PromotionPosition(PlayerColor.White, 0.3);
        Check(manager.BeginPromotion(new Position(1,1), new Position(0,1)), "Timeout promotion missing");
        yield return Until(() => manager.state.IsGameOver(), "Promotion timeout missing");
        Check(manager.state.Result.EndReason == EndReason.Timeout && manager.state.Result.Winner == PlayerColor.None, "Bare king timeout should draw");
        Check(!manager.Session.PromotionPending && !UIManager.Instance.promotionUI.gameObject.activeSelf, "Expired promotion remained active");
        yield return Capture("promotion-timeout");
        Passed("Promotion time expires and closes selection; bare king draw");

        Screen.SetResolution(1280,720,FullScreenMode.Windowed);
        manager.engine.enabled = true;
        manager.StartMatch(5,3,PlayerColor.White);
        yield return null;
        report.managedBytesBefore = GC.GetTotalMemory(true);
        double soakStart = Time.realtimeSinceStartupAsDouble;
        int seconds = int.Parse(Argument("-stage8-seconds", "600"));
        int lastRevision = manager.Session.Revision;
        double lastProgress = soakStart, nextSample = soakStart + 30;
        while (Time.realtimeSinceStartupAsDouble - soakStart < seconds)
        {
            Check(manager.engine.LastError == null, "AI error: " + manager.engine.LastError);
            if (manager.Session.Revision != lastRevision)
            { lastProgress = Time.realtimeSinceStartupAsDouble; lastRevision = manager.Session.Revision; }
            Check(Time.realtimeSinceStartupAsDouble - lastProgress < 15, "Soak stalled for 15 seconds");
            if (manager.state.IsGameOver())
            {
                report.completedGames++;
                Check(UIManager.Instance.resultUI.gameObject.activeSelf, "Soak result UI missing");
                manager.StartMatch(5,3,report.completedGames % 2 == 0 ? PlayerColor.White : PlayerColor.Black);
                report.restarts++;
            }
            else if (manager.CanHumanMove)
            {
                var moves = manager.state.AllLegalMovesFor(manager.HumanColor).ToArray();
                Check(moves.Length > 0, "No human move without terminal result");
                var move = moves[random.Next(moves.Length)];
                if (move.Type == MoveType.PawnPromotion)
                {
                    Check(manager.BeginPromotion(move.FromPos,move.ToPos), "Soak promotion rejected");
                    Click(UIManager.Instance.promotionUI.quenn_image.GetComponent<Button>());
                }
                else manager.MakeMove(move);
                report.moves++;
            }
            if (Time.realtimeSinceStartupAsDouble >= nextSample)
            {
                // Also exercise cancellation/re-enable of an in-flight engine during long use.
                manager.engine.enabled = false;
                yield return null;
                manager.engine.enabled = true;
                report.soakSeconds = Time.realtimeSinceStartupAsDouble - soakStart;
                report.managedBytesAfter = GC.GetTotalMemory(false);
                Save(); nextSample += 30;
            }
            yield return null;
        }
        manager.SetPaused(true);
        report.soakSeconds = Time.realtimeSinceStartupAsDouble - soakStart;
        report.managedBytesAfter = GC.GetTotalMemory(true);
        Check(report.moves > 10, "Too few moves during soak");
        Passed("Continuous player soak with AI, analysis, clocks and engine disable/re-enable");
        yield return Capture("soak-final");
    }

    private void PromotionPosition(PlayerColor side, double seconds)
    {
        manager.StartMatch(5,3,side);
        var board = new Board();
        int Flip(int row) => side == PlayerColor.White ? row : 7-row;
        board[new Position(Flip(0),0)] = new King(side.Opponent());
        board[new Position(Flip(1),2)] = new King(side);
        board[new Position(Flip(1),1)] = new Pawn(side);
        manager.state = new GameState(side,board);
        manager.Session.Restart(manager.state);
        manager.Session.Configure(side,seconds,3);
        manager.board.board = board;
        manager.board.RedrawPiecesFromBoard();
    }
}
