using System.Collections.Generic;
using UnityEngine;

public enum Color
{
    White,
    Black,
} 

public enum PieceName
{
    Pawn,
    Night,
    Bishop,
    Rook,
    Queen,
    King
}

public abstract class Piece : MonoBehaviour
{
    public Color color;
    public PieceName pieceName;
    public Vector2Int pos;

    public abstract List<Vector2Int> GetAvailableMoves(Piece[,] board, Vector2Int pos);

    public virtual void MoveTo(Vector2Int newPos)
    {
        pos = newPos;
        transform.position = new Vector3(newPos.x, 0, newPos.y); // 3D 기준 예시
    }
    public void Death() { }
}
