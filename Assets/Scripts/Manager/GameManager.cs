using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance {  get { return instance; } }

    public BoardManager board;
    public GameState state;

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;

            if(board == null)
            {
                board = gameObject.AddComponent<BoardManager>();
            }
        }
        else 
        { 
            Destroy(this);
        }
    }

    private void Start()
    {
        board.InitMovePlatform();
        RestartGame();
    }

    public void RestartGame()
    {
        board.Init();
        state = new GameState(PlayerColor.White, board.board);
    }
}
