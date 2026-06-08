using UnityEngine;

// 모든 UI의 공통 기능을 정의하는 추상 클래스
public abstract class BaseUI : MonoBehaviour
{
    // 이 UI를 관리하는 UIManager 참조
    protected UIManager uiManager;

    // UI 초기화 함수
    // UIManager를 전달받아 저장한
    public virtual void Init(UIManager uiManager)
    {
        this.uiManager = uiManager;
    }

    // 이 UI가 어떤 UIState에 해당하는지 자식 클래스에서 정의
    protected abstract UIState GetUIState();

    // 현재 상태(state)에 따라 UI 활성화 여부를 설정
    public virtual void SetActive(UIState state)
    {
        // 현재 UI의 상태와 전달된 상태가 같으면 활성화, 다르면 비활성화
        gameObject.SetActive(GetUIState() == state);
    }
}
