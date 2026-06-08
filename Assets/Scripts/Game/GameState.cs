using System.Collections.Generic;
using System.Linq;

public class GameState
{
    // 현재 게임에서 사용중인 체스판
    public Board Board { get; }
    
    // 현재 턴인 플레이어
    public PlayerColor CurrentPlayer { get; private set; }
    
    // 게임 결과
    // null이면 아직 게임이 끝나지 않은 상태
    public Result Result { get; private set; } = null;
    
    // 잡기(capture) 또는 폰 이동이 없이 진행된 반수(half-move) 카운트
    // 50수 룰 판정에 사용
    private int no_capture_or_pawn_moves = 0;
    
    // 현재 보드 상태를 문자열로 표현한 값
    // 3회 반복 체크에 사용
    private string state_string;
    
    // 보드 상태 문자열이 몇 번 등장했는지 저장하는 딕션너리
    // key : 상태 문자열
    // value : 등장 횟수
    private readonly Dictionary<string, int> state_history = new Dictionary<string, int>();

    // 게임 상태 생성자
    // 시작 플레이어와 보드를 받아 초기 상태를 설정
    public GameState(PlayerColor player, Board board)
    {
        this.CurrentPlayer = player;
        this.Board = board;

        //현재 보드상태를 문자열로 저장
        state_string = new StateString(CurrentPlayer, board).ToString();
        
        //초기 상태는 1번 등장한 것으로 기록
        state_history[state_string] = 1;
    }

    // 특정 위치의 말리 이동할 수 있는 합법적인 수만 반환
    public IEnumerable<Move> LegalMoveForPiece(Position pos)
    {
        // 해당 칸이 비어 있거나, 현재 턴 플레이어의 말이 아니면 이동 불가
        if(Board.IsEmpty(pos) || Board[pos].Color != CurrentPlayer)
        {
            return Enumerable.Empty<Move>();
        }

        // 해당 위치의 말 가져오기
        Piece piece = Board[pos];

        // 말 종류별 가능한 이동 후보 생성
        IEnumerable<Move> moveCandiates = piece.GetMoves(pos, Board);
        
        // 그중 실제로 합법적인 수만 필터링해서 반환
        return moveCandiates.Where(move => move.IsLegal(Board));
    }

    // 실제로 수를 실행하고, 턴 변경 및 게임 종료 여부를 갱신
    public void MakeMove(Move move)
    {
        // 현재 플레이어의 앙파상 기능 상태를 먼저 초기화
        Board.SetPawnSkipPosition(CurrentPlayer, null);

        // 수를 실행
        // 반환값은 "잡기 또는 폰 이동이 있었는가" 여부
        bool capture_or_pawn_move = move.Execute(Board);

        if (capture_or_pawn_move)
        {
            // 잡기 또는 폰 이동이 있었다면 50 수 룰 카운트 초기화
            no_capture_or_pawn_moves = 0;
            
            // 상태 반복 기록도 초기화
            // 일반적으로 잡기나 폰 이동이 있으면 이전 반복 상태와의 연속성이 의미 없어짐
            state_history.Clear();
        }
        else
        {
            // 아무것도 잡지 않았고 폰도 움직이지 않았다면 반수 카운트 증가
            no_capture_or_pawn_moves++;
        }

        // 턴을 상대방으로 변경
        CurrentPlayer = CurrentPlayer.Opponent();
        
        // 새로운 상태 문자열 갱신 및 기록
        UpdateStateString();

        // 게임 종료 조건 검사
        CheckForGameOver();
    }

    // 특정 플레이어가 둘 수 있는 모든 합법적인 수 반환
    public IEnumerable<Move> AllLegalMovesFor(PlayerColor player)
    {
        // 해당 플레이어의 모든 말 위치를 순회하면서
        // 각 말의 이동 후보를 모두 모음
        IEnumerable<Move> moveCandiates = Board.PiecePositionsFor(player).SelectMany(pos =>
        {
            Piece piece = Board[pos];
            return piece.GetMoves(pos, Board);
        });

        // 그중 실제 합법 수만 반환
        return moveCandiates.Where(move => move.IsLegal(Board));
    }

    // 체크메이트, 스테일메이트, 기물 부족, 50수 룰, 3회 반복 등을 검사
    private void CheckForGameOver()
    {
        // 현재 턴 플레이어가 둘 수 있는 합법적인 수가 하나도 없을 때
        if (!AllLegalMovesFor(CurrentPlayer).Any())
        {
            // 체크 상태인데 둘 수 있는 수가 없으면 체크메이트
            if (Board.IsInCheck(CurrentPlayer))
            {
                Result = Result.Win(CurrentPlayer.Opponent());
            }
            // 체크가 아닌데 둘 수 있는 수가 없으면 스테일메이트
            else
            {
                Result = Result.Draw(EndReason.Stalemate);
            }
        }
        // 기물이 부족해서 체크메이트가 불가능한 경우 무승부
        else if (Board.InsufficientMaterial())
        {
            Result = Result.Draw(EndReason.InsufficientMaterial);
        }
        // 50수 룰 만족 시 무승부
        else if (FiftyMoveRules())
        {
            Result = Result.Draw(EndReason.FiftyMoveRule);
        }
        // 동일한 상태가 3번 반복되면 무승부
        else if (ThreefoldRepetition())
        {
            Result = Result.Draw(EndReason.ThreefoldRepetition);
        }
    }

    // 게임이 끝났는지 확인
    public bool IsGameOver()
    {
        return Result != null;
    }
    
    // 50수 룰 검사
    // 반수 100번 = 양쪽이 각각 50번씩 둔 것
    private bool FiftyMoveRules()
    {
        int full_moves = no_capture_or_pawn_moves / 2;
        return full_moves >= 50;
    }

    // 현재 보드 상태 문자열을 갱신하고 등장 횟수를 기록
    public void UpdateStateString()
    {
        // 현재 턴 플레이어와 보드 상태를 문자열로 반환
        state_string = new StateString(CurrentPlayer, Board).ToString();

        // 처음 등장한 상태면 1로 등록
        if (!state_history.ContainsKey(state_string))
        {
            state_history[state_string] = 1;
        }
        else
        {
            // 이미 존재한 상태면 등장 횟수 증가
            state_history[state_string]++;
        }
    }

    // 같은 상태가 3번 등장했는지 확인
    private bool ThreefoldRepetition()
    {
        return state_history[state_string] == 3;
    }

    public void MakeMoveForTraining(Move move)
    {
        Board.SetPawnSkipPosition(CurrentPlayer, null);

        bool capture_or_pawn_move = move.Execute(Board);

        if (capture_or_pawn_move)
        {
            no_capture_or_pawn_moves = 0;
            state_history.Clear();
        }
        else
        {
            no_capture_or_pawn_moves++;
        }

        CurrentPlayer = CurrentPlayer.Opponent();

        // 학습 중에는 무거운 반복 상태 문자열 생성과 전체 게임 종료 검사를 매번 하지 않음
    }

    public bool HasAnyLegalMove(PlayerColor player)
    {
        return AllLegalMovesFor(player).Any();
    }

    public bool IsInCheck(PlayerColor player)
    {
        return Board.IsInCheck(player);
    }

    public void SetTrainingResult(Result result)
    {
        Result = result;
    }
}
