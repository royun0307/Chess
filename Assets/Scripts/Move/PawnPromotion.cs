using System.Diagnostics;

public class PawnPromotion : Move
{
    public override MoveType Type => MoveType.PawnPromotion;
    public override Position FromPos { get; }
    public override Position ToPos { get; }

    private readonly PieceType newType;

    public PawnPromotion(Position from, Position to, PieceType newType)
    {
        FromPos = from;
        ToPos = to;
        this.newType = newType;
    }

    private Piece CreatePromotionPiece(PlayerColor color)
    {
        switch (newType) 
        {
            case PieceType.Knight:
                return new Knight(color);
            case PieceType.Bishop:
                return new Bishop(color);
            case PieceType.Rook:
                return new Rook(color);
            default:
                return new Queen(color);
        }
    }

    public override bool Execute(Board board)
    {
        Piece pawn = board[FromPos];
        board[FromPos] = null;

        Piece promotion_piece = CreatePromotionPiece(pawn.Color);
        promotion_piece.hasMoved = true;
        board[ToPos] = promotion_piece;

        return true;
    }

    public PieceType GetPromotionPieceType()
    {
        return newType;
    }
}
