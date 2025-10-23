using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Chessman : MonoBehaviour
{
    public Position Pos { get; private set; }

    public void Init(Position startPos)
    {
        Pos = startPos;
    }

    public void SetBoardPos(Position newPos)
    {
        Pos = newPos;
    }

    private void OnMouseUpAsButton()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("BoardManager instance is null!");
            return;
        }
        GameManager.Instance.board.OnClickChessman(this);
    }
}
