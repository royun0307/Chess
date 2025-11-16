public class Direction
{
    public readonly static Direction North = new Direction(-1, 0);
    public readonly static Direction South = new Direction(1, 0);
    public readonly static Direction East = new Direction(0, 1);
    public readonly static Direction West = new Direction(0, -1);
    public readonly static Direction NorthEast = North + East;
    public readonly static Direction NorthWest = North + West;
    public readonly static Direction SouthEast = South + East;
    public readonly static Direction SouthWest = South + West;


    public int row_delta { get; }
    public int col_delta { get; }

    public Direction(int row_delta, int col_delta)
    {
        this.row_delta = row_delta;
        this.col_delta = col_delta;
    }

    public static Direction operator +(Direction dir1, Direction dir2)
    {
        return new Direction(dir1.row_delta + dir2.row_delta, dir1.col_delta + dir2.col_delta);
    }

    public static Direction operator *(int scalar, Direction dir)
    {
        return new Direction(scalar * dir.row_delta, scalar * dir.col_delta);
    }
}
