// 플레이어의 색상을 나타내는 열거형
public enum PlayerColor
{
    None,   // 플레이어가 없는 상태
    White,  // 백 플레이어
    Black   // 흑 플레이어
}

// PlayerColor와 관련된 확장 메서드를 모아둔 정적 클래스
public static class Player
{
    // 현재 플레이어의 반대편 색상을 반환하는 확장 메서드
    public static PlayerColor Opponent(this PlayerColor player_color)
    {
        switch (player_color)
        {
            case PlayerColor.White:
                return PlayerColor.Black; // 백의 상대는 흑

            case PlayerColor.Black:
                return PlayerColor.White; // 흑의 상대는 백
            default:
                return PlayerColor.None;  // None의 상대도 None 처리
        }
    }
}
