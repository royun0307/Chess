using JetBrains.Annotations;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;

public class SimpleChessEngine : IChessEngine
{
    private const int INF = 1000000;

    public Move GetBestMove(Board board, PlayerColor side_to_move, int depth)
    {
        Move best_move = default;
        int best_score = side_to_move == PlayerColor.White ? -INF : INF;

        GameState state = new GameState(side_to_move, board);
        List<Move> moves = state.AllLegalMovesFor(side_to_move).ToList();

        int alpha = -INF;
        int beta = INF;

        foreach (var move in moves)
        {
            Board next = board.Copy();
            move.Execute(next);

            int score = Search(next, depth - 1, alpha, beta, side_to_move.Opponent());

            if (side_to_move == PlayerColor.White)
            {
                if (score > best_score)
                {
                    best_score = score;
                    best_move = move;
                }
                alpha = System.Math.Max(alpha, score);
            }
            else
            {
                if (score < best_score)
                {
                    best_score = score;
                    best_move = move;
                }
                beta= System.Math.Min(beta, score);
            }

            if(beta <= alpha)
                break;
        }

        return best_move;
    }

    private int Search(Board board, int depth, int alpha, int beta, PlayerColor side_to_move)
    {
        if(depth == 0)
        {
            return Evaluate(board);
        }

        GameState state = new GameState(side_to_move, board);
        List<Move> moves = state.AllLegalMovesFor(side_to_move).ToList();

        if (moves.Count == 0)
        {
            if (board.IsInCheck(side_to_move))
            {
                return side_to_move == PlayerColor.White ? -INF + 1 : INF - 1;
            }
            else
            {
                return 0;
            }
        }

        if (side_to_move == PlayerColor.White)
        {
            int value = -INF;

            foreach (var move in moves)
            {
                Board next = board.Copy();
                move.Execute(next);

                int score = Search(next, depth - 1, alpha, beta, side_to_move.Opponent());
                value = System.Math.Max(value, score);
                alpha = System.Math.Min(alpha, value);

                if(beta <= alpha)
                    break;
            }

            return value;
        }
        else
        {
            int value = INF;

            foreach (var move in moves)
            {
                Board next = board.Copy();
                move.Execute(next);

                int score = Search(next, depth - 1, alpha, beta, side_to_move.Opponent());
                value = System.Math.Min(value, score);
                beta = System.Math.Min(beta, value);

                if (beta <= alpha)
                    break;
            }

            return value;
        }
    }

    private int Evaluate(Board board)
    {
        int score = 0;

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece piece = board[r, c];
                if (piece == null)
                    continue;

                int value = GetPieceValue(piece.Type);
                if(piece.Color == PlayerColor.White)
                {
                    score += value;
                }
                else
                {
                    score -= value;
                }
            }
        }

        return score;
    }

    private int GetPieceValue(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
                return 300;
            case PieceType.Bishop:
                return 300;
            case PieceType.Rook:
                return 500;
            case PieceType.Queen:
                return 600;
            case PieceType.King:
                return 10000;
            default:
                return 0;
        }
    }
}