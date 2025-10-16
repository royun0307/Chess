using System;
using System.Collections.Generic;

public class Position
{
    public int row { get; }
    public int column { get; }

    public Position(int row, int column)
    {
        this.row = row;
        this.column = column;
    }

    public PlayerColor SquareColor()
    {
        if((row + column) %2 == 0)
        {
            return PlayerColor.White;
        }
        return PlayerColor.Black;
    }

    public override bool Equals(object obj)
    {
        return obj is Position position &&
               base.Equals(obj) &&
               row == position.row &&
               column == position.column;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), row, column);
    }

    public static bool operator ==(Position left, Position right)
    {
        return EqualityComparer<Position>.Default.Equals(left, right);
    }

    public static bool operator !=(Position left, Position right)
    {
        return !(left == right);
    }

    public static Position operator +(Position pos, Direction dir)
    {
        return new Position(pos.row + dir.row_delta, pos.column + dir.col_delta);
    }
}
