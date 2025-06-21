using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{

    public Color color;

    public PieceName pieceName = PieceName.Pawn;

    public Pair<int, int> pos = new Pair<int, int>(-1, -1);

    public override List<Vector2Int> GetAvailableMoves(Piece[,] board, Vector2Int pos)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int direction = color == Color.White ? 1 : -1;

        // 전진 1칸
        Vector2Int forward = new Vector2Int(pos.x, pos.y + direction);
        if (IsInsideBoard(forward) && board[forward.x, forward.y] == null)
            moves.Add(forward);

        // 전진 2칸 (첫 이동일 경우)
        if ((color == Color.White && pos.y == 1) || (color == Color.Black && pos.y == 6))
        {
            Vector2Int doubleForward = new Vector2Int(pos.x, pos.y + 2 * direction);
            if (board[forward.x, forward.y] == null && board[doubleForward.x, doubleForward.y] == null)
                moves.Add(doubleForward);
        }

        // 대각선 잡기
        Vector2Int[] diagonals = {
            new Vector2Int(pos.x - 1, pos.y + direction),
            new Vector2Int(pos.x + 1, pos.y + direction)
        };

        foreach (var diag in diagonals)
        {
            if (IsInsideBoard(diag) && board[diag.x, diag.y] != null &&
                board[diag.x, diag.y].color != this.color)
            {
                moves.Add(diag);
            }
        }

        return moves;
    }

    private bool IsInsideBoard(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }
}
