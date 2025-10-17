using System.Collections.Generic;
using System.Linq;

public class Queen : Piece
{
    public override PieceType Type => PieceType.Queen;
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

    public Queen(PlayerColor color)
    {
        this.Color = color;
    }

    public override Piece Copy()
    {
        Queen copy = new Queen(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
    }
}
