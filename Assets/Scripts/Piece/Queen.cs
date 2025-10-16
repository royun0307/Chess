public class Queen : Piece
{
    public override PieceType Type => PieceType.Queen;

    public override PlayerColor Color { get; }

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
}
