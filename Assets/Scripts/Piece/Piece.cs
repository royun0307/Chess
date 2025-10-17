using System.Collections;
using System.Collections.Generic;
using System.Linq;


public abstract class Piece
{
    public abstract PieceType Type { get; }
    public abstract PlayerColor Color { get; }
    public bool hasMoved { get; set; } = false;

    public abstract Piece Copy();

    public abstract IEnumerable<Move> GetMoves(Position from, Board board);

    protected IEnumerable<Position> MovePositionInDir(Position from, Board board, Direction dir)
    {
        for (Position pos = from + dir; Board.IsInside(pos); pos += dir)
        {
            if (board.IsEmpty(pos))
            {
                yield return pos;
                continue;
            }

            Piece piece = board[pos];

            if (piece.Color != Color)
            {
                yield return pos;
            }
            yield break;
        }
    }

    protected IEnumerable<Position> MovePositionsInDirs(Position from, Board board, Direction[] dirs)
    {
        return dirs.SelectMany(dir => MovePositionInDir(from, board, dir));
    }
}

