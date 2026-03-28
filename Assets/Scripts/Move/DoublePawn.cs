public class DoublePawn : Move
{
    // 이 이동의 종류는 폰의 두 칸 전진
    public override MoveType Type => MoveType.DoublePawn;
    
    // 폰이 출발한 위치
    public override Position FromPos { get; }
    
    // 폰이 도착할 위치
    public override Position ToPos {  get; }

    // 폰이 두 칸 이동하면서 중간에 지나친 칸
    // 앙파상 가능 여부를 기록할 때 사용됨
    private readonly Position skipped_pos;

    public DoublePawn(Position from, Position to)
    {
        // 시작 위치 저장
        FromPos = from;

        // 도착 위치 저장
        ToPos = to;

        // 두 칸 이동 중간의 위치 계산
        // 예: (6, 4) -> (4, 4) 라면 중간 칸은 (5, 4)
        skipped_pos = new Position((from.row + to.row) / 2, from.column);
    }

    public override bool Execute(Board board)
    {
        // 이동하는 폰의 색상 확인
        PlayerColor player = board[FromPos].Color;

        // 이 폰이 이번 턴에 두 칸 전진하면서 지나친 칸을 기록
        // 상대 폰이 다음 턴에 앙파상할 수 있도록 저장하는 용도
        board.SetPawnSkipPosition(player, skipped_pos);

        // 실제로 폰을 시작 위치에서 도착 위치로 이동
        new NormalMove(FromPos, ToPos).Execute(board);

        // 반환값은 "잡기 또는 폰 이동이 있었는가" 여부
        // 더블폰은 폰 이동이므로 true 반환
        return true;
    }
}
