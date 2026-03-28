public class Castle : Move
{
    // 이 이동이 어떤 종류의 이동인지 나타냄
    // 예: 킹사이드 캐슬링, 퀸사이드 캐슬링
    public override MoveType Type { get; }

    // 킹이 원래 있던 위치
    public override Position FromPos {  get; }
    
    // 캐슬링 후 킹이 도착할 위치
    public override Position ToPos { get; }

    // 킹이 이동하는 방향
    // 킹사이드면 오른쪽(East), 퀸사이드면 왼쪽(West)
    private readonly Direction king_move_dir;
    
    // 캐슬리힐 때 룩이 원래 있던 위치
    private readonly Position rook_from_pos;

    // 캐슬링할 때 룩이 이동할 위치
    private readonly Position rook_to_pos;

    public Castle(MoveType type, Position king_pos)
    {
        // 캐슬링 종류 저장
        Type = type;

        // 킹의 시작 위치 저장
        FromPos = king_pos;
        
        // 킹사이드 캐슬링인 경우
        if(type == MoveType.CastleKS)
        {
            // 킹은 오르쪽으로 이동
            king_move_dir = Direction.East;
            
            // 킹은 g열(6)로 이동
            ToPos = new Position(king_pos.row, 6);
            
            // 룩은 h열(7)에서 시작
            rook_from_pos = new Position(king_pos.row, 7);
            
            // 룩은 f열(5) 이동
            rook_to_pos = new Position(king_pos.row, 5);
        }
        // 퀸사이드 캐슬링인 경우
        else if(type == MoveType.CastleQS)
        {
            // 킹은 왼쪽으로 이동
            king_move_dir = Direction.West;

            // 킹은 c열(2)로 이동
            ToPos = new Position(king_pos.row, 2);
            
            // 룩은 a열(0)에서 시작
            rook_from_pos = new Position(king_pos.row, 0);
            
            // 룩은 d열(3)로 이동
            rook_to_pos = new Position(king_pos.row, 3);
        }
    }

    public override bool Execute(Board board)
    {
        // 먼저 킹을 이동시킴
        new NormalMove(FromPos, ToPos).Execute(board);

        // 그 다음 룩을 이동시킴
        new NormalMove(rook_from_pos, rook_to_pos).Execute(board);

        // 반환값은 "잡기 또는 폰 이동이 있었는가" 여부
        // 캐슬링 잡기와 폰 이동이 없으므로 false 반환
        return false;
    }

    public override bool IsLegal(Board board)
    {
        // 현재 캐슬링을 시도하는 플레이어 색상
        PlayerColor player = board[FromPos].Color;

        // 현재 킹이 체크 상태라면 캐슬링 불가능
        if (board.IsInCheck(player))
        {
            return false;
        }

        // 실제 보드에 영향을 주지 않기 위해 복사본 사용
        Board copy = board.Copy();

        // 복사본에서 킹의 현재 위치 추적
        Position king_pos_in_copy = FromPos;

        // 캐슬링 시 킹은 2칸 이동하므로
        // 중간 칸과 최종칸이 모두 체크 상태가 아니어야 함
        for (int i = 0; i < 2; i++)
        {
            // 킹을 한 칸 씩 이동
            new NormalMove(king_pos_in_copy, king_pos_in_copy + king_move_dir).Execute(copy);
            
            // 킹 위치 갱신
            king_pos_in_copy += king_move_dir;

            // 이동한 칸에서 체크 상태라면 캐슬링 불가능
            if (copy.IsInCheck(player))
            {
                return false;
            }
        }

        // 현재 체크 상태도 아니고,
        // 지나가는 칸과 도착 칸도 안전하므로 캐슬링 가능
        return true;
    }
}
