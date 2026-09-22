using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;
    public BoardManager board;
    public GameState state;
    public EngineManager engine;
    public GameSession Session { get; private set; }
    public bool CanHumanMove => Session != null && Session.CanHumanMove;
    public bool SetupVisible { get; private set; }
    public PlayerColor HumanColor { get; private set; } = PlayerColor.White;
    public int InitialMinutes { get; private set; } = 5;
    public int IncrementSeconds { get; private set; } = 3;
    private double clockStamp;

    public void Awake()
    {
        if (instance != null) { Destroy(this); return; }
        instance = this;
        if (board == null) board = gameObject.AddComponent<BoardManager>();
        if (engine == null) engine = gameObject.AddComponent<EngineManager>();
        if (GetComponent<EngineStatusUI>() == null) gameObject.AddComponent<EngineStatusUI>();
        if (GetComponent<MatchUI>() == null) gameObject.AddComponent<MatchUI>();
        if (GetComponent<PositionAnalysis>() == null) gameObject.AddComponent<PositionAnalysis>();
    }

    private void Start() { board.InitMovePlatform(); RestartGame(); ShowSetup(); }

    public void ShowSetup()
    {
        SyncClock();
        SetupVisible = true;
        engine.CancelPendingMove();
        Session?.SetPaused(true);
        board.Deselect();
        UIManager.Instance?.ChangeState(UIState.None);
    }

    public void StartMatch(int minutes, int increment, PlayerColor color)
    {
        InitialMinutes = Mathf.Clamp(minutes, 1, 180);
        IncrementSeconds = Mathf.Clamp(increment, 0, 60);
        HumanColor = color == PlayerColor.Black ? PlayerColor.Black : PlayerColor.White;
        RestartGame();
    }

    private void Update() { SyncClock(); }

    private bool SyncClock()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        double elapsed = now - clockStamp;
        clockStamp = now;
        if (SetupVisible || Session == null) return false;
        if (!Session.AdvanceClock(elapsed)) return false;
        UIManager.Instance?.promotionUI?.ResetAction();
        FinishMove();
        return true;
    }

    public void RestartGame()
    {
        SetupVisible = false;
        engine.CancelPendingMove();
        UIManager.Instance?.promotionUI?.ResetAction();
        board.Init();
        state = new GameState(PlayerColor.White, board.board);
        if (Session == null) Session = new GameSession(state);
        else Session.Restart(state);
        Session.Configure(HumanColor, InitialMinutes * 60, IncrementSeconds);
        clockStamp = Time.realtimeSinceStartupAsDouble;
        board.SetPerspective(HumanColor);
        UIManager.Instance?.ChangeState(UIState.None);
        if (Session.CanEngineMove) engine.EngineMove();
    }

    public void MakeMove(Move move)
    {
        if (SyncClock() || SetupVisible) return;
        if (Session != null && Session.TryHumanMove(move)) FinishMove();
    }

    public bool BeginPromotion(Position from, Position to)
    {
        if (SyncClock() || SetupVisible) return false;
        var ui = UIManager.Instance;
        if (ui == null || ui.promotionUI == null || Session == null || !Session.BeginPromotion(from, to)) return false;
        board.Deselect();
        int revision = Session.Revision;
        ui.ChangeState(UIState.Promotion);
        ui.promotionUI.SetUI();
        ui.promotionUI.select_promotion += type =>
        {
            if (SyncClock()) return;
            if (Session.CompletePromotion(type, revision)) FinishMove();
        };
        return true;
    }

    public bool ApplyEngineMove(Move move, GameSession session, int revision)
    {
        if (SyncClock() || SetupVisible) return false;
        if (Session == null || !ReferenceEquals(Session, session) || !Session.TryEngineMove(move, revision)) return false;
        FinishMove();
        return true;
    }

    public void SetPaused(bool paused)
    {
        if (SyncClock() || SetupVisible) return;
        if (Session == null || !Session.SetPaused(paused)) return;
        engine.CancelPendingMove();
        board.Deselect();
        UIManager.Instance?.ChangeState(paused ? UIState.Pause : UIState.None);
        if (!paused && Session.CanEngineMove) engine.EngineMove();
    }

    private void FinishMove()
    {
        board.Deselect();
        board.RedrawPiecesFromBoard();
        if (state.IsGameOver())
        {
            engine.CancelPendingMove();
            var ui = UIManager.Instance;
            if (ui != null)
            {
                ui.resultUI?.SetUI(state.Result.Winner, state.Result.EndReason);
                ui.ChangeState(UIState.Result);
            }
            return;
        }
        UIManager.Instance?.ChangeState(UIState.None);
        if (Session.CanEngineMove) engine.EngineMove();
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        if (engine != null) engine.CancelPendingMove();
        instance = null;
    }
}
