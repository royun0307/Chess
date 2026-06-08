using Unity.VisualScripting;
using UnityEngine;

public class MovePlate : MonoBehaviour
{
    //MovePlate 포지션
    public Position Pos {  get; private set; }

    // 생성자: 포지션 세팅
    public void Init(Position pos)
    {
        Pos = pos;
    }

    //마우스 클릭시
    private void OnMouseUpAsButton()
    {
        GameManager.Instance.board.OnClickMovePlate(this);
    }
}
