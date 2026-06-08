using System.Collections.Generic;
using System.Linq;

// 비숍(대각선으로 이동하는 체스 기물) 클래스
public class Bishop : Piece
{
    // 이 기물의 종류는 Bishop
    public override PieceType Type => PieceType.Bishop;
    
    // 이 기물의 색상(흑/백)
    public override PlayerColor Color { get; }

    // 비숍의 이동할 수 있는 4개의 대각선 방향
    private static readonly Direction[] dirs = new Direction[]
    {
        Direction.NorthWest, // 좌상향
        Direction.NorthEast, // 우상향
        Direction.SouthWest, // 좌하향
        Direction.SouthEast, // 좌우향
    };

    // 비숍 생성자
    // 생성할 때 기물의 색상을 받아 저장
    public Bishop(PlayerColor color)
    {
        this.Color = color;
    }

    // 현재 비숍 객체를 복사해서 새로운 비숍 객체를 반환
    public override Piece Copy()
    {
        // 같은 색상의 비숍 생성
        Bishop copy = new Bishop(Color);

        // 이동 여부(hasMoved)도 함꼐 복사
        copy.hasMoved = hasMoved;

        return copy;
    }

    // 현재 위치(from)에서 이동 가능한 모든 수를 반환
    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        // 대각선 4방향으로 갈 수 있는 모든 위치를 구한 뒤
        // 각 위치를 NormalMove 객체로 변환해서 반환
        return MovePositionsInDirs(from, board, dirs).Select(to => new NormalMove(from, to));
    }
}
