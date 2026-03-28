using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 체스판의 말 오브젝트 생성, 삭제, 선택, 이동 표시판 관리 등을 담당하는 매니저
public class BoardManager : MonoBehaviour
{
    // 한 칸의 월드 좌표 크기
    public float cellSize = 0.66f;
    
    // 체스판 시작 원점 좌표
    public Vector2 origin = new(-2.3f, -2.3f);

    // 실제 논리 체스판 데이터
    public Board board;

    // 화면에 보이는 체스말 오브젝트를 저장하는 2차원 배열
    private Chessman[,] views = new Chessman[8, 8]; 

    // (플레이어 색, 말 종류) -> 프리팹 매핑용 딕셔너리
    private Dictionary<(PlayerColor, PieceType), GameObject> prefabMap;

    // 각 말의 프리팹
    public GameObject white_pawn, white_knight, white_bishop, white_rook, white_queen, white_king;
    public GameObject black_pawn, black_knight, black_bishop, black_rook, black_queen, black_king;
    
    // 이동 가능 위치를 표시하는 플레이트 프리팹
    public GameObject move_plate;

    // 보드 전체의 이동 플레이트 저장 배열
    public MovePlate[,] move_plates = new MovePlate[8, 8];
    
    // 현재 선택된 말이 이동 가능한 수를 캐싱
    public List<Move> cachedMoves = new List<Move>();

    // 말 오브젝트를 넣어둘 부모 Transform
    [SerializeField] private Transform pieceParent;
    // 이동 플레이트들을 넣어둘 부모 Transform
    [SerializeField] private Transform plateParent;

    // 현제 선택된 체스말
    private Chessman selected;

    private void Awake()
    {
        // 말 프리팹 매핑 테이블 생성
        BuildPrefabMap();

        // pieceParent가 Inspector에서 지정되지 않았다면 이름으로 찾아서 연결
        if (pieceParent == null)
        {
            var go = GameObject.Find("Piece");
            if (go != null) pieceParent = go.transform;
        }

        // plateParent가 Inspector에서 지정되지 않았다면 이름으로 찾아서 연결
        if (plateParent == null)
        {
            var go = GameObject.Find("MovePlate");
            if (go != null) plateParent = go.transform;
        }
    }

    // 게임 시작 시 보드를 초기화하고 화면을 다시 그림
    public void Init()
    {
        DestroyAllPiece();          // 기존 말 제거
        board = Board.Initial();    // 초기 체스판 세팅
        RedrawPiecesFromBoard();    // 보드 상태를 기준으로 화면 말 생성
        Deselect();                 // 선택 상태 초기화
    }

    // 현재 화면에 있는 모든 말 오브젝트 삭제
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

    // 실제 Board 데이터를 기준으로 화면의 말 오브젝트를 다시 생성
    public void RedrawPiecesFromBoard()
    {
        // 기존 화면 오브젝트 전부 제거
        DestroyAllPiece();

        // 보드 전체를 순회하면서 말이 있는 칸만 프리팹 생성
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece piece = board[r, c];
                if (piece == null) continue;

                // 해당 말에 맞는 프리팹 찾기
                GameObject prefab = GetPrefab(piece);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not assigned for {piece.Color} {piece.Type}");
                    continue;
                }

                // 보드 좌표를 월드 좌표로 변환 후 생성
                Vector3 pos = GridToWorld(r, c);
                GameObject go = Instantiate(prefab, pos, Quaternion.identity);
                
                // Chessman 컴포넌트 초기화
                Chessman chessman = go.GetComponent<Chessman>();
                chessman.Init(new Position(r, c));

                // views 배열에 저장
                views[r, c] = chessman;

                // 오브젝트 이름 저장
                go.name = $"{piece.Color}_{piece.Type}_{r}_{c}";
                
                // 부모 지정
                go.transform.SetParent(pieceParent);
            }
        }
    }

    // 이동 가능 위치를 표시할 모든 MovePlate를 미리 생성
    public void InitMovePlatform()
    {
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Vector3 pos = GridToWorld(r, c, 0f);
                GameObject go = Instantiate(move_plate, pos, Quaternion.identity);
                MovePlate movePlate = go.GetComponent<MovePlate>();

                // 각 플레이트에 자기 보드 위치 저장
                movePlate.Init(new Position(r, c));

                go.name = $"move_platform_{r}_{c}";
                move_plates[r, c] = movePlate;
                
                // 처음에는 비활성화
                go.SetActive(false);

                // 부모 설정
                go.transform.SetParent(pieceParent.transform);
            }
        }
    }

    // 모든 이동 플레이트를 한 번에 켜거나 끄는 함수
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

    // 말 프리팹들을 딕셔너리에 등록
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

    // 체스말 클릭 시 호출
    public void OnClickChessman(Chessman chessman)
    {
        // 이미 선택된 말을 다시 클릭하면 선택 해제 
        if(selected == chessman)
        {
            Deselect();
            return;
        }

        // 현재 게임 상태에서 해당 말의 합법적인 이동 목록 가져오기
        var moves = GameManager.Instance.state.LegalMoveForPiece(chessman.Pos).ToList();
       
        // 이동 가능한 수가 없으면 선택 해제
        if(moves.Count == 0)
        {
            Deselect();
            return;
        }

        // 말 선택 및 이동 목록 저장
        selected = chessman;
        cachedMoves = moves;

        // 기존 표시판 전부 끄기
        SetAllPlatesActive(false);

        // 이동 가능한 칸에 해당하는 플레이트만 활성화
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

    // 이동 플레이트 클릭 시 호출
    public void OnClickMovePlate(MovePlate movePlate)
    {
        // 선택된 말이 없으면 아무것도 안 함
        if(selected == null)
        {
            return;
        }

        // 클릭한 플레이트의 위치
        var target = movePlate.Pos;

        // 캐싱된 수 중에서 클릭한 칸으로 가는 수 찾기
        var mv = cachedMoves.FirstOrDefault(m => m.ToPos.row == target.row && m.ToPos.column == target.column);

        // 해당 수가 없으면 종료
        if(mv == null)
        {
            return;
        }

        // 폰 프로모션이면 승급 처리
        if (mv.Type == MoveType.PawnPromotion)
        {
            HandlePromotion(mv.FromPos, mv.ToPos);
        }
        else
        {
            // 일반 이동 처리
            HandleMove(mv);
        }

        // 플레이어 수가 끝난 뒤 엔진 수 진행
        GameManager.Instance.engine.EngineMove();
    }

    // 실제 이동을 처리하는 함수
    private void HandleMove(Move move)
    {
        // 게임 상태에 수 반영
        GameManager.Instance.state.MakeMove(move);
        
        // 화면에 다시 그림
        RedrawPiecesFromBoard();
        
        // 선택 해제
        Deselect();
    }

    // 폰 승급 처리
    private void HandlePromotion(Position from, Position to)
    {
        // 승급 UI 상태로 변경
        UIManager.Instance.ChangeState(UIState.Promotion);
        
        // 승급 선택 UI 표시
        UIManager.Instance.promotionUI.SetUI();

        // 사용자가 승급 기물을 선택했을 때 실행될 콜백 등록
        UIManager.Instance.promotionUI.select_promotion += type =>
        {
            Move promMove = new PawnPromotion(from, to, type);
            HandleMove(promMove);
            ReplaceViewForPromotion(promMove);
        };
    }

    // 승급 후 해당 칸의 말 뷰를 새 기물로 교체
    private void ReplaceViewForPromotion(Move move)
    {
        var old = views[move.ToPos.row, move.ToPos.column];
        if (old != null)
        {
            Destroy(old.gameObject);
            views[move.ToPos.row, move.ToPos.column] = null;
        }

        // 승급 후 보드에 놓인 기물에 맞는 프리팹 생성
        var prefab = GetPrefab(board[move.ToPos.row, move.ToPos.column]);
        var go = Instantiate(prefab, GridToWorld(move.ToPos.row, move.ToPos.column), Quaternion.identity);
        go.transform.SetParent(pieceParent);

        var cm = go.GetComponent<Chessman>();
        cm.Init(move.ToPos);
        views[move.ToPos.row, move.ToPos.column] = cm;
    }

    // 기물 정보에 맞는 프리팹 반환
    private GameObject GetPrefab(Piece piece)
    {
        prefabMap.TryGetValue((piece.Color, piece.Type), out GameObject prefab);
        return prefab;
    }

    // 보드 좌표(row, col)를 월드 좌표로 변환
    private Vector3 GridToWorld(int row, int col, float z = 1f)
    {
        float x = origin.x + col * cellSize;
        float y = origin.y + (7 - row) * cellSize;
        return new Vector3(x, y, z);
    }

    // 현재 선택 상태와 이동 플레이트 표시를 모두 초기화
    private void Deselect()
    {
        selected = null;
        cachedMoves.Clear();
        SetAllPlatesActive(false);
    }

    // 좌표가 보드 범위 안에 있는지 확인
    private static bool InBounds(int r, int c) => (uint)r < 8 && (uint)c < 8;
}
