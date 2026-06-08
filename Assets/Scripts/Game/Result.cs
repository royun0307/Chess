// 게임이 종료된 이유를 나타내는 열거형
public enum EndReason
{
    Checkmate,              // 체크메이트
    Stalemate,              // 스테일메이트
    FiftyMoveRule,          // 50수 룰
    InsufficientMaterial,   // 기물 부족
    ThreefoldRepetition     // 동일한 상태 3회 반복
};

// 게임 종료 경과를 나타내는 클래스
public class Result
{
    // 승리한 플레이어
    // 무승부라면 PlayerColor.None
    public PlayerColor Winner { get; }

    // 게임이 어떤 이유로 끝났는지 저장
    public EndReason EndReason { get; }

    // 결과 객체 생성자
    public Result(PlayerColor winner, EndReason endReason)
    {
        this.Winner = winner;
        this.EndReason = endReason;
    }

    // 승리 결과를 생성하는 정적 메서드
    // 체큼메이트로 게임이 끝난 경우 사용
    public static Result Win(PlayerColor winner)
    {
        return new Result(winner, EndReason.Checkmate);
    }

    // 무승부 결과를 생성하는 정적 메서드
    // 무승부 사유를 받아서 Result 객체 생성
    public static Result Draw(EndReason reason)
    {
        return new Result(PlayerColor.None, reason);
    }
}