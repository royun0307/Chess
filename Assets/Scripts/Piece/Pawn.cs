public class Pawn : Piece
{
    public override PieceType Type => PieceType.Pawn;

    public override PlayerColor Color { get; }

    public Pawn(PlayerColor color)
    {
        this.Color = color;
    }

    public override Piece Copy()
    {
        Pawn copy = new Pawn(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }
}
