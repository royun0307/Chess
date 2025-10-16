public class Bishop : Piece
{
    public override PieceType Type => PieceType.Bishop;

    public override PlayerColor Color { get; }

    public Bishop(PlayerColor color)
    {
        this.Color = color;
    }

    public override Piece Copy()
    {
        Bishop copy = new Bishop(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }
}
