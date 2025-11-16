public class Enpassant : Move
{
    public override MoveType Type => MoveType.EnPassant;
    public override Position FromPos { get; }
    public override Position ToPos { get; }

    private readonly Position capture_pos;

    public Enpassant(Position from, Position to)
    {
        FromPos = from;
        ToPos = to;
        capture_pos = new Position(from.row, to.column);
    }

    public override void Execute(Board board)
    {
        new NormalMove(FromPos, ToPos).Execute(board);
        board[capture_pos] = null;
    }
}
