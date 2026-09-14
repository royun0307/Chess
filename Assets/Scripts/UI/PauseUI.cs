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
