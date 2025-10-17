using System.Collections.Generic;
using System.Linq;

public class Knight : Piece
{
    public override PieceType Type => PieceType.Knight;
    public override PlayerColor Color { get; }

    public Knight(PlayerColor color)
    {
        this.Color = color;
    }

    public override Piece Copy()
    {
        Knight copy = new Knight(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }

    private static IEnumerable<Position> PotentialToPosition(Position from)
    {
        foreach(Direction vDir in new Direction[] { Direction.North, Direction.South })
        {
            foreach(Direction hDir in new Direction[] { Direction.West, Direction.East })
            {
                yield return from + 2 * vDir + hDir;
                yield return from + vDir + 2 * hDir;
            }
        }
    }

    private IEnumerable<Position> MovePositions(Position from, Board board)
    {
        return PotentialToPosition(from).Where(pos => Board.IsInside(pos) 
            && (board.IsEmpty(pos) || board[pos].Color != Color));
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        return MovePositions(from, board).Select(to => new NormalMove(from, to));
    }
}
