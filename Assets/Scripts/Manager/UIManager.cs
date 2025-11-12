using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public enum UIState
{
    None,
    Result,
    Promotion
}

public class UIManager : MonoBehaviour
{
    static UIManager instance;
    public static UIManager Instance { get { return instance; } }

    UIState currentState = UIState.None;

    public ResultUI resultUI = null;
    public PromotionUI promotionUI = null;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;

            resultUI?.Init(this);
            promotionUI?.Init(this);

            ChangeState(currentState);
        }
        else
        {
            Destroy(this);
        }
    }

    public void ChangeState(UIState state)
    {
        currentState = state;
        resultUI?.SetActive(currentState);
        promotionUI?.SetActive(currentState);
    }

    public void OnClickRestartButton()
    {
        GameManager.Instance.board.Init();
    }
    public void OnClickExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}