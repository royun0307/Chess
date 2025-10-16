public class Rook : Piece
{
    public override PieceType Type => PieceType.Rook;

    public override PlayerColor Color { get; }

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
}
