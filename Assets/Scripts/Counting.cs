using System;
using System.Collections.Generic;

public class Counting
{
    private readonly Dictionary<PieceType, int> white_count = new();
    private readonly Dictionary<PieceType, int> black_count = new();

    public int TotalCount { get; private set; }

    public Counting()
    {
        foreach (PieceType type in Enum.GetValues(typeof(PieceType)))
        {
            white_count[type] = 0;
            black_count[type] = 0;
        }
    }

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

        TotalCount++;
    }

    public int GetWhiteCount(PieceType type)
    {
        return white_count[type];
    }
    
    public int GetBlackCount(PieceType type)
    {
        return black_count[type];
    }
}
