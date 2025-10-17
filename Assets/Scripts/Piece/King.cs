using System.Collections.Generic;
using System.Linq;

public class King : Piece
{
    public override PieceType Type => PieceType.King;
    public override PlayerColor Color { get; }

    private static readonly Direction[] dirs = new Direction[]
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    public King(PlayerColor color)
    {
        this.Color = color;
    }

    public override Piece Copy()
    {
        King copy = new King(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }

    private IEnumerable<Position> MovePositions(Position from, Board board)
    {
        foreach (Direction dir in dirs)
        { 
            Position to = from + dir;

            if (!Board.IsInside(to))
            {
                continue;
            }

            if(board.IsEmpty(to) || board[to].Color != Color)
            {
                yield return to;
            }
        }
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        foreach (Position to in MovePositions(from, board))
        {
            yield return new NormalMove(from, to);
        }
    }
}
