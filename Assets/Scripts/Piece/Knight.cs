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
}
