using System;
using System.Collections.Generic;

public class Counting
{
    // 백 기물 개수 저장용 딕셔너리
    private readonly Dictionary<PieceType, int> white_count = new();
    // 흑 기물 개수 저장용 딕셔너리
    private readonly Dictionary<PieceType, int> black_count = new();

    // 보드 위 전체 기물 수
    public int TotalCount { get; private set; }

    // 생성자: 모든 PieceType에 대해 초기 카운트를 0으로 세팅
    public Counting()
    {
        foreach (PieceType type in Enum.GetValues(typeof(PieceType)))
        {
            white_count[type] = 0;
            black_count[type] = 0;
        }
    }

    // 특정 색상의 특정 기물 개수를 1 증가
    public void Increment(PlayerColor color, PieceType type)
    {
        if (color == PlayerColor.White)
        {
            white_count[type]++;
        }
        else if (color == PlayerColor.Black)
        {
            black_count[type]++;
        }

        // 색상과 관계없이 전체 기물 수도 증가
        TotalCount++;
    }

    // 백의 특정 기물 개수 반환
    public int GetWhiteCount(PieceType type)
    {
        return white_count[type];
    }

    // 흑의 특정 기물 개수 반환
    public int GetBlackCount(PieceType type)
    {
        return black_count[type];
    }
}
