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
                return PlayerColor.White;
            case PlayerColor.Black:
                return PlayerColor.Black;
            default:
                return PlayerColor.None;
        }
    }
}
