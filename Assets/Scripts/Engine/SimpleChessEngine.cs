using System;
using System.Collections.Generic;
using System.Linq;

//간단한 체스 엔진 구현
public class SimpleChessEngine : IChessEngine
{
    // 탐색에서 사용하는 매우 큰 값
    private const int INF = 1000000;
    
    // 퀘이선스 탐색 최대 깊이
    private const int QDEPTH_LIMIT = 8;

    // 기물 기본 가치
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

    //  현재 보드와 차례, 탐색 깊이를 받아 최선의 수를 반환
    public Move GetBestMove(Board board, PlayerColor side_to_move, int depth)
    {
        Move best_move = default;
        // 백이면 최대 점수를, 흑이면 최소 점수를 찾는다
        int best_score = side_to_move == PlayerColor.White ? -INF : INF;

        GameState state = new GameState(side_to_move, board);
        // 현재 차례의 모든 합법 수 생성
        List<Move> moves = state.AllLegalMovesFor(side_to_move).ToList();
        // 수 정렬(MVV-LVA 기반)
        OrderMoves(board, moves, side_to_move);

        // 둘 수 있는 수가 없으면 default 반환
        if (moves.Count == 0)
            return best_move;

        int alpha = -INF;
        int beta = INF;

        // 루트 노드에서 모든 수를 시험
        foreach (var move in moves)
        {
            Board next = board.Copy();
            move.Execute(next);

            // 상대 턴으로 들어가서 탐색
            int score = Search(next, depth - 1, alpha, beta, side_to_move.Opponent());

            if (side_to_move == PlayerColor.White)
            {
                // 백은 더 큰 평가값을 선호
                if (score > best_score)
                {
                    best_score = score;
                    best_move = move;
                }
                alpha = Math.Max(alpha, score);
            }
            else
            {
                // 흑은 더 작은 평가값을 선호
                if (score < best_score)
                {
                    best_score = score;
                    best_move = move;
                }
                beta = Math.Min(beta, score);
            }

            // 알파-베타 컷
            if (beta <= alpha)
                break;
        }

        return best_move;
    }

    // 미니맥스 + 알파베타 탐색
    private int Search(Board board, int depth, int alpha, int beta, PlayerColor side_to_move)
    {
        // 깊이가 다 떨어지면 정적 평가 or 퀘이선스 탐색
        if (depth <= 0)
        {
            // 체크 상태면 퀘이선스 탐색
            if (board.IsInCheck(side_to_move))
                return Quiescence(board, alpha, beta, side_to_move, QDEPTH_LIMIT);

            int eval = Evaluate(board);

            GameState st = new GameState(side_to_move, board);
            var ms = st.AllLegalMovesFor(side_to_move);

            // 전술적 수(잡기, 프로모션)가 있는지 확인
            bool hasTactical = ms.Any(m => IsTacticalMove(board, m));
            // 전술 수가 없으면 정적 평가
            if (!hasTactical)
                return eval;

            // 전술 수가 있으면 horizon effect 방지를 위해 퀘이선스 탐색
            return Quiescence(board, alpha, beta, side_to_move, QDEPTH_LIMIT);
        }

        GameState state = new GameState(side_to_move, board);
        List<Move> moves = state.AllLegalMovesFor(side_to_move).ToList();
        // 좋은 수부터 보게 해서 pruning 효율 향상
        OrderMoves(board, moves, side_to_move);

        // 합법 수가 없으면 체크메이트 or 스테일메이트
        if (moves.Count == 0)
        {
            if (board.IsInCheck(side_to_move))
            {
                // 현제 상태가 체크 상태이면, 체크메이트
                return side_to_move == PlayerColor.White ? -INF + 1 : INF - 1;
            }
            else
            {
                // 현제 상태가 체그 상태가 아니면, 스테일메이트
                return 0;
            }
        }

        if (side_to_move == PlayerColor.White)
        {
            // 백은 최대화
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
            //흑은 최소화
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

    // 일반 수 정렬
    // 현재는 MVV-LVA(가치 높은 말을 가치 낮은 말로 잡는 수 우선)
    private void OrderMoves(Board board, List<Move> moves, PlayerColor side_to_move)
    {
        var scored = new List<(Move move, int score)>(moves.Count);
        for (int i = 0; i < moves.Count; i++)
        {
            scored.Add((moves[i], ScoreMoveMVVLVA(board, moves[i])));
        }

        // 점수 높은 수부터 앞으로
        scored.Sort((a, b) => b.score.CompareTo(a.score));

        moves.Clear();
        for (int i = 0; i < scored.Count; i++)
        {
            moves.Add(scored[i].move);
        }
    }

    // 실제로 다음 보드까지 만들어서 점수를 주는 방식
    // 현재 코드에서는 사용되지 않지만, 체크를 거는 수 등을 반영할 수 있다.
    private int ScoreMove(Board board, Move move, PlayerColor side_to_move, int base_mat)
    {
        Board next = board.Copy();
        move.Execute(next);

        int next_mat = EvaluateMaterial(next);
        int delta = next_mat - base_mat;
        // 현재 플레이어 입장에서 이득인지 손해인지 계산
        int delta_for_side = (side_to_move == PlayerColor.White) ? delta : -delta;

        int score = 0;

        // 물질 이득을 크게 반영
        score += delta_for_side * 100;

        // 상대 킹 체크면 가산점
        if (next.IsInCheck(side_to_move.Opponent()))
        {
            score += 500;
        }

        return score;
    }

    // MVV-LVA 점수 계산
    // Most Valuable Victim - Least Valuable Attacker
    // 비싼 말을 싼 말로 잡는 수를 우선시함
    private int ScoreMoveMVVLVA(Board board, Move move)
    {
        GetFromTo(move, out int fr, out int fc, out int tr, out int tc);
        Piece attacker = board[fr, fc];

        if (attacker == null) return 0;

        int score = 0;

        // 프로모션이면 매우 높은 점수
        if (move is PawnPromotion promo)
        {
            score += 8000 + PieceValue[(int)promo.GetPromotionPieceType()];
        }

        Piece victim = board[tr, tc];

        // 앙파상도 잡기 취급
        bool isEnPassant = move is Enpassant;
        if (victim != null || isEnPassant)
        {
            int victimValue = victim != null ? PieceValue[(int)victim.Type] : PieceValue[(int)PieceType.Pawn];
            int attackerValue = PieceValue[(int)attacker.Type];
            score += 10000 + victimValue * 10 - attackerValue;
        }

        return score;
    }

    // 퀘이션스용 전술 수 정렬
    private void OrderTacticalMoves(Board board, List<Move> moves)
    {
        moves.Sort((a, b) => ScoreTactical(board, b).CompareTo(ScoreTactical(board, a)));
    }

    // 전술 수(잡기, 프로모션) 점수 계산
    private int ScoreTactical(Board board, Move move)
    {
        GetFromTo(move, out int fr, out int fc, out int tr, out int tc);

        Piece attacker = board[fr, fc];
        int attacker_value = attacker != null ? PieceValue[(int)attacker.Type] : 0;

        int score = 0;

        // 프로모션은 매우 강한 전술이므로 큰 점수
        if (move is PawnPromotion promo)
        {
            score += 20000 + PieceValue[(int)promo.GetPromotionPieceType()];
        }

        // 잡기 수면 MVV-LVA 방식으로 점수 부여
        if (IsCaptureByBoard(board, move))
        {
            Piece victim = board[tr, tc];

            int victim_value = victim != null ? PieceValue[(int)victim.Type] : PieceValue[(int)PieceType.Pawn];
            score += 10000 + victim_value * 10 - attacker_value;
        }
        return score;
    }

    // 퀘이션스 탐색
    // 일반 탐색 깊이가 끝난 뒤, 불안정한 전술 상황(잡기/프로모션)을 조금 더 본다
    private int Quiescence(Board board, int alpha, int beta, PlayerColor side_to_move, int qdepth)
    {
        // 이동성은 제외한 정적 평가
        int stand_pat = EvaluateStatic(board);

        if (side_to_move == PlayerColor.White)
        {
            //이미 beta 이상이면 더 볼 필요 없음
            if (stand_pat >= beta) return beta;
            if (stand_pat > alpha) alpha = stand_pat;
        }
        else
        {
            // 흑은 작은 값을 선호
            if (stand_pat <= alpha) return alpha;
            if (stand_pat < beta) beta = stand_pat;
        }

        // 퀘이선스 깊이 제한
        if (qdepth <= 0)
        {
            return stand_pat;
        }

        GameState state = new GameState(side_to_move, board);
        List<Move> moves = state.AllLegalMovesFor(side_to_move).ToList();

        // 둘 수기 있는 수가 없으면 체크메이트/스테일메이트 처리
        if (moves.Count == 0)
        {
            if (board.IsInCheck(side_to_move))
                return side_to_move == PlayerColor.White ? -INF + 1 : INF - 1;
            return 0;
        }

        // 전술 수만 추림
        List<Move> tactical = new List<Move>();
        for (int i = 0; i < moves.Count; i++)
        {
            if (IsTacticalMove(board, moves[i]))
            {
                tactical.Add(moves[i]);
            }
        }

        // 전술 수가 없으면 현재 정적 평가 반환
        if (tactical.Count == 0)
            return stand_pat;

        // 좋은 전술 수부터 본다
        OrderTacticalMoves(board, tactical);

        if (side_to_move == PlayerColor.White)
        {
            int best = stand_pat;

            foreach (var move in tactical)
            {
                Board next = board.Copy();
                move.Execute(next);

                int score = Quiescence(next, alpha, beta, side_to_move.Opponent(), qdepth - 1);
                // 여기서는 백이므로 최대값을 추적해야 한다
                best = Math.Max(best, score);
                alpha = Math.Max(alpha, score);
                if (alpha >= beta) break;
            }

            return best;
        }
        else
        {
            int best = stand_pat;

            foreach (var move in tactical)
            {
                Board next = board.Copy();
                move.Execute(next);

                int score = Quiescence(next, alpha, beta, side_to_move.Opponent(), qdepth - 1);
                // 흑이므로 최소값 추적
                best = Math.Min(best, score);

                beta = Math.Min(beta, best);
                if (beta <= alpha) break;
            }
            return best;
        }
    }

    // 현재 보드 기준으로 이 수가 잡기인지 판볖
    private bool IsCaptureByBoard(Board board, Move move)
    {
        GetFromTo(move, out _, out _, out int tr, out int tc);

        // 도착 칸에 말이 있으면 일반 잡기
        if (board[tr, tc] != null) return true;

        // 앙파상도 잡기
        if (move is Enpassant) return true;

        return false;
    }

    // 전술 수 판별: 잡기 또는 프로모션
    private bool IsTacticalMove(Board board, Move move)
    {
        return IsCaptureByBoard(board, move) || move is PawnPromotion;
    }

    // 전체 평가 함수
    // 양수면 백 우세, 음수면 흑 우세
    private int Evaluate(Board board)
    {
        int score = 0;

        score += EvaluateMaterial(board);       // 기물 가치
        score += EvaluatePieceSquare(board);    // 기물 위치
        score += EvaluateMobility(board);       // 이동 가능성
        score += EvaluatePawnStructure(board);  // 폰 구조
        score += EvaluateKingSafety(board);     // 킹 안정성
        score += EvaluateTempo(board);          // 템포

        return score;
    }

    //퀘이선스용 정적 평가
    //이동성을 제외해 속도를 높인다.
    private int EvaluateStatic(Board board)
    {
        int score = 0;
        score += EvaluateMaterial(board);
        score += EvaluatePieceSquare(board);
        score += EvaluatePawnStructure(board);
        score += EvaluateKingSafety(board);
        score += EvaluateTempo(board);
        return score;
    }

    // 기물 가지 평가
    // 백 기물은 +, 흑 기물은 -
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

    // Piece-Square Table 조회
    // 혹은 보드를 뒤집어서 같은 테이블을 재사용
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

    // 기물 위치 평가
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

    // 이동성 평가
    // 현재 보드에서 백/흑이 둘 수 있는 합법 수 개수 차이를 반영
    private int EvaluateMobility(Board board)
    {
        GameState state = new GameState(PlayerColor.White, board);
        int white_moves = state.AllLegalMovesFor(PlayerColor.White).Count();
        int black_moves = state.AllLegalMovesFor(PlayerColor.Black).Count();

        int factor = 2;
        return (white_moves - black_moves) * factor;
    }

    // 폰 구조 평가
    // 더블 폰, 고립된 폰, 통과된 폰 등을 반영
    private int EvaluatePawnStructure(Board board)
    {
        int score = 0;

        // 각 파일(file)별 폰 개수
        int[] white_file_count = new int[8];
        int[] black_file_count = new int[8];

        // 모든 폰 위치 저장
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
            //int[] my_black_file_ = is_white ? black_file_count : white_file_count;

            // 같은 파일에 폰이 2개 이상이면 더블 폰 패널티
            if (my_file_count[c] > 1)
            {
                score += is_white ? -10 : 10;
            }

            // 양 옆 파일에 같은 편 폰이 없으면 고립된 폰 패널티
            bool left_has = c > 0 && my_file_count[c - 1] > 0;
            bool right_has = c < 7 && my_file_count[c + 1] > 0;

            if (!left_has && !right_has)
            {
                score += is_white ? -15 : 15;
            }

            // 통과된 폰 반별용
            // 앞쪽 3개 파일에 상대 폰이 있는지 확인
            bool blocked = false;

            for (int dc = -1; dc <= 1; dc++)
            {
                int file = c + dc;
                if (file < 0 || file > 7) continue;

                if (is_white)
                {
                    // 벡 폰은 위쪽 방향 검사
                    for (int rr = r + 1; rr < 8; rr++)
                    {
                        Piece pp = board[rr, file];
                        if (pp != null && pp.Type == PieceType.Pawn && pp.Color != color)
                        {
                            blocked = true;
                            break;
                        }
                    }
                }
                else
                {
                    // 흑 폰은 아래쪽 방향 검사
                    for (int rr = r - 1; rr >= 0; rr--)
                    {
                        Piece pp = board[rr, file];
                        if (pp != null && pp.Type == PieceType.Pawn && pp.Color != color)
                        {
                            blocked = true;
                            break;
                        }
                    }
                }
                if (blocked) break;
            }
            // 앞에 막는 상대 폰이 없으면 통과된 폰 보너스
            if (!blocked)
            {
                int rank = is_white ? r : (7 - r);
                score += is_white ? (20 + rank * 5) : -(20 + rank * 5);
            }
        }

        return score;
    }

    // 킹 안정성 평가
    // 미구현
    private int EvaluateKingSafety(Board board)
    {
        return 0;
    }

    // 템포 평가
    // 미구현
    private int EvaluateTempo(Board board)
    {
        return 0;
    }

    // Move에서 시작 좌표와 도착 좌표를 꺼내는 유틸 함수
    private void GetFromTo(Move move, out int fr, out int fc, out int tr, out int tc)
    {
        fr = move.FromPos.row;
        fc = move.FromPos.column;
        tr = move.ToPos.row;
        tc = move.ToPos.column;
    }

}