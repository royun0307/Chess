using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private BoardManager instance;
    public BoardManager Instance { get { return instance; } }

    private float xPivot = 1.1f;
    private float yPivot = 1.1f;

    public Piece[,] board;
    [SerializeField] private List<GameObject> points;
    public GameObject[,] points_board = new GameObject[8, 8];

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else 
        {
            Destroy(this);
        }
    }

    public void MovePiece()
    {

    }
}
