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

    public void Awake()
    {
        if (instance != null) { Destroy(this); return; }
        instance = this;
        if (board == null) board = gameObject.AddComponent<BoardManager>();
        if (engine == null) engine = gameObject.AddComponent<EngineManager>();
        if (GetComponent<EngineStatusUI>() == null) gameObject.AddComponent<EngineStatusUI>();
    }

    private void Start() { board.InitMovePlatform(); RestartGame(); }

    public void RestartGame()
    {
        engine.CancelPendingMove();
        UIManager.Instance?.promotionUI?.ResetAction();
        board.Init();
        state = new GameState(PlayerColor.White, board.board);
        if (Session == null) Session = new GameSession(state);
        else Session.Restart(state);
        UIManager.Instance?.ChangeState(UIState.None);
    }

    public void MakeMove(Move move)
    {
        if (Session != null && Session.TryHumanMove(move)) FinishMove();
    }

    public bool BeginPromotion(Position from, Position to)
    {
        var ui = UIManager.Instance;
        if (ui == null || ui.promotionUI == null || Session == null || !Session.BeginPromotion(from, to)) return false;
        board.Deselect();
        int revision = Session.Revision;
        ui.ChangeState(UIState.Promotion);
        ui.promotionUI.SetUI();
        ui.promotionUI.select_promotion += type =>
        {
            if (Session.CompletePromotion(type, revision)) FinishMove();
        };
        return true;
    }

    public bool ApplyEngineMove(Move move, GameSession session, int revision)
    {
        if (Session == null || !ReferenceEquals(Session, session) || !Session.TryEngineMove(move, revision)) return false;
        FinishMove();
        return true;
    }

    public void SetPaused(bool paused)
    {
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
