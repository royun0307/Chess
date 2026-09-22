using UnityEngine;
using UnityEngine.UI;

public class PauseUI : BaseUI
{
    public Button pause_button;
    public Button restart_button;
    public Button continue_button;
    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);
        // The scene's old center-relative offset put this control off-screen at 720p.
        // Keep it in the top-left corner without changing scene/prefab references.
        var rect = (RectTransform)pause_button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -12);
        rect.sizeDelta = new Vector2(40, 40);
        var oldImage = pause_button.GetComponent<Image>();
        if (oldImage != null) { oldImage.sprite = null; oldImage.color = Color.white; oldImage.enabled = false; }
        var theme = pause_button.GetComponentInChildren<SettingsButtonGraphic>();
        if (theme == null)
        {
            var icon = new GameObject("Themed settings icon", typeof(RectTransform), typeof(SettingsButtonGraphic));
            icon.transform.SetParent(rect, false);
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
            theme = icon.GetComponent<SettingsButtonGraphic>();
            theme.raycastTarget = true;
        }
        pause_button.targetGraphic = theme;
        var colors = pause_button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.35f, 1.3f);
        colors.pressedColor = new Color(0.75f, 0.9f, 0.82f);
        colors.selectedColor = Color.white;
        pause_button.colors = colors;
        pause_button.onClick.AddListener(OnClickPauseButton);
        restart_button.onClick.AddListener(uiManager.OnClickRestartButton);
        continue_button.onClick.AddListener(() => GameManager.Instance.SetPaused(false));
    }
    protected override UIState GetUIState() => UIState.Pause;
    private void OnClickPauseButton()
    {
        var manager = GameManager.Instance;
        if (manager.Session != null) manager.SetPaused(!manager.Session.Paused);
    }
}
