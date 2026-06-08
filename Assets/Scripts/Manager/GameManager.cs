using UnityEngine;

// 게임 전체 흐름을 관리하는 매니저
// 싱글톤으로 접근하며 보드, 게임 상태, 엔진을 관리함
public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스 저장 변수
    private static GameManager instance;

    // 외부에서 GameManager.Instance로 접근하기 위한 프로퍼티
    public static GameManager Instance {  get { return instance; } }

    // 체스판 화면 및 입력을 관리하는 BoardManager
    public BoardManager board;

    // 현제 게임 진행 상태를 관리하는 GameState
    public GameState state;

    // AI 엔진 동작을 관리하는 EngineManager
    public EngineManager engine;

    public void Awake()
    {
        // 아직 인스턴스가 없으면 현재 오브젝트를 싱글톤 인스턴스로 등록
        if (instance == null)
        {
            instance = this;

            // board가 연결되지 않았다면 현재 오브젝트에 BoardManager 추가
            if(board == null)
            {
                board = gameObject.AddComponent<BoardManager>();
            }

            // engine이 연결되지 않았다면 현재 오브젝트에 EngineManager 추가
            if(engine == null)
            {
                engine = gameObject.AddComponent<EngineManager>();
            }
        }
        else 
        { 
            // 이미 인스턴스가 있으면 중복 생성된 것이므로 제거
            Destroy(this);
        }
    }

    private void Start()
    {
        // 이동 가능 위치 표시판 생성
        board.InitMovePlatform();

        // 게임 시작 또는 재시작
        RestartGame();
    }

    // 게임을 초기 상태로 다시 시작하는 함수
    public void RestartGame()
    {
        // 보드 초기화
        board.Init();

        // 백부터 시작하는 새로운 게임 상태 생성
        state = new GameState(PlayerColor.White, board.board);
    }

    public void MakeMove(Move move)
    {
        state.MakeMove(move);

        if (state.IsGameOver())
        {
            ShowResultUI(state.Result);    
        }
    }

    private void ShowResultUI(Result result)
    {
        if (result.Winner == PlayerColor.None)
        {
            UIManager.Instance.resultUI.SetUI(PlayerColor.None, result.EndReason);
        }
        else
        {
            UIManager.Instance.resultUI.SetUI(result.Winner, result.EndReason);
        }

        UIManager.Instance.ChangeState(UIState.Result);
    }
}
