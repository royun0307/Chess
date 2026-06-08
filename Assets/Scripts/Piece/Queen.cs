using System.Collections.Generic;
using System.Linq;

// 퀸(직선과 대각선 모든 방향으로 이동하는 체스 기물) 클래스
public class Queen : Piece
{
    // 이 기물의 종류는 Queen
    public override PieceType Type => PieceType.Queen;
    
    // 이 기물의 색상(흑/백)
    public override PlayerColor Color { get; }

    // 퀸이 이동할 수 있는 8개의 방향
    // 상하좌우 + 4개의 대각선 방향으로 모두 이동 가능
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

    // 퀸 생성자
    // 생성할 때 기물의 색상을 받아 저장
    public Queen(PlayerColor color)
    {
        this.Color = color;
    }

    // 현재 퀸 객체를 복사해서 새로운 퀸 객체를 반환
    public override Piece Copy()
    {
        // 같은 색상의 퀸 생성
        Queen copy = new Queen(Color);

        // 이동 여부(hasMoved)도 함께 복사
        copy.hasMoved = hasMoved;

        return copy;
    }

    // 현재 위치(from)에서 이동 가능한 모든 수를 반환
    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        // 8방향으로 갈 수 있는 모든 위치를 구한 뒤
        // 각 위치를 NormalMove 객체로 변환해서 반환
        return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
    }
}
