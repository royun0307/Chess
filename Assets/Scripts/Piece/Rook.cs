using System.Collections.Generic;
using System.Linq;

public class Rook : Piece
{
    public override PieceType Type => PieceType.Rook;
    public override PlayerColor Color { get; }

    private static readonly Direction[] dirs = new Direction[]
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };

    public Rook(PlayerColor color)
    {
        this.Color = color;
    }

    public override Piece Copy()
    {
        Rook copy = new Rook(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
    }
}
