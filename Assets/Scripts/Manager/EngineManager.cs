using UnityEngine;
using System.Collections;

public class EngineManager : MonoBehaviour
{
    private IChessEngine engine;
    private Coroutine pending;
    private void Awake() { engine = new SimpleChessEngine(); }

    public void EngineMove()
    {
        var manager = GameManager.Instance;
        if (pending != null || manager == null || manager.Session == null || !manager.Session.CanEngineMove) return;
        pending = StartCoroutine(AITurn(manager, manager.Session, manager.Session.Revision));
    }

    public void CancelPendingMove()
    {
        if (pending != null) StopCoroutine(pending);
        pending = null;
    }

    private void OnDisable() { CancelPendingMove(); }

    private IEnumerator AITurn(GameManager manager, GameSession session, int revision)
    {
        yield return new WaitForSeconds(0.1f);
        if (manager == null || manager.Session != session || session.Revision != revision || !session.CanEngineMove)
        {
            pending = null;
            yield break;
        }
        Move best = engine.GetBestMove(session.State.Board.Copy(), session.State.CurrentPlayer, depth: 3);
        pending = null;
        manager.ApplyEngineMove(best, session, revision);
    }
}
