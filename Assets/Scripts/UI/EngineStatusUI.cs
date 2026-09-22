using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runtime overlay avoids changes to existing scene and prefab references.
public sealed class EngineStatusUI : MonoBehaviour
{
    private TextMeshProUGUI label;
    private Button retry;
    public string DisplayText => label == null ? "" : label.text;
    public bool RetryVisible => retry != null && retry.gameObject.activeSelf;

    private void Start()
    {
        var canvasObject = new GameObject("Engine status", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;
        var strip = new GameObject("Status strip", typeof(RectTransform), typeof(Image));
        strip.transform.SetParent(canvasObject.transform, false);
        var rect = strip.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -12);
        rect.sizeDelta = new Vector2(380, 40);
        var background = strip.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);
        background.raycastTarget = false;
        label = CreateText(strip.transform, "Turn status");
        label.margin = new Vector4(12, 0, 12, 0);
        var buttonObject = new GameObject("Retry AI", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(strip.transform, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1, 0.5f);
        buttonRect.anchoredPosition = new Vector2(-8, 0);
        buttonRect.sizeDelta = new Vector2(74, 28);
        buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.4f, 0.7f);
        retry = buttonObject.GetComponent<Button>();
        retry.onClick.AddListener(() => GameManager.Instance?.engine.EngineMove());
        CreateText(buttonObject.transform, "Retry").text = "Retry";
        retry.gameObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.sizeDelta = Vector2.zero;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.Midline;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void Update()
    {
        if (label == null) return;
        var manager = GameManager.Instance;
        var session = manager?.Session;
        bool failed = session != null && session.CanEngineMove && manager.engine.LastError != null;
        string status = manager != null && manager.SetupVisible ? "Set up your game" : session == null ? "Starting..." :
            session.State.IsGameOver() ? "Game over" : session.Paused ? "Paused" :
            session.PromotionPending ? "Choose promotion" :
            failed ? "AI unavailable" : manager.engine.IsThinking ? "AI thinking..." :
            session.CanHumanMove ? "Your turn (" + session.HumanColor + ")" : "AI turn (" + session.HumanColor.Opponent() + ")";
        if (label.text != status) label.text = status;
        if (retry.gameObject.activeSelf != failed) retry.gameObject.SetActive(failed);
        label.margin = new Vector4(12, 0, failed ? 90 : 12, 0);
    }
}
