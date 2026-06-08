using UnityEngine.UI;

// 일시정지 화면을 담당하는 UI 클래스
public class PauseUI : BaseUI
{
    // 일시정지 버튼
    public Button pause_button;
    
    // 재시작 버튼
    public Button restart_button;
    
    // 계속하기 버튼
    public Button continue_button;

    // UI 초기화 버튼
    // 버튼 클릭 이벤트를 각각의 처리 함수와 연결한다
    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);

        pause_button.onClick.AddListener(OnClickPauseButton);
        restart_button.onClick.AddListener(OnClickRestartButton);
        continue_button.onClick.AddListener(OnClickContinueButton);
    }

    // 이 UI가 담당하는 상태는 Pause
    protected override UIState GetUIState()
    {
        return UIState.Pause;
    }

    // 현재 상태에 따라 UI 활성화 여부를 설정
    // Pause 상태가 들어오면 토글 방식으로 켜고 끄며,
    // 다른 상태가 들어오면 무조건 비활성화한다
    public override void SetActive(UIState uIState)
    {
        // 현재 오브젝트가 활성화되어 있는지 확인
        bool isActive = gameObject.activeSelf;

        // 전달된 상태가 Pause 상태라면 현재 상태를 반전시켜 토글
        if (uIState == GetUIState())
        {
            gameObject.SetActive(!isActive);
        }
        else
        {
            // Pause 상태가 아니면 비활성화
            gameObject.SetActive(false);
        }
    }

    // 일시정지 버튼 클릭 시 호출
    // Pause UI를 토글한다
    void OnClickPauseButton()
    {
        SetActive(UIState.Pause);
    }

    // 재시작 버튼 클릭 시 호출
    // 게임을 재시작하고 UI 상태를 None으로 변경한다
    void OnClickRestartButton()
    {
        uiManager.OnClickRestartButton();
        uiManager.ChangeState(UIState.None);
    }

    // 계속하기 버튼 클릭 시 호출
    // Pause UI를 닫고 일반 상태로 돌아간다
    void OnClickContinueButton()
    {
        uiManager.ChangeState(UIState.None);
    }
}
