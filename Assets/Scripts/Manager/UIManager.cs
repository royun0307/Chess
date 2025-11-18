using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public enum UIState
{
    None,
    Result,
    Promotion,
    Pause
}

public class UIManager : MonoBehaviour
{
    static UIManager instance;
    public static UIManager Instance { get { return instance; } }

    UIState currentState = UIState.None;

    public ResultUI resultUI = null;
    public PromotionUI promotionUI = null;
    public PauseUI pauseUI = null;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;

            resultUI?.Init(this);
            promotionUI?.Init(this);
            pauseUI?.Init(this);

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
        pauseUI?.SetActive(currentState);
    }

    public void OnClickRestartButton()
    {
        GameManager.Instance.RestartGame();
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