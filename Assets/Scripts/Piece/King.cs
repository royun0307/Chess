using System.Collections.Generic;
using System.Linq;

// 킹(한 칸씩 이동하고 캐슬링이 가능한 체스 기물) 클래스
public class King : Piece
{
    // 이 기물의 종류는 King
    public override PieceType Type => PieceType.King;
    
    // 이 기물의 색상(흑/백)
    public override PlayerColor Color { get; }

    // 킹이 이동할 수 있는 8개의 방향
    private static readonly Direction[] dirs = new Direction[]
    {
        Direction.North,        // 위
        Direction.South,        // 아래
        Direction.East,         // 오른쪽
        Direction.West,         // 왼쪽
        Direction.NorthEast,    // 우상향
        Direction.NorthWest,    // 좌상향
        Direction.SouthEast,    // 우하향
        Direction.SouthWest,    // 좌하향
    };

    // 킹 생성자
    // 생성할 때 기물의 색상을 받아 저장
    public King(PlayerColor color)
    {
        this.Color = color;
    }

    // 해당 위치에 아직 움직이지 않은 룩이 있는지 확인
    // 캐슬링 가능 여부를 검사할 때 사용
    private static bool IsUnmovedRook(Position pos, Board board)
    {
        // 해당 칸이 비어 있으면 룩이 있을 수 없으므로 false
        if (board.IsEmpty(pos))
        {
            return false;
        }

        // 해당 위치의 기물이 룩이고 아직 움직이지 않았는지 확인
        Piece piece = board[pos];
        return piece.Type == PieceType.Rook && !piece.hasMoved;
    }

    // 전달된 모든 위치가 비어 있는지 확인
    // 킹과 룩 사이에 다른 기물이 없는지 검사할 때 사용
    private static bool AllEmpty(IEnumerable<Position> positions, Board board)
    {
        return positions.All(pos => board.IsEmpty(pos));
    }

    // 킹사이드 캐승링 가능 여부를 확인
    private bool CanCastleKingSide(Position from, Board board)
    {
        // 킹이 한 번이라도 움직였으면 캐슬링 불가
        if(hasMoved)
        {
            return false;
        }

        // 같은 행의 맨 오른쪽 룩 위치
        Position rook_pos = new Position(from.row, 7);
        
        // 킹과 룩 사이의 칸들
        Position[] between_positions = new Position[] { new(from.row, 5), new(from.row, 6) };

        // 움직이지 않은 룩이 있고, 사이 칸이 모두 비어 있으면 가능
        return IsUnmovedRook(rook_pos, board) && AllEmpty(between_positions, board);
    }

    // 퀸사이드 캐슬링 가능 여부를 확인
    private bool CanCastleQueenSide(Position from, Board board)
    {
        // 킹이 한 번이라도 움직였으면 캐슬링 불가
        if (hasMoved)
        {
            return false;
        }

        // 같은 행의 맨 왼쪽 룩 위치
        Position rook_pos = new Position(from.row, 0);
        
        // 킹과 룩 사이의 칸들
        Position[] between_positions = new Position[] { new(from.row, 1), new(from.row, 2), new(from.row, 3) };

        // 움직이지 않은 룩이 있고, 사이 칸이 모두 비어 있으면 가능
        return IsUnmovedRook(rook_pos, board) && AllEmpty(between_positions, board);
    }

    // 현재 킹 객체를 복사해서 새로운 킹 객체를 반환
    public override Piece Copy()
    {
        // 같은 색상의 킹 생성
        King copy = new King(Color);

        // 이동 여부(hasMoved)도 함께 복사
        copy.hasMoved = hasMoved;

        return copy;
    }

    // 현재 위치(from)에서 킹이 일반적으로 이동 가능한 위치들을 계산
    private IEnumerable<Position> MovePositions(Position from, Board board)
    {
        // 8방향을 하나씩 확인
        foreach (Direction dir in dirs)
        { 
            Position to = from + dir;

            // 보드 밖으로 나가면 무시
            if (!Board.IsInside(to))
            {
                continue;
            }

            // 빈 칸이거나 상대 기물이 있는 칸이면 이동 가능
            if(board.IsEmpty(to) || board[to].Color != Color)
            {
                yield return to;
            }
        }
    }

    // 햔재 위치(from)에서 이동 가능한 모든 수를 반환
    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        // 일반 이동 가능한 위치들을 NormalMove로 반환
        foreach (Position to in MovePositions(from, board))
        {
            yield return new NormalMove(from, to);
        }

        // 킹사이드 캐슬링이 가능하면 해당 이동 추가
        if(CanCastleKingSide(from, board))
        {
            yield return new Castle(MoveType.CastleKS, from);
        }

        // 퀸사이드 캐슬링이 가능하면 해당 이동 추가
        if(CanCastleQueenSide(from, board))
        {
            yield return new Castle(MoveType.CastleQS, from);
        }
    }

    // 현재 킹이 상대 킹을 공격할 수 있는지 확인
    public override bool CanCaptureOpponentKing(Position from, Board board)
    {
        return MovePositions(from, board).Any(to =>
        {
            Piece piece = board[to];
            return piece != null && piece.Type == PieceType.King;
        });
    }
}
