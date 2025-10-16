using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.Image;

public class BoardManager : MonoBehaviour
{
    public float cellSize = 0.66f;
    public Vector2 origin = new(-2.3f, -2.3f);

    public Board board;

    private Dictionary<(PlayerColor, PieceType), GameObject> prefabMap;

    public GameObject white_pawn, white_knight, white_bishop, white_rook, white_queen, white_king;
    public GameObject black_pawn, black_knight, black_bishop, black_rook, black_queen, black_king;

    private void Awake()
    {
        BuildPrefabMap();
    }

    public void Init()
    {
        board = Board.Initial();
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece piece = board[r, c];
                if (piece == null) continue;

                GameObject prefab = GetPrefab(piece);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not assigned for {piece.Color} {piece.Type}");
                    continue;
                }

                Vector3 pos = GridToWorld(r, c);
                GameObject go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = $"{piece.Color}_{piece.Type}_{r}_{c}";
            }
        }
    }

    private void BuildPrefabMap()
    {
        prefabMap = new()
        {
            [(PlayerColor.White, PieceType.Pawn)] = white_pawn,
            [(PlayerColor.White, PieceType.Knight)] = white_knight,
            [(PlayerColor.White, PieceType.Bishop)] = white_bishop,
            [(PlayerColor.White, PieceType.Rook)] = white_rook,
            [(PlayerColor.White, PieceType.Queen)] = white_queen,
            [(PlayerColor.White, PieceType.King)] = white_king,

            [(PlayerColor.Black, PieceType.Pawn)] = black_pawn,
            [(PlayerColor.Black, PieceType.Knight)] = black_knight,
            [(PlayerColor.Black, PieceType.Bishop)] = black_bishop,
            [(PlayerColor.Black, PieceType.Rook)] = black_rook,
            [(PlayerColor.Black, PieceType.Queen)] = black_queen,
            [(PlayerColor.Black, PieceType.King)] = black_king,
        };
    }

    private GameObject GetPrefab(Piece piece)
    {
        prefabMap.TryGetValue((piece.Color, piece.Type), out GameObject prefab);
        return prefab;
    }

    private Vector3 GridToWorld(int row, int col)
    {
        float x = origin.x + col * cellSize;
        float y = origin.y + (7 - row) * cellSize;
        return new Vector3(x, y, 1f);
    }
}
