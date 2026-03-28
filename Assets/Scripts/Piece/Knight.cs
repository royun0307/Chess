using System.Collections.Generic;
using System.Linq;

// 나이트(L자 형태로 이동하는 체스 기물) 클래스
public class Knight : Piece
{
    // 이 기물의 종류는 Knight
    public override PieceType Type => PieceType.Knight;
    
    // 이 기물의 색상(흑/백)
    public override PlayerColor Color { get; }

    // 나이트 생성자
    // 생성할 때 기물의 색상을 받아 저장
    public Knight(PlayerColor color)
    {
        this.Color = color;
    }

    // 현재 나이트 객체를 복사해서 새로운 나이트 객체를 반환
    public override Piece Copy()
    {
        // 같은 색상의 나이트 생성
        Knight copy = new Knight(Color);
        
        // 이동 여부(hasMoved)도 함께 복사
        copy.hasMoved = hasMoved;

        return copy;
    }

    // 현재 위치(from)에서 나이트가 갈 수 있는 모든 후보 위치를 생성
    // 나이트는 세로 2칸 + 가로 1칸, 또는 세로 1칸 + 가로 2칸 이동한다
    private static IEnumerable<Position> PotentialToPosition(Position from)
    {
        foreach(Direction vDir in new Direction[] { Direction.North, Direction.South })
        {
            foreach(Direction hDir in new Direction[] { Direction.West, Direction.East })
            {
                yield return from + 2 * vDir + hDir;
                yield return from + vDir + 2 * hDir;
            }
        }
    }

    // 후보 위치들 중 실제로 이동 가능한 위치만 반환
    // 보드 안에 있어야 하고, 빈 칸이거나 상대 기물이 있는 칸이어야 한다
    private IEnumerable<Position> MovePositions(Position from, Board board)
    {
        return PotentialToPosition(from).Where(pos => Board.IsInside(pos) 
            && (board.IsEmpty(pos) || board[pos].Color != Color));
    }

    // 현재 위치(from)에서 이동 가능한 모든 수를 반환 
    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        // 이동 가능한 각 위치를 NormalMove 객체로 변환해서 반환
        return MovePositions(from, board).Select(to => new NormalMove(from, to));
    }
}
