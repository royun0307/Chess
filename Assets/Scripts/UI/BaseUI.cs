using UnityEngine;

public abstract class BaseUI : MonoBehaviour
{
    protected UIManager uiManager;

    public virtual void Init(UIManager uiManager)
    {
        this.uiManager = uiManager;
    }

    protected abstract UIState GetUIState();

    public virtual void SetActive(UIState state)
    {
        gameObject.SetActive(GetUIState() == state);
    }
}
