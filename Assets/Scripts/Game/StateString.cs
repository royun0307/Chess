using System.Text;
using UnityEditor.Build;
using UnityEngine.Rendering;

// 현재 체스판 상태를 문자열로 변환하는 클래스
// 주로 3회 반복 체크나 상태 비교를 위해 사용
public class StateString
{
    // 문자열을 효율적으로 이어붙이기 위한 StringBuilder
    private readonly StringBuilder sb = new StringBuilder();

    // 생성자
    // 현재 플레이어와 보드 상태를 받아 상태 문자열을 구성
    public StateString(PlayerColor current_player, Board board)
    {
        AddPiecePlacement(board);           // 기물 배치 정보 추가
        sb.Append(' ');
        AddCurrentPlayer(current_player);   // 현재 턴 플레이어 추가
        sb.Append(' ');
        AddCastlingRights(board);           // 캐슬링 가능 여부 추가
        sb.Append(' ');
        AddEnPassant(board, current_player);// 앙파상 가능 위치 추가
    }

    // 최종적으로 완성된 상태 문자열 반환
    public override string ToString()
    {
        return sb.ToString();
    }

    // 기물을 FEN 스타일 문자로 변환
    // 혹은 소문자, 백은 대문자 사용
    private static char PieceChar(Piece piece)
    {
        char c = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => ' '
        };

        // 백 기물은 대문자로 변환
        if (piece.Color == PlayerColor.White)
        {
            return char.ToUpper(c);
        }

        // 흑 기물은 그대로 소문자 사용
        return c;
    }

    // 한 줄(row)의 기물 배치 정보를 문자열의 추가
    private void AddRowData(Board board, int row)
    {
        // 연속된 빈칸 개수 저장
        int empty = 0;

        for (int c = 0; c < 8; c++)
        {
            // 현재 칸이 비어 있으면 empty 증가
            if (board[row, c] == null)
            {
                empty++;
                continue;
            }

            // 이전까지 빈칸이 있었다면 숫자로 기록
            if(empty > 0)
            {
                sb.Append(empty);
                empty = 0;
            }

            // 기물이 있으면 해당 기물 문자 추가
            sb.Append(PieceChar(board[row, c]));
        }

        // 행 끝까지 왔는데 빈칸이 남아 있으면 기록
        if (empty > 0)
        {
            sb.Append(empty);
        }
    }

    // 전체 보드의 기물 배치 정보를 문자열의 추가
    // 각 행은 '/'로 구분
    private void AddPiecePlacement(Board board)
    {
        for (int r = 0; r < 8; r++)
        {
            if (r != 0)
            {
                sb.Append('/');
            }
            AddRowData(board, r);
        }
    }

    // 현재 턴 플레이어르르 문자열에 추가
    // 백이면 w, 흑이면 b
    private void AddCurrentPlayer(PlayerColor current_player)
    {
        if(current_player == PlayerColor.White)
        {
            sb.Append('w');
        }
        else
        {
            sb.Append('b');
        }
    }

    // 캐슬링 가능 여부를 문자열에 추가
    // 백 킹사이드: K
    // 백 퀸사이드: Q
    // 흑 킹사이드: k
    // 흑 퀸사이드: q
    // 흑, 백 둘 다 캐슬링 불가면: -
    private void AddCastlingRights(Board board)
    {
        bool castleWKS = board.CastleRightKS(PlayerColor.White);
        bool castleWQS = board.CastleRightQS(PlayerColor.White);
        bool castleBKS = board.CastleRightKS(PlayerColor.Black);
        bool castleBQS = board.CastleRightQS(PlayerColor.Black);

        // 어떤 캐슬링도 불가능하면 '-'
        if (!(castleWKS || castleWQS || castleBKS || castleBQS))
        {
            sb.Append('-');
            return;
        }

        if (castleWKS)
        {
            sb.Append('K');
        }
        if (castleWQS)
        {
            sb.Append('Q');
        }
        if (castleBKS)
        {
            sb.Append('k');
        }
        if (castleBQS)
        {
            sb.Append('q');
        }
    }

    // 앙파상 가능 위치를 문자열에 추가
    // 가능하지 않으면 '-'
    private void AddEnPassant(Board board, PlayerColor current_player)
    {
        // 현제 플레이어가 앙파상으로 잡을 수 없으면 '-'
        if (!board.CanCaptureEnPassant(current_player))
        {
            sb.Append('-');
            return;
        }

        // 상대가 직전에 2칸 전진한 폰의 위치를 가져옴
        Position pos = board.GetPawnSkipPosition(current_player.Opponent());
        
        // 열(column)을 체스 표기법 파일 문자(a~h)로 변환
        char file = (char)('a' + pos.column);

        // 행(row)을 체스 표기법 랭크(1~8)로 변환
        int rank = 8 - pos.row;

        // 예: e3 같은 형태로 기록
        sb.Append(file);
        sb.Append(rank);
    }
}
