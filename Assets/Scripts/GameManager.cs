using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance = null;
    public static GameManager Instance;

    public BoardManager board;

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
        board.Init();
    }
}
