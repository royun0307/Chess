public class Enpassant : Move
{
    // 이 이동의 종류는 앙파상
    public override MoveType Type => MoveType.EnPassant;
    
    // 이동하는 폰의 시작 위치
    public override Position FromPos { get; }
    
    // 이동하는 폰의 도착 위치
    public override Position ToPos { get; }

    // 실제로 잡히는 상대 폰의 위치
    // 앙파상은 도착 칸의 말이 아니라,
    // 옆 칸을 지나쳐 간 상대 폰을 잡는 특수 규칙이므로 따로 저장함
    private readonly Position capture_pos;

    public Enpassant(Position from, Position to)
    {
        // 시작 위치 저장
        FromPos = from;
        
        // 도착 위치 저장
        ToPos = to;
        
        // 앙파상으로 잡히는 폰의 위치 계산
        // 이동하는 폰은 대각선 앞으로 가지만,
        // 실제 잡히는 폰은 출발 행(from.row)에 있고 도착 열(to.column)에 있음
        // 예: 백 폰이 (3,4)에서 (2,5)로 앙파상하면
        // 잡히는 상대 폰은 (3,5)에 있음
        capture_pos = new Position(from.row, to.column);
    }

    public override bool Execute(Board board)
    {
        // 먼저 내 폰을 목적지로 이동
        new NormalMove(FromPos, ToPos).Execute(board);
        
        // 앙파상으로 잡히는 폰 제거
        // 도착 칸에 있는 말이 아니라 capture_pos 위치의 말을 제거해야 함
        board[capture_pos] = null;

        // 반환값은 "잡기 또는 폰 이동이 있었는가" 여부
        // 앙파상은 폰 이동이면서 동시에 잡기이므로 true 반환
        return true;
    }
}
