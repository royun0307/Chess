public class King : Piece
{
    public override PieceType Type => PieceType.King;

    public override PlayerColor Color { get; }

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
}
