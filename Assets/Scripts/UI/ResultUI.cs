using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게임 결과 화면을 담당하는 UI 클래스
public class ResultUI : BaseUI
{
    // 승리한 플레이어를 표시하는 텍스트
    public TextMeshProUGUI winner_text;

    // 게임 종료 사유를 표시하는 텍스트
    public TextMeshProUGUI result_text;

    // 다시 시작 버튼
    public Button restart_button;
    
    // 종료 버튼
    public Button exit_button;

    // 이 UI가 담당하는 상태는 Result
    protected override UIState GetUIState()
    {
        return UIState.Result;
    }

    // UI 초기화 점수
    // 버튼 클릭 이벤트를 각각의 처리 함수는 연결한다
    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);

        restart_button.onClick.AddListener(OnClickRestartButton);
        exit_button.onClick.AddListener(OnClickExitButton);
    }

    // 결과 UI에 승리한 플레이어와 종료 사유를 표시
    public void SetUI(PlayerColor player, EndReason endReason)
    {
        winner_text.text = player.ToString();
        result_text.text = endReason.ToString();
    }

    // 다시 시작 버튼 클릭 시 호출
    // 게임을 재시작하고 UI 상태를 None으로 변경한다
    void OnClickRestartButton()
    {
        uiManager.OnClickRestartButton();
        uiManager.ChangeState(UIState.None);
    }

    // 종료 버튼 클릭 시 호출
    // 게임을 종료하고 UI 상태를 None으로 변경한다
    void OnClickExitButton()
    {
        uiManager.OnClickExit();
        uiManager.ChangeState(UIState.None);
    }
}