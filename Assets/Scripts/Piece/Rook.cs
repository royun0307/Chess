using System.Collections.Generic;
using System.Linq;

// 룩(상하좌우 직선으로 이동하는 체스 기물) 클래스
public class Rook : Piece
{
    // 이 기물의 종류는 Rook
    public override PieceType Type => PieceType.Rook;
    
    // 이 기물의 색상(흑/벡)
    public override PlayerColor Color { get; }

    // 룩이 이동할 수 있는 4개의 방향
    // 상, 하, 좌, 우 방향으로만 이동가능
    private static readonly Direction[] dirs = new Direction[]
    {
        Direction.North,    // 위
        Direction.South,    // 아래
        Direction.East,     // 오른쪽
        Direction.West,     // 왼쪽
    };

    // 룩 생성자
    // 생성할 때 기물의 색상을 받아 저장
    public Rook(PlayerColor color)
    {
        this.Color = color;
    }

    // 현재 룩 객체를 복사해서 새로운 룩 객체를 반환
    public override Piece Copy()
    {
        // 같은 색상의 룩 생성
        Rook copy = new Rook(Color);

        // 이동 여부(hasMoved)도 함께 복사
        copy.hasMoved = hasMoved;

        return copy;
    }

    // 현재 위치(from)에서 이동 가능한 모든 수를 반환
    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        // 상하좌우 4방향으로 갈 수 있는 모든 위치를 구한 뒤
        // 각 위치를 NormalMove 객체로 변환해서 반환
        return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
    }
}
