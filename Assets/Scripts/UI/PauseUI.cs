using UnityEngine.UI;

public class PauseUI : BaseUI
{
    public Button pause_button;
    public Button restart_button;
    public Button continue_button;
    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);
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
