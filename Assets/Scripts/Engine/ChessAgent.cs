using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;
using System.Diagnostics;

public class ChessAgent : Agent
{
    private GameState state;
    private List<Move> legalMoves = new List<Move>();

    private int turnCount;

    private const int MaxTurnCount = 40;
    private const int MaxMoveCount = 128;

    private readonly PlayerColor agentColor = PlayerColor.White;
    private readonly PlayerColor opponentColor = PlayerColor.Black;

    public override void OnEpisodeBegin()
    {
        //Debug.Log("Episode Begin");

        Board board = Board.Initial();
        state = new GameState(PlayerColor.White, board);
        turnCount = 0;

        UpdateLegalMoves();
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        if (state == null)
        {
            for (int i = 0; i < 71; i++)
                sensor.AddObservation(0f);

            return;
        }

        Board board = state.Board;

        // 1. 보드 64칸 관찰
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];
                sensor.AddObservation(PieceToValue(piece));
            }
        }

        // 2. 현재 턴
        sensor.AddObservation(state.CurrentPlayer == PlayerColor.White ? 1f : -1f);

        // 3. 캐슬링 가능 여부
        sensor.AddObservation(board.CastleRightKS(PlayerColor.White) ? 1f : 0f);
        sensor.AddObservation(board.CastleRightQS(PlayerColor.White) ? 1f : 0f);
        sensor.AddObservation(board.CastleRightKS(PlayerColor.Black) ? 1f : 0f);
        sensor.AddObservation(board.CastleRightQS(PlayerColor.Black) ? 1f : 0f);

        // 4. 앙파상 가능 여부
        sensor.AddObservation(board.CanCaptureEnPassant(PlayerColor.White) ? 1f : 0f);
        sensor.AddObservation(board.CanCaptureEnPassant(PlayerColor.Black) ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {   
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        if (state == null)
        {
            AddReward(-0.1f);
            EndEpisode();
            return;
        }

        if (state.CurrentPlayer != agentColor)
        {
            AddReward(-0.1f);
            EndEpisode();
            return;
        }

        if (legalMoves == null || legalMoves.Count == 0)
        {
            UnityEngine.Debug.LogWarning("Agent has no legal moves. Ending episode.");

            // 백 차례인데 둘 수 있는 수가 없으면 체크메이트 또는 스테일메이트
            GiveResultReward(agentColor);
            EndEpisode();
            return;
        }

        int actionIndex = actions.DiscreteActions[0];

        if (actionIndex < 0 || actionIndex >= legalMoves.Count)
        {
            AddReward(-0.1f);
            EndEpisode();
            return;
        }

        float beforeScore = EvaluateBoard(state.Board, agentColor);

        // 1. 백 Agent 수 실행
        Move selectMove = legalMoves[actionIndex];

        state.MakeMoveForTraining(selectMove);

        // 백이 둔 뒤에는 흑 차례.
        // 흑이 둘 수 없으면 게임 종료.
        List<Move> opponentMoves = GetLegalMovesForTraining(opponentColor, "Opponent");

        if (opponentMoves.Count == 0)
        {
            GiveResultReward(opponentColor);
            EndEpisode();
            return;
        }

        // 2. 흑 자동 수 실행
        PlayRandomOpponentMove(opponentMoves);

        // 흑이 둔 뒤에는 백 차례.
        // 백이 둘 수 없으면 게임 종료.
        legalMoves = GetLegalMovesForTraining(agentColor, "Agent");

        if (legalMoves.Count == 0)
        {
            GiveResultReward(agentColor);
            EndEpisode();
            return;
        }

        float afterScore = EvaluateBoard(state.Board, agentColor);

        // 3. 백 기준 기물 이득 보상
        AddReward((afterScore - beforeScore) * 0.01f);

        // 4. 너무 긴 게임 방지
        AddReward(-0.001f);

        turnCount++;

        if (turnCount >= MaxTurnCount)
        {
            AddReward(-0.1f);
            EndEpisode();
            return;
        }

        totalSw.Stop();

        if (totalSw.ElapsedMilliseconds > 100)
        {
            UnityEngine.Debug.LogWarning($"OnActionReceived total took {totalSw.ElapsedMilliseconds} ms");
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (state == null)
            return;

        if (state.CurrentPlayer != agentColor)
            return;

        if (legalMoves == null)
            return;

        for (int i = legalMoves.Count; i < MaxMoveCount; i++)
        {
            actionMask.SetActionEnabled(0, i, false);
        }
    }

    private void PlayRandomOpponentMove(List<Move> opponentMoves)
    {
        if (state == null)
            return;

        if (state.CurrentPlayer != opponentColor)
            return;

        if (opponentMoves == null || opponentMoves.Count == 0)
            return;

        int randomIndex = Random.Range(0, opponentMoves.Count);
        Move opponentMove = opponentMoves[randomIndex];

        state.MakeMoveForTraining(opponentMove);
    }

    private float PieceToValue(Piece piece)
    {
        if (piece == null)
            return 0f;

        int value = piece.Type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 2,
            PieceType.Bishop => 3,
            PieceType.Rook => 4,
            PieceType.Queen => 5,
            PieceType.King => 6,
            _ => 0
        };

        return piece.Color == PlayerColor.White ? value : -value;
    }

    private float EvaluateBoard(Board board, PlayerColor player)
    {
        float score = 0f;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Piece piece = board[row, col];

                if (piece == null)
                    continue;

                float value = GetPieceScore(piece.Type);

                if (piece.Color == player)
                    score += value;
                else
                    score -= value;
            }
        }

        return score;
    }

    private float GetPieceScore(PieceType type)
    {
         return type switch
        {
            PieceType.Pawn => 1f,
            PieceType.Knight => 3f,
            PieceType.Bishop => 3f,
            PieceType.Rook => 5f,
            PieceType.Queen => 9f,
            PieceType.King => 0f,
            _ => 0f
        };
    }

    private void UpdateLegalMoves()
    {
        if (state == null)
        {
            legalMoves.Clear();
            return;
        }

        if (state.CurrentPlayer != agentColor)
        {
            legalMoves.Clear();
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        legalMoves = state.AllLegalMovesFor(state.CurrentPlayer).ToList();

        sw.Stop();

        if (sw.ElapsedMilliseconds > 50)
        {
            UnityEngine.Debug.LogWarning(
                $"UpdateLegalMoves took {sw.ElapsedMilliseconds} ms / legalMoves: {legalMoves.Count}"
            );
        }
    }

    private void GiveResultReward(PlayerColor playerToMove)
    {
        if (state == null)
        {
            AddReward(-0.1f);
            return;
        }

        bool isCheck = state.IsInCheck(playerToMove);

        
        UnityEngine.Debug.LogWarning(
            $"GameOver Reward Check - PlayerToMove: {playerToMove}, IsCheck: {isCheck}"
        );

        if (isCheck)
        {
            if (playerToMove == opponentColor)
            {
                // 흑이 둘 차례인데 체크메이트면 백 Agent 승리
                AddReward(1f);
            }
            else
            {
                // 백이 둘 차례인데 체크메이트면 백 Agent 패배
                AddReward(-1f);
            }
        }
        else
        {
            // 둘 수 있는 수가 없는데 체크가 아니면 스테일메이트
            AddReward(-0.1f);
        }
    }

    private List<Move> GetLegalMovesForTraining(PlayerColor player, string label)
    {
        if (state == null)
            return new List<Move>();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        List<Move> moves = state.AllLegalMovesFor(player).ToList();

        sw.Stop();

        if (sw.ElapsedMilliseconds > 50)
        {
            UnityEngine.Debug.LogWarning(
                $"{label} AllLegalMovesFor took {sw.ElapsedMilliseconds} ms / moves: {moves.Count}"
            );
        }

        return moves;
    }
}