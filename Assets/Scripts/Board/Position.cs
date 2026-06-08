using System;
using System.Collections.Generic;

public class Position
{
    // 체스판 좌표 (행, 열)
    public int row { get; }
    public int column { get; }

    // 좌표 생성자
    public Position(int row, int column)
    {
        this.row = row;
        this.column = column;
    }

    // 현재 좌표의 칸 색상 반환
    // (행 + 열)이 짝수면 White 칸, 홀수면 Black 칸으로 판정
    public PlayerColor SquareColor()
    {
        if((row + column) %2 == 0)
        {
            return PlayerColor.White;
        }
        return PlayerColor.Black;
    }

    // 같은 row, column 값을 가지면 같은 위치로 판단
    public override bool Equals(object obj)
    {
        // 같은 참조면 동일 객체
        if (ReferenceEquals(this, obj)) return true;
        // Position 타입이 아니면 false
        if (obj is not Position other) return false;

        // 좌표값 비교
        return row == other.row && column == other.column;
    }

    // 해시 기반 컬렉션(Dictionary, HashSet 등)에서 사용되는 해시코드
    // Equals 기준(row, column)과 동일하게 구성해야 함
    public override int GetHashCode()
    {
        return HashCode.Combine(row, column);
    }

    // == 연산자 오버로드
    public static bool operator ==(Position left, Position right)
    {
        return EqualityComparer<Position>.Default.Equals(left, right);
    }

    // != 연산자 오버로드
    public static bool operator !=(Position left, Position right)
    {
        return !(left == right);
    }

    // Position + Direction 연산자 오버로드
    public static Position operator +(Position pos, Direction dir)
    {
        return new Position(pos.row + dir.row_delta, pos.column + dir.col_delta);
    }
}
