using System;
using System.Collections.Generic;
using System.Linq;

public class SimpleChessEngine : IChessEngine
{
    private const int INF = 1000000;

    static readonly int[] PieceValue =
    {
        0,    // None
        100,  // Pawn
        320,  // Knight
        330,  // Bishop
        500,  // Rook
        900,  // Queen
        100000     // King
    };

    public Move GetBestMove(Board board, PlayerColor side_to_move, int depth)
    {
        Move best_move = default;
        int best_score = side_to_move == PlayerColor.White ? -INF : INF;

        GameState state = new GameState(side_to_move, board);
        List<Move> moves = state.AllLegalMovesFor(side_to_move).ToList();
        OrderMoves(board, moves, side_to_move);

        if (moves.Count == 0)
            return best_move;

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
                alpha = Math.Max(alpha, score);
            }
            else
            {
                if (score < best_score)
                {
                    best_score = score;
                    best_move = move;
                }
                beta = Math.Min(beta, score);
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
        OrderMoves(board, moves, side_to_move);

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
                value = Math.Max(value, score);
                alpha = Math.Max(alpha, value);

                if (beta <= alpha)
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
                value = Math.Min(value, score);
                beta = Math.Min(beta, value);

                if (beta <= alpha)
                    break;
            }

            return value;
        }
    }

    private void OrderMoves(Board board, List<Move> moves, PlayerColor side_to_move)
    {
        int base_mat = EvaluateMaterial(board);

        var scored = new List<(Move move, int score)>(moves.Count);
        for (int i = 0; i < moves.Count; i++)
        {
            scored.Add((moves[i], ScoreMoveMVVLVA(board, moves[i])));
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));

        moves.Clear();
        for (int i = 0; i < scored.Count; i++)
        {
            moves.Add(scored[i].move);
        }
    }

    private int ScoreMove(Board board, Move move, PlayerColor side_to_move, int base_mat)
    {
        Board next = board.Copy();
        move.Execute(next);

        int next_mat = EvaluateMaterial(next);
        int delta = next_mat - base_mat;
        int delta_for_side = (side_to_move == PlayerColor.White) ? delta : -delta;

        int score = 0;

        score += delta_for_side * 100;

        if (next.IsInCheck(side_to_move.Opponent()))
        {
            score += 500;
        }

        return score;
    }

    private int ScoreMoveMVVLVA(Board board, Move move)
    {
        int fr = move.FromPos.row;
        int fc = move.FromPos.column;
        int tr = move.ToPos.row;
        int tc = move.ToPos.column;
        Piece attacker = board[fr, fc];

        if (attacker == null) return 0;

        int score = 0;

        if (move is PawnPromotion promo)
        {
            score += 8000 + PieceValue[(int)promo.GetPromotionPieceType()];
        }

        Piece victim = board[tr, tc];

        bool isEnPassant = move is Enpassant;
        if (victim != null || isEnPassant)
        {
            int victimValue = victim != null ? PieceValue[(int)victim.Type] :PieceValue[(int)PieceType.Pawn];
            int attackerValue = PieceValue[(int)attacker.Type];
            score += 10000 + victimValue * 10 - attackerValue;
        }

        return score;
    }

    private int Evaluate(Board board)
    {
        int score = 0;

        score += EvaluateMaterial(board);
        score += EvaluatePieceSquare(board);
        score += EvaluateMobility(board);
        score += EvaluatePawnStructure(board);
        score += EvaluateKingSafety(board);
        score += EvaluateTempo(board);

        return score;
    }

    private int EvaluateMaterial(Board board)
    {
        int score = 0;

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece p = board[r, c];
                if (p == null) continue;

                int v = PieceValue[(int)p.Type];
                score += p.Color == PlayerColor.White ? v : -v;
            }
        }

        return score;
    }

    int GetPST(Piece p, int r, int c)
    {
        int rr = p.Color == PlayerColor.White ? r : 7 - r;

        switch (p.Type)
        {
            case PieceType.Pawn:
                return SimplePST.PawnPST_MG[rr, c];
            case PieceType.Knight:
                return SimplePST.KnightPST_MG[rr, c];
            case PieceType.Bishop:
                return SimplePST.BishopPST_MG[rr, c];
            case PieceType.Rook:
                return SimplePST.RookPST_MG[rr, c];
            case PieceType.Queen:
                return SimplePST.QueenPST_MG[rr, c];
            case PieceType.King:
                return SimplePST.KingPST_MG[rr, c];
        }
        return 0;
    }

    private int EvaluatePieceSquare(Board board)
    {
        int score = 0;

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece p = board[r, c];
                if (p == null) continue;

                int v = GetPST(p, r, c);
                score += p.Color == PlayerColor.White ? v : -v;
            }
        }
        return score;
    }

    private int EvaluateMobility(Board board)
    {
        GameState state = new GameState(PlayerColor.White, board);
        int white_moves = state.AllLegalMovesFor(PlayerColor.White).Count();
        int black_moves = state.AllLegalMovesFor(PlayerColor.Black).Count();

        int factor = 2;
        return (white_moves - black_moves) * factor;
    }

    private int EvaluatePawnStructure(Board board)
    {
        int score = 0;

        int[] white_file_count = new int[8];
        int[] black_file_count = new int[8];

        List<(int r, int c, PlayerColor color)> pawns = new();

        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Piece p = board[r, c];
                if (p == null || p.Type != PieceType.Pawn) continue;

                pawns.Add((r, c, p.Color));
                if (p.Color == PlayerColor.White) white_file_count[c]++;
                else black_file_count[c]++;
            }
        }

        foreach (var (r, c, color) in pawns)
        {
            bool is_white = color == PlayerColor.White;
            int[] my_file_count = is_white ? white_file_count : black_file_count;
            int[] my_black_file_ = is_white ? black_file_count : white_file_count;
            
            if (my_file_count[c] > 1)
            {
                score += is_white ? -10 : 10;
            }

            bool left_has = c > 0 && my_file_count[c - 1] > 0;
            bool right_has = c < 7 && my_file_count[c + 1] > 0;

            if (!left_has && !right_has)
            {
                score += is_white ? -15 : 15;
            }

            bool blocked = false;

            for (int dc = -1; dc <= 1; dc++) 
            {
                int file = c + dc;
                if (file < 0 || file > 7) continue;

                if (is_white)
                {
                    for (int rr = r + 1; rr < 8; rr++)
                    {
                        Piece pp = board[rr, file];
                        if (pp != null && pp.Type == PieceType.Pawn && pp.Color != color)
                        {
                            blocked = true;
                        }
                    }
                }
                else
                {
                    for (int rr = r - 1; rr >= 0; rr--)
                    {
                        Piece pp = board[rr, file];
                        if (pp != null && pp.Type == PieceType.Pawn && pp.Color != color)
                        {
                            blocked = true;
                        }
                    }
                }
            }
            if (!blocked) 
            {
                int rank = is_white ? r : (7 - r);
                score += is_white ? (20 + rank * 5) : -(20 + rank * 5);
            } 
        }

        return score;
    }

    private int EvaluateKingSafety(Board board)
    {
        return 0;
    }

    private int EvaluateTempo(Board board)
    {
        return 0;
    }

}