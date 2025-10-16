using UnityEngine;

public class GameState
{
    public Board Board { get; }
    public PlayerColor CurrnetPlayer { get; private set; }
    
    public GameState(PlayerColor player, Board board)
    {
        this.CurrnetPlayer = player;
        this.Board = board;
    }
}
