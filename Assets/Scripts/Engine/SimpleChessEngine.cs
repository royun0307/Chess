using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;

//간단한 체스 엔진 구현
public class SimpleChessEngine : IBudgetedChessEngine
{
    // 탐색에서 사용하는 매우 큰 값
    private const int INF = 1000000;
    
    // 퀘이선스 탐색 최대 깊이
    private const int QDEPTH_LIMIT = 8;

    // 기물 기본 가치
    static readonly IReadOnlyDictionary<PieceType, int> PieceValue = new Dictionary<PieceType, int>
    {
        [PieceType.Pawn] = 100,
        [PieceType.Knight] = 320,
        [PieceType.Bishop] = 330,
        [PieceType.Rook] = 500,
        [PieceType.Queen] = 900,
        [PieceType.King] = 100000
    };

    //  현재 보드와 차례, 탐색 깊이를 받아 최선의 수를 반환
    public Move GetBestMove(Board board, PlayerColor side_to_move, int depth)
    {
        return SearchAtDepth(board, side_to_move, depth, null);
    }

    private sealed class SearchTimeoutException : Exception { }
    private sealed class SearchControl
    {
        public readonly Stopwatch Clock = Stopwatch.StartNew();
        public long Nodes;
        public int RootScore;
        private readonly TimeSpan limit;
        private readonly CancellationToken token;
        public SearchControl(TimeSpan limit, CancellationToken token) { this.limit = limit; this.token = token; }
        public void Check()
        {
            token.ThrowIfCancellationRequested();
            if (Clock.Elapsed >= limit) throw new SearchTimeoutException();
        }
        public void Visit() { Check(); Nodes++; }
    }

    public EngineSearchResult FindBestMove(Board board, PlayerColor side, int maxDepth, TimeSpan timeLimit, CancellationToken token)
    {
        if (board == null) throw new ArgumentNullException(nameof(board));
        if (maxDepth < 1 || maxDepth > 64) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        if (timeLimit < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeLimit));
        token.ThrowIfCancellationRequested();
        var control = new SearchControl(timeLimit, token);
        var best = new GameState(side, board).AllLegalMovesFor(side).FirstOrDefault();
        token.ThrowIfCancellationRequested();
        if (best == null || board.InsufficientMaterial())
            return new EngineSearchResult(null, 0, 0, control.Clock.Elapsed, false);
        int completed = 0;
        int? evaluation = null;
        bool timedOut = false;
        try
        {
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                control.Check();
                var candidate = SearchAtDepth(board, side, depth, control);
                control.Check();
                best = candidate;
                completed = depth;
                evaluation = control.RootScore;
            }
        }
        catch (SearchTimeoutException) { timedOut = true; }
        token.ThrowIfCancellationRequested();
        return new EngineSearchResult(best, completed, control.Nodes, control.Clock.Elapsed, timedOut, evaluation);
    }

    private Move SearchAtDepth(Board board, PlayerColor side_to_move, int depth, SearchControl control)
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
        if (moves.Count == 0 || board.InsufficientMaterial())
            return best_move;

        int alpha = -INF;
        int beta = INF;

        // 루트 노드에서 모든 수를 시험
        foreach (var move in moves)
        {
            Board next = NextBoard(board, move, side_to_move);

            // 상대 턴으로 들어가서 탐색
            int score = SearchNode(next, Math.Max(1, depth) - 1, alpha, beta, side_to_move.Opponent(), 1, control);

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

        if (control != null) control.RootScore = best_score;
        return best_move;
    }

    // 미니맥스 + 알파베타 탐색
    private int Search(Board board, int depth, int alpha, int beta, PlayerColor side_to_move)
    {
        return SearchNode(board, depth, alpha, beta, side_to_move, 0);
    }

    private int SearchNode(Board board, int depth, int alpha, int beta, PlayerColor side_to_move, int ply, SearchControl control = null)
    {
        control?.Visit();
        var moves = new GameState(side_to_move, board).AllLegalMovesFor(side_to_move).ToList();
        // 깊이 제한이나 정적 평가보다 실제 종료 상태가 우선이다.
        if (TryTerminalScore(board, side_to_move, moves, ply, out int terminal)) return terminal;
        if (depth <= 0)
        {
            if (!board.IsInCheck(side_to_move) && !moves.Any(m => IsTacticalMove(board, m)))
                return Evaluate(board);
            return QuiescenceNode(board, alpha, beta, side_to_move, QDEPTH_LIMIT, ply, control);
        }
        OrderMoves(board, moves, side_to_move);

        if (side_to_move == PlayerColor.White)
        {
            // 백은 최대화
            int value = -INF;

            foreach (var move in moves)
            {
                Board next = NextBoard(board, move, side_to_move);

                int score = SearchNode(next, depth - 1, alpha, beta, side_to_move.Opponent(), ply + 1, control);
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
                Board next = NextBoard(board, move, side_to_move);

                int score = SearchNode(next, depth - 1, alpha, beta, side_to_move.Opponent(), ply + 1, control);
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
        Board next = NextBoard(board, move, side_to_move);

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
            score += 8000 + PieceValue[promo.GetPromotionPieceType()];
        }

        Piece victim = board[tr, tc];

        // 앙파상도 잡기 취급
        bool isEnPassant = move is Enpassant;
        if (victim != null || isEnPassant)
        {
            int victimValue = victim != null ? PieceValue[victim.Type] : PieceValue[PieceType.Pawn];
            int attackerValue = PieceValue[attacker.Type];
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
        int attacker_value = attacker != null ? PieceValue[attacker.Type] : 0;

        int score = 0;

        // 프로모션은 매우 강한 전술이므로 큰 점수
        if (move is PawnPromotion promo)
        {
            score += 20000 + PieceValue[promo.GetPromotionPieceType()];
        }

        // 잡기 수면 MVV-LVA 방식으로 점수 부여
        if (IsCaptureByBoard(board, move))
        {
            Piece victim = board[tr, tc];

            int victim_value = victim != null ? PieceValue[victim.Type] : PieceValue[PieceType.Pawn];
            score += 10000 + victim_value * 10 - attacker_value;
        }
        return score;
    }

    // 퀘이션스 탐색
    // 일반 탐색 깊이가 끝난 뒤, 불안정한 전술 상황(잡기/프로모션)을 조금 더 본다
    private int Quiescence(Board board, int alpha, int beta, PlayerColor side_to_move, int qdepth)
    {
        return QuiescenceNode(board, alpha, beta, side_to_move, qdepth, 0);
    }

    private int QuiescenceNode(Board board, int alpha, int beta, PlayerColor side_to_move, int qdepth, int ply, SearchControl control = null)
    {
        control?.Visit();
        var moves = new GameState(side_to_move, board).AllLegalMovesFor(side_to_move).ToList();
        if (TryTerminalScore(board, side_to_move, moves, ply, out int terminal)) return terminal;
        bool inCheck = board.IsInCheck(side_to_move);
        bool maximize = side_to_move == PlayerColor.White;
        int best = maximize ? -INF : INF;

        // 체크일 때는 pass를 뜻하는 stand-pat도, 전술 수만 남기는 필터도 금지한다.
        if (!inCheck)
        {
            best = EvaluateStatic(board);
            if (qdepth <= 0) return best;
            if (maximize)
            {
                if (best >= beta) return best;
                alpha = Math.Max(alpha, best);
            }
            else
            {
                if (best <= alpha) return best;
                beta = Math.Min(beta, best);
            }
            moves.RemoveAll(m => !IsTacticalMove(board, m));
        }

        OrderTacticalMoves(board, moves);
        foreach (var move in moves)
        {
            Board next = NextBoard(board, move, side_to_move);
            // 깊이를 소진한 체크 국면에서도 반드시 한 번 회피한 뒤 평가한다.
            // 자식의 종료 여부를 먼저 확인하며, 재귀 연장은 여기서 끝내 유한하게 유지한다.
            int score = qdepth <= 0
                ? EvaluateLeaf(next, side_to_move.Opponent(), ply + 1, control)
                : QuiescenceNode(next, alpha, beta, side_to_move.Opponent(), qdepth - 1, ply + 1, control);
            best = maximize ? Math.Max(best, score) : Math.Min(best, score);
            if (maximize) alpha = Math.Max(alpha, best);
            else beta = Math.Min(beta, best);
            if (alpha >= beta) break;
        }
        return best;
    }

    private int EvaluateLeaf(Board board, PlayerColor side, int ply, SearchControl control = null)
    {
        control?.Visit();
        var moves = new GameState(side, board).AllLegalMovesFor(side).ToList();
        return TryTerminalScore(board, side, moves, ply, out int terminal) ? terminal : EvaluateStatic(board);
    }

    private static bool TryTerminalScore(Board board, PlayerColor side, List<Move> moves, int ply, out int score)
    {
        score = 0;
        if (moves.Count == 0)
        {
            if (board.IsInCheck(side))
                score = side == PlayerColor.White ? -INF + 1 + ply : INF - 1 - ply;
            return true;
        }
        return board.InsufficientMaterial();
    }

    private static Board NextBoard(Board board, Move move, PlayerColor side)
    {
        var next = new GameState(side, board.Copy());
        next.MakeMoveForTraining(move);
        return next.Board;
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

                int v = PieceValue[p.Type];
                score += p.Color == PlayerColor.White ? v : -v;
            }
        }

        return score;
    }

    // Piece-Square Table 조회
    // 혹은 보드를 뒤집어서 같은 테이블을 재사용
    int GetPST(Piece p, int r, int c)
    {
        // PST의 row 0은 백의 1랭크, Board의 row 0은 8랭크다.
        int rr = p.Color == PlayerColor.White ? 7 - r : r;

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
                else
                {
                    // 흑 폰은 아래쪽 방향 검사
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
                if (blocked) break;
            }
            // 앞에 막는 상대 폰이 없으면 통과된 폰 보너스
            if (!blocked)
            {
                int rank = is_white ? 7 - r : r;
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
