using Mono.Cecil;
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

    [SerializeField] private Transform pieceParent;
    [SerializeField] private Transform plateParent;

    private Chessman selected;

    private void Awake()
    {
        BuildPrefabMap();
        if (pieceParent == null)
        {
            var go = GameObject.Find("Piece");
            if (go != null) pieceParent = go.transform;
        }

        if (plateParent == null)
        {
            var go = GameObject.Find("MovePlate");
            if (go != null) plateParent = go.transform;
        }
    }

    public void Init()
    {
        DestroyAllPiece();
        board = Board.Initial();
        RedrawPiecesFromBoard();
        Deselect();
    }

    private void DestroyAllPiece()
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if(views[r, c] != null)
                {
                    Destroy(views[r, c].gameObject);
                    views[r, c] = null;
                }
            }
        }
    }

    public void RedrawPiecesFromBoard()
    {
        DestroyAllPiece();

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
                views[r, c] = chessman;
                go.name = $"{piece.Color}_{piece.Type}_{r}_{c}";
                go.transform.SetParent(pieceParent);
            }
        }
    }

    public void InitMovePlatform()
    {
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
                go.transform.SetParent(pieceParent.transform);
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

        if (mv.Type == MoveType.PawnPromotion)
        {
            HandlePromotion(mv.FromPos, mv.ToPos);
        }
        else
        {
            HandleMove(mv);
        }
    }

    private void HandleMove(Move move)
    {
        GameManager.Instance.state.MakeMove(move);
        RedrawPiecesFromBoard();
        Deselect();
    }

    private void HandlePromotion(Position from, Position to)
    {
        UIManager.Instance.ChangeState(UIState.Promotion);
        UIManager.Instance.promotionUI.SetUI();
        UIManager.Instance.promotionUI.select_promotion += type =>
        {
            Move promMove = new PawnPromotion(from, to, type);
            HandleMove(promMove);
            ReplaceViewForPromotion(promMove);
        };
    }

    private void ReplaceViewForPromotion(Move move)
    {
        var old = views[move.ToPos.row, move.ToPos.column];
        if (old != null)
        {
            Destroy(old.gameObject);
            views[move.ToPos.row, move.ToPos.column] = null;
        }

        var prefab = GetPrefab(board[move.ToPos.row, move.ToPos.column]);
        var go = Instantiate(prefab, GridToWorld(move.ToPos.row, move.ToPos.column), Quaternion.identity);
        go.transform.SetParent(pieceParent);

        var cm = go.GetComponent<Chessman>();
        cm.Init(move.ToPos);
        views[move.ToPos.row, move.ToPos.column] = cm;
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
