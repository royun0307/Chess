public class PawnPromotion : Move
{
    // 이 이동의 종류는 폰 승급
    public override MoveType Type => MoveType.PawnPromotion;
    
    // 승급 전 폰의 시작 위치
    public override Position FromPos { get; }

    // 승급 후 말이 놓일 위치
    public override Position ToPos { get; }

    // 승급할 말의 종류
    // 나이트, 비숍, 룩, 퀸 중 하나를 저장
    private readonly PieceType newType;

    public PawnPromotion(Position from, Position to, PieceType newType)
    {
        // 시작 위치를 저장
        FromPos = from;

        // 도착 위치 저장
        ToPos = to;

        // 어떤 말로 승급할지 저장
        this.newType = newType;
    }

    private Piece CreatePromotionPiece(PlayerColor color)
    {
        // 저장된 승급 타일에 따라 새로운 말 객체 생성
        switch (newType) 
        {
            case PieceType.Knight:
                return new Knight(color);
            case PieceType.Bishop:
                return new Bishop(color);
            case PieceType.Rook:
                return new Rook(color);

            // 기본값은 퀸
            // 잘못된 타입이 들어오거나 따로 처리되지 않은 경우 퀸으로 승급
            default:
                return new Queen(color);
        }
    }

    public override bool Execute(Board board)
    {
        // 원래 위치의 폰 가져오기
        Piece pawn = board[FromPos];

        // 기존 폰 제거
        board[FromPos] = null;

        // 폰의 색상을 유지한 채 승급할 새 말 생성
        Piece promotion_piece = CreatePromotionPiece(pawn.Color);
        
        // 새로 생성된 말은 이미 한 번 이동한 것으로 표시
        // 승급 직후의 말이므로 "처음 이동 전 상태"로 둘 필요가 없음
        promotion_piece.hasMoved = true;

        // 도착 위치에 승급한 말 배치
        board[ToPos] = promotion_piece;

        // 폰 이동은 반수 카운트 초기화 대상이므로 true 반환
        return true;
    }

    public PieceType GetPromotionPieceType()
    {
        // 어떤 말로 승급하는지 외부에서 확인할 수 있게 반환
        return newType;
    }
}
