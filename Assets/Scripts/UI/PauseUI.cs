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
        restart_button.onClick.AddListener(OnClickRestartButton);
        continue_button.onClick.AddListener(OnClickContinueButton);
    }

    protected override UIState GetUIState()
    {
        return UIState.Pause;
    }

    public override void SetActive(UIState uIState)
    {
        bool isActive = gameObject.activeSelf;
        if (uIState == GetUIState())
        {
            gameObject.SetActive(!isActive);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void OnClickPauseButton()
    {
        SetActive(UIState.Pause);
    }

    void OnClickRestartButton()
    {
        uiManager.OnClickRestartButton();
        uiManager.ChangeState(UIState.None);
    }

    void OnClickContinueButton()
    {
        uiManager.ChangeState(UIState.None);
    }
}
