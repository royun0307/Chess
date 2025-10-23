using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public float cellSize = 0.66f;
    public Vector2 origin = new(-2.3f, -2.3f);

    public Board board;
    private Chessman[,] views = new Chessman[8, 8];

    private Dictionary<(PlayerColor, PieceType), GameObject> prefabMap;

    public GameObject white_pawn, white_knight, white_bishop, white_rook, white_queen, white_king;
    public GameObject black_pawn, black_knight, black_bishop, black_rook, black_queen, black_king;
    
    public GameObject move_plate;
    public MovePlate[,] move_plates = new MovePlate[8, 8];
    public List<Move> cachedMoves = new List<Move>();

    private Chessman selected;

    private void Awake()
    {
        BuildPrefabMap();
    }

    public void Init()
    {
        board = Board.Initial();
        GameObject parent = GameObject.Find("Piece");
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
                Chessman chessman = go.GetComponent<Chessman>();
                chessman.Init(new Position(r, c));
                views[r,c] = chessman;
                go.name = $"{piece.Color}_{piece.Type}_{r}_{c}";
                go.transform.parent = parent.transform;
            }
        }
    }

    public void InitMovePlatform()
    {
        GameObject parent = GameObject.Find("MovePlate");

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {

                Vector3 pos = GridToWorld(r, c, 0f);
                GameObject go = Instantiate(move_plate, pos, Quaternion.identity);
                MovePlate movePlate = go.GetComponent<MovePlate>();

                movePlate.Init(new Position(r, c));
                go.name = $"move_platform_{r}_{c}";
                move_plates[r, c] = movePlate;
                go.SetActive(false);
                go.transform.parent = parent.transform;
            }
        }
    }

    public void RefreshPieces()
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (views[r, c] == null) continue;
                views[r, c].gameObject.SetActive(false);
            }
        }

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece p = board[r, c];
                if (p == null) continue;

                var view = views[r, c];
                view.gameObject.SetActive(true);
                view.SetBoardPos(new Position(r, c));
                view.transform.position = GridToWorld(r, c);
            }
        }
    }

    private void SetAllPlatesActive(bool on)
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (move_plates[r, c] != null)
                {
                    move_plates[r, c].gameObject.SetActive(on);
                }
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

    public void OnClickChessman(Chessman chessman)
    {
        if(selected == chessman)
        {
            Deselect();
            return;
        }

        var moves = GameManager.Instance.state.LegalMoveForPiece(chessman.Pos).ToList();
        if(moves.Count == 0)
        {
            Deselect();
            return;
        }

        selected = chessman;
        cachedMoves = moves;

        SetAllPlatesActive(false);


        foreach(var mv in cachedMoves)
        {
            int r = mv.ToPos.row;
            int c = mv.ToPos.column;
            if (InBounds(r, c))
            {
                var plate = move_plates[r, c];
                if (plate != null)
                {
                    plate.gameObject.SetActive(true);
                }
            }
        }
    }

    public void OnClickMovePlate(MovePlate movePlate)
    {
        if(selected == null)
        {
            return;
        }

        var target = movePlate.Pos;
        var mv = cachedMoves.FirstOrDefault(m => m.ToPos.row == target.row && m.ToPos.column == target.column);

        if(mv == null)
        {
            return;
        }

        views[mv.ToPos.row, mv.ToPos.column] = selected;
        GameManager.Instance.state.MakeMove(mv);
        RefreshPieces();
        Deselect();
    }

    private GameObject GetPrefab(Piece piece)
    {
        prefabMap.TryGetValue((piece.Color, piece.Type), out GameObject prefab);
        return prefab;
    }

    private Vector3 GridToWorld(int row, int col, float z = 1f)
    {
        float x = origin.x + col * cellSize;
        float y = origin.y + (7 - row) * cellSize;
        return new Vector3(x, y, z);
    }

    private void Deselect()
    {
        selected = null;
        cachedMoves.Clear();
        SetAllPlatesActive(false);
    }

    private static bool InBounds(int r, int c) => (uint)r < 8 && (uint)c < 8;
}
