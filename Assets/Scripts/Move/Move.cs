public enum MoveType
{
    Normal,
    CastleKS,
    CastleQS,
    DoublePawn,
    EnPassant,
    PawnPromotion
}

public abstract class Move
{
    public abstract MoveType Type { get; }
    public abstract Position FromPos { get; }
    public abstract Position ToPos { get; }
    
    public abstract void Execute(Board board);

    public virtual bool IsLegal(Board board)
    {
        PlayerColor player = board[FromPos].Color;
        Board board_copy = board.Copy();
        Execute(board_copy);

        return !board_copy.IsInCheck(player);
    }
}
