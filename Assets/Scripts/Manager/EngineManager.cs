using UnityEngine;
using System.Collections;

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

    private IEnumerator AITurnCorutine()
    {
        yield return new WaitForSeconds(0.1f);

        Move best = engine.GetBestMove(GameManager.Instance.board.board, GameManager.Instance.state.CurrnetPlayer, depth: 3);

        GameManager.Instance.state.MakeMove(best);
        GameManager.Instance.board.RedrawPiecesFromBoard();
    }
}
