using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchUI : MonoBehaviour
{
    private RectTransform root, topCard, bottomCard, bar, whiteFill, newGame;
    private GameObject setup, hud;
    private TextMeshProUGUI minutesText, incrementText, colorText, topName, bottomName;
    private TextMeshProUGUI topClock, bottomClock, topMaterial, bottomMaterial, evaluation;
    private int minutes = 5, increment = 3;
    private PlayerColor color = PlayerColor.White;
    private Image topBackground, bottomBackground;
    private Image[] topCaptured, bottomCaptured;
    private TextMeshProUGUI evaluationNumber;
    private static readonly Color Panel = new Color(0.09f, 0.12f, 0.16f, 0.98f);
    private static readonly Color Accent = new Color(0.27f, 0.65f, 0.53f);

    private void Start()
    {
        var canvas = new GameObject("Match interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 800);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root = canvas.GetComponent<RectTransform>();
        hud = Box(root, "Game HUD", Vector2.zero, Vector2.zero, Color.clear).gameObject;
        Stretch((RectTransform)hud.transform);
        topCard = Card(hud.transform, "Opponent", out topName, out topClock, out topMaterial, out topBackground);
        bottomCard = Card(hud.transform, "Player", out bottomName, out bottomClock, out bottomMaterial, out bottomBackground);
        topCaptured = CaptureIcons(topCard);
        bottomCaptured = CaptureIcons(bottomCard);
        gameObject.AddComponent<BoardBackdrop>();
        bar = Box(hud.transform, "Evaluation bar", Vector2.zero, new Vector2(16, 400), new Color(0.12f, 0.14f, 0.17f));
        whiteFill = Box(bar, "White advantage", Vector2.zero, Vector2.zero, new Color(0.93f, 0.94f, 0.90f));
        // Fixed midpoint marks an equal position, regardless of the current score.
        Box(bar, "Zero evaluation reference", Vector2.zero, new Vector2(80, 2), Accent);
        evaluation = Label(hud.transform, "Evaluation", Vector2.zero, new Vector2(220, 28), 16);
        var badge = Box(hud.transform, "Evaluation number badge", Vector2.zero, new Vector2(64, 34), Panel);
        evaluationNumber = Label(badge, "Evaluation number", Vector2.zero, new Vector2(62, 32), 20);
        evaluationNumber.fontStyle = FontStyles.Bold;
        newGame = Button(hud.transform, "New game", Vector2.zero, new Vector2(150, 32), () => GameManager.Instance.ShowSetup());

        setup = Box(root, "Start screen", Vector2.zero, Vector2.zero, new Color(0.035f, 0.055f, 0.075f, 1f)).gameObject;
        Stretch((RectTransform)setup.transform);
        setup.GetComponent<Image>().raycastTarget = true;
        var menu = Box(setup.transform, "Game settings", Vector2.zero, new Vector2(520, 520), Panel);
        Label(menu, "CHESS", new Vector2(0, 204), new Vector2(440, 62), 42).fontStyle = FontStyles.Bold;
        Label(menu, "Choose your time control and side", new Vector2(0, 151), new Vector2(460, 30), 18).color = new Color(0.65f, 0.73f, 0.79f);
        minutesText = Setting(menu, "Time per side", 85, () => { minutes = Mathf.Max(1, minutes - 1); RefreshSettings(); }, () => { minutes = Mathf.Min(180, minutes + 1); RefreshSettings(); });
        incrementText = Setting(menu, "Increment per move", 10, () => { increment = Mathf.Max(0, increment - 1); RefreshSettings(); }, () => { increment = Mathf.Min(60, increment + 1); RefreshSettings(); });
        colorText = Setting(menu, "Play as", -65, ToggleColor, ToggleColor);
        Button(menu, "START GAME", new Vector2(0, -168), new Vector2(420, 54), () => GameManager.Instance.StartMatch(minutes, increment, color));
        Label(menu, "Your pieces always start at the bottom", new Vector2(0, -222), new Vector2(460, 26), 16);
        RefreshSettings();
    }

    private void ToggleColor() { color = color.Opponent(); RefreshSettings(); }
    private void RefreshSettings()
    {
        minutesText.text = minutes + " min";
        incrementText.text = "+ " + increment + " sec";
        colorText.text = color == PlayerColor.White ? "WHITE" : "BLACK";
    }

    private static TextMeshProUGUI Setting(Transform parent, string title, float y, Action minus, Action plus)
    {
        var label = Label(parent, title, new Vector2(-110, y), new Vector2(210, 44), 18);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        Button(parent, "<", new Vector2(22, y), new Vector2(36, 40), minus);
        Button(parent, ">", new Vector2(202, y), new Vector2(36, 40), plus);
        return Label(parent, title + " value", new Vector2(112, y), new Vector2(135, 44), 23);
    }

    private static RectTransform Card(Transform parent, string title, out TextMeshProUGUI name, out TextMeshProUGUI clock, out TextMeshProUGUI material, out Image background)
    {
        var card = Box(parent, title + " card", Vector2.zero, new Vector2(500, 72), Panel);
        background = card.GetComponent<Image>();
        name = Label(card, title, new Vector2(14, -17), new Vector2(230, 28), 20);
        name.rectTransform.anchorMin = name.rectTransform.anchorMax = name.rectTransform.pivot = new Vector2(0, 1);
        name.alignment = TextAlignmentOptions.MidlineLeft;
        material = Label(card, title + " advantage", new Vector2(14, -44), new Vector2(48, 24), 18);
        material.rectTransform.anchorMin = material.rectTransform.anchorMax = material.rectTransform.pivot = new Vector2(0, 1);
        material.alignment = TextAlignmentOptions.MidlineLeft;
        clock = Label(card, title + " clock", new Vector2(-14, 0), new Vector2(130, 60), 34);
        clock.rectTransform.anchorMin = clock.rectTransform.anchorMax = clock.rectTransform.pivot = new Vector2(1, 0.5f);
        clock.alignment = TextAlignmentOptions.MidlineRight;
        return card;
    }

    private void LateUpdate()
    {
        if (root == null) return;
        var manager = GameManager.Instance;
        if (manager?.Session == null) return;
        // Existing pause/promotion/result dialogs must cover the HUD; setup covers everything.
        root.GetComponent<Canvas>().sortingOrder = manager.SetupVisible ? 30 : -1;
        setup.SetActive(manager.SetupVisible);
        hud.SetActive(!manager.SetupVisible);
        if (manager.SetupVisible) return;
        var session = manager.Session;
        var player = session.HumanColor;
        topName.text = player.Opponent() + "  /  AI";
        bottomName.text = player + "  /  YOU";
        topClock.text = ClockText(session.Remaining(player.Opponent()));
        bottomClock.text = ClockText(session.Remaining(player));
        int white = Material(session.State.Board, PlayerColor.White);
        int black = Material(session.State.Board, PlayerColor.Black);
        int own = player == PlayerColor.White ? white : black, other = player == PlayerColor.White ? black : white;
        topMaterial.text = other > own ? "+" + (other - own) : "";
        bottomMaterial.text = own > other ? "+" + (own - other) : "";
        topBackground.color = !session.Paused && !session.State.IsGameOver() && session.State.CurrentPlayer != player ? new Color(0.13f, 0.24f, 0.23f) : Panel;
        bottomBackground.color = !session.Paused && !session.State.IsGameOver() && session.State.CurrentPlayer == player ? new Color(0.13f, 0.24f, 0.23f) : Panel;
        topClock.color = session.Remaining(player.Opponent()) < 10 ? new Color(1, 0.45f, 0.35f) : Color.white;
        bottomClock.color = session.Remaining(player) < 10 ? new Color(1, 0.45f, 0.35f) : Color.white;
        Layout(manager.board);
        DrawCaptures(topCaptured, topMaterial, topCard, session.CapturedBy(player.Opponent()), player, manager.board);
        DrawCaptures(bottomCaptured, bottomMaterial, bottomCard, session.CapturedBy(player), player.Opponent(), manager.board);
        var analysis = manager.GetComponent<PositionAnalysis>();
        int? score = analysis.Score;
        float fraction = score.HasValue ? 0.5f + 0.5f * (float)Math.Tanh(score.Value / 400.0) : 0.5f;
        if (session.State.IsGameOver())
        {
            var winner = session.State.Result.Winner;
            fraction = winner == PlayerColor.None ? 0.5f : winner == PlayerColor.White ? 1 : 0;
            evaluation.text = winner == PlayerColor.None ? "DRAW" : winner + " wins";
            evaluationNumber.text = winner == PlayerColor.None ? "0.00" : winner == PlayerColor.White ? "1-0" : "0-1";
        }
        else
        {
            evaluationNumber.text = score.HasValue ? ScoreText(score.Value) : "...";
            evaluation.text = score.HasValue ? "White evaluation  " + ScoreText(score.Value) : session.Paused ? "Evaluation paused" : analysis.Busy ? "Analyzing..." : "Evaluation unavailable";
        }
        whiteFill.anchorMin = new Vector2(0, player == PlayerColor.White ? 0 : 1 - fraction);
        whiteFill.anchorMax = new Vector2(1, player == PlayerColor.White ? fraction : 1);
        whiteFill.offsetMin = whiteFill.offsetMax = Vector2.zero;
    }

    private void Layout(BoardManager board)
    {
        var camera = Camera.main;
        if (camera == null) return;
        // Keep board, cards and the evaluation bar in view in portrait and landscape.
        camera.orthographicSize = Mathf.Max(4.5f, 3.4f / camera.aspect);
        Vector2 low, high;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, camera.WorldToScreenPoint(new Vector3(board.origin.x - board.cellSize / 2, board.origin.y - board.cellSize / 2, 0)), null, out low);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, camera.WorldToScreenPoint(new Vector3(board.origin.x + board.cellSize * 7.5f, board.origin.y + board.cellSize * 7.5f, 0)), null, out high);
        float middle = (low.x + high.x) / 2;
        topCard.anchoredPosition = new Vector2(middle, high.y + 48);
        bottomCard.anchoredPosition = new Vector2(middle, low.y - 48);
        topCard.sizeDelta = bottomCard.sizeDelta = new Vector2(high.x - low.x, 72);
        bar.anchoredPosition = new Vector2(low.x - 42, (low.y + high.y) / 2);
        bar.sizeDelta = new Vector2(16, high.y - low.y);
        evaluation.rectTransform.anchoredPosition = new Vector2(middle, low.y - 104);
        ((RectTransform)evaluationNumber.transform.parent).anchoredPosition = bar.anchoredPosition;
        newGame.anchoredPosition = new Vector2(middle, low.y - 140);
    }

    public static int Material(Board board, PlayerColor color)
    {
        int value = 0;
        foreach (var position in board.PiecePositionsFor(color))
            value += board[position].Type switch { PieceType.Pawn => 1, PieceType.Knight => 3, PieceType.Bishop => 3, PieceType.Rook => 5, PieceType.Queen => 9, _ => 0 };
        return value;
    }
    private static Image[] CaptureIcons(RectTransform card)
    {
        var icons = new Image[15];
        for (int i = 0; i < icons.Length; i++)
        {
            var rect = Box(card, "Captured piece " + i, Vector2.zero, new Vector2(22, 22), Color.white);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            icons[i] = rect.GetComponent<Image>();
            icons[i].preserveAspect = true;
            icons[i].gameObject.SetActive(false);
        }
        return icons;
    }

    private static void DrawCaptures(Image[] icons, TextMeshProUGUI advantage, RectTransform card,
        System.Collections.Generic.IReadOnlyList<PieceType> captures, PlayerColor capturedColor, BoardManager board)
    {
        float available = card.sizeDelta.x - 224;
        float step = captures.Count < 2 ? 24 : Mathf.Min(24, (available - 22) / (captures.Count - 1));
        for (int i = 0; i < icons.Length; i++)
        {
            bool visible = i < captures.Count;
            icons[i].gameObject.SetActive(visible);
            if (!visible) continue;
            icons[i].sprite = board.PieceSprite(capturedColor, captures[i]);
            icons[i].rectTransform.anchoredPosition = new Vector2(14 + i * step, -44);
        }
        float used = captures.Count == 0 ? 0 : (captures.Count - 1) * step + 22;
        advantage.rectTransform.anchoredPosition = new Vector2(20 + used, -44);
    }
    public static string ClockText(double seconds)
    {
        int total = (int)Math.Ceiling(Math.Max(0, seconds));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }
    public static string ScoreText(int score) => Math.Abs(score) > 990000
        ? (score < 0 ? "-M" : "M") + ((1000000 - 1 - Math.Abs(score) + 1) / 2)
        : (score / 100.0).ToString("+0.00;-0.00;0.00", System.Globalization.CultureInfo.InvariantCulture);

    private static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static TextMeshProUGUI Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Midline;
        label.raycastTarget = false;
        return label;
    }
    private static RectTransform Button(Transform parent, string title, Vector2 position, Vector2 size, Action action)
    {
        var rect = Box(parent, title, position, size, Accent);
        rect.GetComponent<Image>().raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(() => action());
        Label(rect, title, Vector2.zero, size, 20);
        return rect;
    }
}
