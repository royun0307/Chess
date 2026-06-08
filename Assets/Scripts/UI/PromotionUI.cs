using System;
using UnityEngine;
using UnityEngine.UI;

// 폰 승급 시 어떤 기물로 바꿀지 선택하는 UI 클래스
public class PromotionUI : BaseUI
{
    // 백색 나이트 스프라이트
    public Sprite white_knight;
    // 흑색 나이트 스프라이트
    public Sprite black_knight;

    // 백색 비숍 스프라이트
    public Sprite white_bishop;
    // 흑색 비숍 스프라이트
    public Sprite black_bishop;

    // 백색 룩 스프라이트
    public Sprite white_rook;
    // 흑색 룩 스프라이트
    public Sprite black_rook;

    // 백색 퀸 스프라이트
    public Sprite white_queen;
    // 흑색 퀸 스프라이트
    public Sprite black_queen;

    // 나이트 선택 버튼이 붙어 있는 오브젝트
    public GameObject knight_image;
    // 비숍 선택 버튼이 붙어 있는 오브젝트
    public GameObject bishop_image;
    // 룩 선택 버튼이 붙어 있는 오브젝트
    public GameObject rook_image;
    // 퀸 선택 버튼이 붙어 있는 오브젝트
    public GameObject quenn_image;

    // 승급 기물이 선택되었을 때 호출되는 이벤트
    public event Action<PieceType> select_promotion;

    // UI 초기화 점수
    // 각 버튼 클릭 이벤트를 승급 처리 함수와 연결한다
    public override void Init(UIManager uiManager)
    {
        base.Init(uiManager);

        knight_image.GetComponent<Button>().onClick.AddListener(OnClickKnightPromotion);
        bishop_image.GetComponent<Button>().onClick.AddListener(OnClickBishopPromotion);
        rook_image.GetComponent<Button>().onClick.AddListener(OnClickRookPromotion);
        quenn_image.GetComponent<Button>().onClick.AddListener(OnClickQuennPromotion);
    }

    // 이 UI가 담당하는 상태는 Promotion
    protected override UIState GetUIState()
    {
        return UIState.Promotion;
    }

    // 현재 턴의 플레이이 색상에 맞게 승급 UI 이미지를 설정
    public void SetUI()
    {
        if(GameManager.Instance.state.CurrentPlayer == PlayerColor.White)
        {
            knight_image.GetComponent<Image>().sprite = white_knight;
            bishop_image.GetComponent<Image>().sprite = white_bishop;
            rook_image.GetComponent<Image>().sprite = white_rook;
            quenn_image.GetComponent<Image>().sprite = white_queen;
        }
        else if(GameManager.Instance.state.CurrentPlayer == PlayerColor.Black)
        {
            knight_image.GetComponent<Image>().sprite = black_knight;
            bishop_image.GetComponent<Image>().sprite = black_bishop;
            rook_image.GetComponent<Image>().sprite = black_rook;
            quenn_image.GetComponent<Image>().sprite = black_queen;
        }
    }

    // UI 활성화 상태를 설정
    // 활성화/비활성화가 이루어진 뒤 이전에 등록된 승급 이벤트를 초기화한다
    public override void SetActive(UIState state)
    {
        base.SetActive(state);
        ResetAction();
    }

    // 나이트 승급 버튼 클릭 시 호출
    private void OnClickKnightPromotion()
    {
        select_promotion?.Invoke(PieceType.Knight);
        uiManager.ChangeState(UIState.None);
    }

    // 비숍 승급 버튼 클릭 시 호출
    private void OnClickBishopPromotion()
    {
        select_promotion?.Invoke(PieceType.Bishop);
        uiManager.ChangeState(UIState.None);
    }

    // 룩 승급 버튼 클릭 시 호출
    private void OnClickRookPromotion() 
    {
        select_promotion?.Invoke(PieceType.Rook);
        uiManager.ChangeState(UIState.None);
    }

    // 퀸 승급 버튼 클릭 시 호출
    private void OnClickQuennPromotion()
    {
        select_promotion?.Invoke(PieceType.Queen);
        uiManager.ChangeState(UIState.None);
    }

    // 등록된 승급 이벤트를 초기화
    public void ResetAction()
    {
        select_promotion = null;
    }
}
