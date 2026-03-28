public class NormalMove : Move
{
    // 이 이동의 종류는 일반 이동
    public override MoveType Type => MoveType.Normal;

    // 말이 시작하는 위치
    public override Position FromPos { get; }

    // 말이 도착하는 위치
    public override Position ToPos { get; }

    public NormalMove(Position from, Position to)
    {
        // 시작 위치 저장
        FromPos = from;

        // 도착 위치 저장
        ToPos = to;
    }

    public override bool Execute(Board board)
    {
        // 시작 위치에 있는 말을 가져옴
        Piece piece = board[FromPos];

        // 도착 위치에 말이 있으면 잡기(capture) 발생
        bool capture = !board.IsEmpty(ToPos);

        // 말을 도착 위치로 이동
        board[ToPos] = piece;

        // 원래 위치는 비움
        board[FromPos] = null;

        // 한 번이라도 움직였음을 기록
        // 캐슬링, 폰의 첫 2칸 전진 가능 여부 같은 판정에 사용될 수 있음
        piece.hasMoved = true;

        // 반환값:
        // 1. 상대 말을 잡았거나
        // 2. 이동한 말이 폰이라면 true
        //
        // 보통 체스에서는 "잡기"나 "폰 이동"이 있었으면
        // 50수 규칙 카운트 초기화 등에 사용 가능
        return capture || piece.Type == PieceType.Pawn;
    }
}
