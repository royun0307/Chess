using UnityEngine;
using TMPro;

// 모든 UI의 공통 기능을 정의하는 추상 클래스
public abstract class BaseUI : MonoBehaviour
{
    // 이 UI를 관리하는 UIManager 참조
    protected UIManager uiManager;
    private Vector3 originalScale;
    private Canvas canvas;

    // UI 초기화 함수
    // UIManager를 전달받아 저장한
    public virtual void Init(UIManager uiManager)
    {
        this.uiManager = uiManager;
        canvas = GetComponentInParent<Canvas>();
        originalScale = transform.localScale;
        foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            // Several legacy labels use a font taller than their 50px text box.
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(14, text.fontSize);
            text.enableAutoSizing = true;
        }
    }

    private void LateUpdate()
    {
        if (canvas == null) return;
        // The wrapper is small and some text boxes extend beyond the background.
        // Measure all descendants relative to the wrapper so prior scaling cancels out.
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(transform);
        float width = 2 * Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)) * canvas.scaleFactor * Mathf.Abs(originalScale.x);
        float height = 2 * Mathf.Max(Mathf.Abs(bounds.min.y), Mathf.Abs(bounds.max.y)) * canvas.scaleFactor * Mathf.Abs(originalScale.y);
        if (width <= 0 || height <= 0) return;
        float fit = Mathf.Clamp01(Mathf.Min((Screen.width - 24f) / width, (Screen.height - 24f) / height));
        transform.localScale = new Vector3(originalScale.x * fit, originalScale.y * fit, originalScale.z);
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
