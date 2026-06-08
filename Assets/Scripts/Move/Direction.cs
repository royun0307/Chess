public class Direction
{
    // 위쪽으로 이동하는 방향 단위 벡터
    // 행(row)은 감소하고, 열(col)은 그대로
    public readonly static Direction North = new Direction(-1, 0);

    // 아래쪽으로 이동하는 방향 단위 벡터
    // 행(row)은 증가하고, 열(col)은 그대로
    public readonly static Direction South = new Direction(1, 0);

    // 오른쪽으로 이동하는 방향 단위 벡터
    // 행(row)은 그대로, 열(col)은 증가
    public readonly static Direction East = new Direction(0, 1);

    // 왼쪽으로 이동하는 방향 단위 벡터
    // 행(row)은 그대로, 열(col)은 감소
    public readonly static Direction West = new Direction(0, -1);

    // 대각선 방향 단위 벡터들
    // 기존 방향 단위 벡터들을 더해서 생성
    public readonly static Direction NorthEast = North + East; // 오른쪽 위
    public readonly static Direction NorthWest = North + West; // 왼쪽 위
    public readonly static Direction SouthEast = South + East; // 오른쪽 아래
    public readonly static Direction SouthWest = South + West; // 왼쪽 아래

    // 행 방향 변화량
    // 예: 위로 가면 -1, 아래로 가면 +1
    public int row_delta { get; }

    // 열 방향 변화량
    // 예: 왼쪽으로 가면 -1, 오른쪽으로 가면 +1
    public int col_delta { get; }

    public Direction(int row_delta, int col_delta)
    {
        // 방향이 가지는 행/열 변화량 저장
        this.row_delta = row_delta;
        this.col_delta = col_delta;
    }

    public static Direction operator +(Direction dir1, Direction dir2)
    {
        // 두 방향을 더해서 새로운 뱡향 생성
        // 예: North + East = NorthEast
        return new Direction(dir1.row_delta + dir2.row_delta, dir1.col_delta + dir2.col_delta);
    }

    public static Direction operator *(int scalar, Direction dir)
    {
        // 방향에 정수를 곱해서 이동거리 확장
        // 예: 2 * North = 위로 2칸
        return new Direction(scalar * dir.row_delta, scalar * dir.col_delta);
    }
}
