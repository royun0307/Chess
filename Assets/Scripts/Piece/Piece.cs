using UnityEngine;


public abstract class Piece
{
    public abstract PieceType Type { get; }
    public abstract PlayerColor Color { get; }
    public bool hasMoved { get; set; } = false;

    public abstract Piece Copy();
}

