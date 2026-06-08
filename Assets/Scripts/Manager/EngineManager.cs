using UnityEngine;
using System.Collections;

// 체스 AI의 수를 계산하고 실행하는 매니저
public class EngineManager : MonoBehaviour
{
    // 실제로 수를 계산하는 체스 엔진 객체
    private IChessEngine engine;

    private void Awake()
    {
        // 게임 시작 시 사용할 체스 엔진 생성
        engine = new SimpleChessEngine();
    }

    // AI 차레를 시작하는 함수
    public void EngineMove()
    {
        // 코루틴을 실행해서 약간의 딜레이 후 AI가 두도록 함
        StartCoroutine(AITurnCorutine());
    }

    // AI가 실제로 수를 계산하고 실행하는 코루틴
    private IEnumerator AITurnCorutine()
    {
        // 너무 즉시 두지 않도록 약한 기다림
        yield return new WaitForSeconds(0.1f);

        // 현재 보드 상태와 현재 플레이어 기준으로 엔진이 가장 좋은 수를 계산
        Move best = engine.GetBestMove(GameManager.Instance.board.board, GameManager.Instance.state.CurrentPlayer, depth: 3);

        // 계산된 수를 게임 상태에 반영
        GameManager.Instance.MakeMove(best);

        // 변경된 보드 상태를 화면에 다시 그림
        GameManager.Instance.board.RedrawPiecesFromBoard();
    }
}
