using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

// 현재 어떤 UI 화면이 활성화되어 있는지 나타내는 상태값
public enum UIState
{
    None,       // 아무 UI도 표시하지 않는 기본 상태
    Result,     // 게임 결과 UI 상태
    Promotion,  // 폰 승급 선택 UI 상태
    Pause       // 일시정지 UI 상태
}

// 게임 내 UI 전체 상태를 관리하는 매니저
public class UIManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    static UIManager instance;

    // 외부에서 접근할 수 있는 싱글톤 프로퍼티
    public static UIManager Instance { get { return instance; } }

    // 현재 UI 상태
    UIState currentState = UIState.None;

    // 각 UI 참조
    public ResultUI resultUI = null;
    public PromotionUI promotionUI = null;
    public PauseUI pauseUI = null;

    private void Awake()
    {
        // 아직 인스턴스가 없으면 현재 객체를 싱글톤으로 등록
        if(instance == null)
        {
            instance = this;

            // 각 UI 초기화
            resultUI?.Init(this);
            promotionUI?.Init(this);
            pauseUI?.Init(this);

            // 현재 상태에 맞게 UI 활성/비활성 반영
            ChangeState(currentState);
        }
        else
        {
            // 이미 인스턴스가 있으면 중복 생성이므로 제거
            Destroy(this);
        }
    }

    // UI 상태를 변경하고, 각 UI에 현재 상태를 전달
    public void ChangeState(UIState state)
    {
        currentState = state;
        
        // 각 UI가 자신의 상태에 맞게 활성/비활성 처리하도록 호출
        resultUI?.SetActive(currentState);
        promotionUI?.SetActive(currentState);
        pauseUI?.SetActive(currentState);
    }

    // 다시 시작 버튼 클릭시 호출
    public void OnClickRestartButton()
    {
        GameManager.Instance.RestartGame();
    }

    // 종료 버튼 클릭 시 호출
    public void OnClickExit()
    {
#if UNITY_EDITOR
        // 에디터에서는 플레이 모드 종료
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 빌드된 게임에서는 애플리케이션 종료
        Application.Quit();
#endif
    }
}