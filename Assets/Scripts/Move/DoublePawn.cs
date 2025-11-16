public class DoublePawn : Move
{
    public override MoveType Type => MoveType.DoublePawn;
    public override Position FromPos { get; }
    public override Position ToPos {  get; }

    private readonly Position skipped_pos;

    public DoublePawn(Position from, Position to)
    {
        FromPos = from;
        ToPos = to;
        skipped_pos = new Position((from.row + to.row) / 2, from.column);
    }

    public override void Execute(Board board)
    {
        PlayerColor player = board[FromPos].Color;
        board.SetPawnSkipPosition(player, skipped_pos);
        new NormalMove(FromPos, ToPos).Execute(board);
    }
}
