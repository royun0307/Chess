public enum PlayerColor
{
    None,
    White,
    Black
}
public static class Player
{
    public static PlayerColor Opponent(this PlayerColor player_color)
    {
        switch (player_color)
        {
            case PlayerColor.White:
                return PlayerColor.Black;
            case PlayerColor.Black:
                return PlayerColor.White;
            default:
                return PlayerColor.None;
        }
    }
}
