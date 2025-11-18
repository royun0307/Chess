public class Castle : Move
{
    public override MoveType Type { get; }
    public override Position FromPos {  get; }
    public override Position ToPos { get; }

    private readonly Direction king_move_dir;
    private readonly Position rook_from_pos;
    private readonly Position rook_to_pos;

    public Castle(MoveType type, Position king_pos)
    {
        Type = type;
        FromPos = king_pos;
        
        if(type == MoveType.CastleKS)
        {
            king_move_dir = Direction.East;
            ToPos = new Position(king_pos.row, 6);
            rook_from_pos = new Position(king_pos.row, 7);
            rook_to_pos = new Position(king_pos.row, 5);
        }
        else if(type == MoveType.CastleQS)
        {
            king_move_dir = Direction.West;
            ToPos = new Position(king_pos.row, 2);
            rook_from_pos = new Position(king_pos.row, 0);
            rook_to_pos = new Position(king_pos.row, 3);
        }
    }

    public override bool Execute(Board board)
    {
        new NormalMove(FromPos, ToPos).Execute(board);
        new NormalMove(rook_from_pos, rook_to_pos).Execute(board);

        return false;
    }

    public override bool IsLegal(Board board)
    {
        PlayerColor player = board[FromPos].Color;

        if (board.IsInCheck(player))
        {
            return false;
        }

        Board copy = board.Copy();
        Position king_pos_in_copy = FromPos;

        for (int i = 0; i < 2; i++)
        {
            new NormalMove(king_pos_in_copy, king_pos_in_copy + king_move_dir).Execute(copy);
            king_pos_in_copy += king_move_dir;

            if (copy.IsInCheck(player))
            {
                return false;
            }
        }

        return true;
    }
}
