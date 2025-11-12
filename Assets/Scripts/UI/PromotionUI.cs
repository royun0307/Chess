using System;
using UnityEngine;
using UnityEngine.UI;

public class PromotionUI : BaseUI
{
    public Sprite white_knight;
    public Sprite black_knight;

    public Sprite white_bishop;
    public Sprite black_bishop;

    public Sprite white_rook;
    public Sprite black_rook;

    public Sprite white_queen;
    public Sprite black_queen;

    public GameObject knight_image;
    public GameObject bishop_image;
    public GameObject rook_image;
    public GameObject quenn_image;

    public event Action<PieceType> select_promotion;

    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);

        knight_image.GetComponent<Button>().onClick.AddListener(OnClickKnightPromotion);
        bishop_image.GetComponent<Button>().onClick.AddListener(OnClickBishopPromotion);
        rook_image.GetComponent<Button>().onClick.AddListener(OnClickRookPromotion);
        quenn_image.GetComponent<Button>().onClick.AddListener(OnClickQuennPromotion);
    }

    protected override UIState GetUIState()
    {
        return UIState.Promotion;
    }

    public void SetUI()
    {
        if(GameManager.Instance.state.CurrnetPlayer == PlayerColor.White)
        {
            knight_image.GetComponent<Image>().sprite = white_knight;
            bishop_image.GetComponent<Image>().sprite = white_bishop;
            rook_image.GetComponent<Image>().sprite = white_rook;
            quenn_image.GetComponent<Image>().sprite = white_queen;
        }
        else if(GameManager.Instance.state.CurrnetPlayer == PlayerColor.Black)
        {
            knight_image.GetComponent<Image>().sprite = black_knight;
            bishop_image.GetComponent<Image>().sprite = black_bishop;
            rook_image.GetComponent<Image>().sprite = black_rook;
            quenn_image.GetComponent<Image>().sprite = black_queen;
        }
    }

    public override void SetActive(UIState state)
    {
        base.SetActive(state);
        ResetAction();
    }

    private void OnClickKnightPromotion()
    {
        select_promotion?.Invoke(PieceType.Knight);
        uiManager.ChangeState(UIState.None);
    }

    private void OnClickBishopPromotion()
    {
        select_promotion?.Invoke(PieceType.Bishop);
        uiManager.ChangeState(UIState.None);
    }

    private void OnClickRookPromotion() 
    {
        select_promotion?.Invoke(PieceType.Rook);
        uiManager.ChangeState(UIState.None);
    }

    private void OnClickQuennPromotion()
    {
        select_promotion?.Invoke(PieceType.Queen);
        uiManager.ChangeState(UIState.None);
    }

    public void ResetAction()
    {
        select_promotion = null;
    }
}
