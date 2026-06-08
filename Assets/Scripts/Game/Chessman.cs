using UnityEngine;

// 이 스크립가 붙은 오브젝트에는 반드시 Collider2D가 있어야 함
// 없으면 Unity가 자동으로 추가해줌
[RequireComponent(typeof(Collider2D))]
public class Chessman : MonoBehaviour
{
    // 현재 체스말의 보드 상 위치
    // 외부에서는 읽기만 가능
    public Position Pos { get; private set; }

    // 체스말의 시작 위치를 초기화
    public void Init(Position startPos)
    {
        Pos = startPos;
    }

    // 체스말의 보드 위치를 새로운 위치로 변경하는 함수
    public void SetBoardPos(Position newPos)
    {
        Pos = newPos;
    }

    // 마우스로 이 오브젝트를 클릭했다가 땠을 때 호출되는 Unity 이벤트 함수
    private void OnMouseUpAsButton()
    {
        // GameManager.Instance가 없으면 에러 출력 후 종료
        if (GameManager.Instance == null)
        {
            Debug.LogError("BoardManager instance is null!");
            return;
        }

        // 현재 클릭된 체스말 정보를 board에 전달
        GameManager.Instance.board.OnClickChessman(this);
    }
}
