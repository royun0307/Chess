// 체스에서 가능한 이동 종류를 구분하는 열거형
public enum MoveType
{
    Normal,         // 일반 이동
    CastleKS,       // 킹사이드 캐슬링
    CastleQS,       // 퀸사이드 캐슬링
    DoublePawn,     // 폰의 첫 두 칸 전진
    EnPassant,      // 앙파상
    PawnPromotion   // 폰 승급
}

public abstract class Move
{
    // 이동의 종류
    public abstract MoveType Type { get; }
    
    // 이동 시작 위치
    public abstract Position FromPos { get; }
    
    // 이동 도착 위치
    public abstract Position ToPos { get; }
    
    // 실제 보드에 이동을 적용하는 함수
    // 반환값은 보통 "잡기 또는 폰 이동 여부" 같은
    // 추가 상태 생신 판단에 사용할 수 있음
    public abstract bool Execute(Board board);

    // 기본적인 이동 가능 여부 검사
    // 자식 클래스에서 필요하면 override 해서
    // 특수 규칙(캐슬링, 앙파상, 승급 등)을 추가로 검사할 수 있음
    public virtual bool IsLegal(Board board)
    {
        // 이동하려는 말의 색상 확인
        PlayerColor player = board[FromPos].Color;

        // 원본 보드를 건드리지 않기 위해 복사본 생성
        Board board_copy = board.Copy();

        // 복사본에 이동을 실제로 적용
        Execute(board_copy);

        // 이동 후 자기 킹이 체크 상태라면 이동 불가
        // 체크 상태가 아니여야 합법적인 이동
        return !board_copy.IsInCheck(player);
    }
}
