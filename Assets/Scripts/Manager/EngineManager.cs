using UnityEngine;

public class EngineManager : MonoBehaviour
{
    private IChessEngine engine;

    private void Awake()
    {
        engine = new SimpleChessEngine();
    }

    public void EngineMove()
    {
        StartCoroutine(AITurnCorutine());
    }

    private System.Collections.IEnumerator AITurnCorutine()
    {
        yield return new WaitForSeconds(0.3f);

        Move best = engine.GetBestMove(GameManager.Instance.board.board, GameManager.Instance.state.CurrnetPlayer, depth: 3);

        GameManager.Instance.state.MakeMove(best);
        GameManager.Instance.board.RedrawPiecesFromBoard();
    }
}
