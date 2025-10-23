using Unity.VisualScripting;
using UnityEngine;

public class MovePlate : MonoBehaviour
{
    public Position Pos {  get; private set; }

    public void Init(Position pos)
    {
        Pos = pos;
    }

    private void OnMouseUpAsButton()
    {
        GameManager.Instance.board.OnClickMovePlate(this);
    }
}
