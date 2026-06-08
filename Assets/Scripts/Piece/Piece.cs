using System.Collections.Generic;
using System.Linq;

// 체스 기물의 종류를 나타내는 열거형
public enum PieceType
{
    Pawn,   // 폰
    Bishop, // 비숍
    Knight, // 나이트
    Rook,   // 룩
    Queen,  // 퀸
    King    // 킹
}

// 모든 체스 기물이 공통으로 상속받는 추상 클래스
public abstract class Piece
{
    // 기물의 종류
    public abstract PieceType Type { get; }
    
    // 기물의 색상(흑/백)
    public abstract PlayerColor Color { get; }
    
    // 해당 기물이 한 번이라도 움직였는지 여부
    // 캐슬링, 폰의 두 칸 전진 등에서 사용
    public bool hasMoved { get; set; } = false;

    // 현재 기물을 복사한 새로운 객체를 반환
    public abstract Piece Copy();

    // 현재 위치(from)에서 이동 가능한 모든 수를 반환
    public abstract IEnumerable<Move> GetMoves(Position from, Board board);

    // 한 방향(dir)으로 계속 이동할 수 있는 위치들을 반환
    // 비숍, 룩, 퀸처럼 직선으로 여러 칸 이동하는 기물들이 사용
    protected IEnumerable<Position> MovePositionInDir(Position from, Board board, Direction dir)
    {
        // 현재 위치에서 dir 방향으로 한 칸 씩 전진하면서 확인
        for (Position pos = from + dir; Board.IsInside(pos); pos += dir)
        {
            // 빈 칸이면 계속 이동 가능하므로 반환하고 다음 칸도 탐색
            if (board.IsEmpty(pos))
            {
                yield return pos;
                continue;
            }

            // 기물이 있는 칸이면 그 기물을 확인
            Piece piece = board[pos];

            // 상대 기물이 있의면 잡을 수 있으므로 해당 위치 반환
            if (piece.Color != Color)
            {
                yield return pos;
            }

            // 기물이 있는 칸 이후로는 더 진행할 수 없으므로 종료
            yield break;
        }
    }

    // 여러 방향(dirs)에 대해 이동 가능한 위치들을 한 번에 반환
    // 각 방향별 이동 결과를 하나의 열거형으로 합쳐 준다
    protected IEnumerable<Position> MovePositionsInDirs(Position from, Board board, Direction[] dirs)
    {
        return dirs.SelectMany(dir => MovePositionInDir(from, board, dir));
    }

    // 현재 기물이 상대 킹을 공격할 수 있는지 확인
    // 기본적으로 GetMoves 결과를 검사하며,
    // 필요하면 자식 클래스에서 override하며 별도로 구현할 수 있다
    public virtual bool CanCaptureOpponentKing(Position from, Board board)
    {
        return GetMoves(from, board).Any(move =>
        {
            Piece piece = board[move.ToPos];
            return piece != null && piece.Type == PieceType.King;
        });
    }
}

