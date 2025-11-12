using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultUI : BaseUI
{
    public TextMeshProUGUI winner_text;
    public TextMeshProUGUI result_text;

    public Button restart_button;
    public Button exit_button;

    protected override UIState GetUIState()
    {
        return UIState.Result;
    }

    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);

        restart_button.onClick.AddListener(OnClickRestartButton);
        exit_button.onClick.AddListener(OnClickExitButton);
    }

    public void SetUI(PlayerColor player, EndReason endReason)
    {
        winner_text.text = player.ToString();
        result_text.text = endReason.ToString();
    }

    void OnClickRestartButton()
    {
        uiManager.OnClickRestartButton();
        uiManager.ChangeState(UIState.None);
    }

    void OnClickExitButton()
    {
        uiManager.OnClickExit();
        uiManager.ChangeState(UIState.None);
    }
}