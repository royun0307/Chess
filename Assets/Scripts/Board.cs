using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class Board
{
    private readonly Piece[,] pieces = new Piece[8, 8];
    
    private readonly Dictionary<PlayerColor, Position> pawn_skip_postions = new Dictionary<PlayerColor, Position>
    {
        {PlayerColor.White, null },
        {PlayerColor.Black, null }
    };

    public Piece this[int row, int col]
    {
        get { return pieces[row, col]; }
        set { pieces[row, col] = value; }
    }

    public Piece this[Position pos]
    {
        get { return pieces[pos.row, pos.column]; }
        set { pieces[pos.row, pos.column] = value; }
    }

    public Position GetPawnSkipPosition(PlayerColor player)
    {
        return pawn_skip_postions[player];
    }

    public void SetPawnSkipPosition(PlayerColor player, Position pos)
    {
        pawn_skip_postions[player] = pos;
    }

    public static Board Initial()
    {
        Board board = new Board();
        board.AddStartPieces();
        return board;
    }

    private void AddStartPieces()
    {
        //BlackMajorPiece Position
        this[0, 0] = new Rook(PlayerColor.Black);
        this[0, 1] = new Knight(PlayerColor.Black);
        this[0, 2] = new Bishop(PlayerColor.Black);
        this[0, 3] = new Queen(PlayerColor.Black);
        this[0, 4] = new King(PlayerColor.Black);
        this[0, 5] = new Bishop(PlayerColor.Black);
        this[0, 6] = new Knight(PlayerColor.Black);
        this[0, 7] = new Rook(PlayerColor.Black);
        
        //WhiteMajorPiece Position
        this[7, 0] = new Rook(PlayerColor.White);
        this[7, 1] = new Knight(PlayerColor.White);
        this[7, 2] = new Bishop(PlayerColor.White);
        this[7, 3] = new Queen(PlayerColor.White);
        this[7, 4] = new King(PlayerColor.White);
        this[7, 5] = new Bishop(PlayerColor.White);
        this[7, 6] = new Knight(PlayerColor.White);
        this[7, 7] = new Rook(PlayerColor.White);

        //MinorPiece Position
        for (int i = 0; i < 8; i++)
        {
            this[1, i] = new Pawn(PlayerColor.Black);
            this[6, i] = new Pawn(PlayerColor.White);
        }
    }

    public static bool IsInside(Position pos)
    {
        return pos.row >= 0 && pos.row < 8 && pos.column >= 0 && pos.column < 8;
    }

    public bool IsEmpty(Position pos)
    {
        return this[pos] == null;
    }

    public IEnumerable<Position> PiecePositions()
    {
        for (int r = 0; r < 8; r++)
        {
            for(int c = 0; c < 8; c++)
            {
                Position pos = new Position(r, c);

                if (!IsEmpty(pos))
                {
                    yield return pos;
                }
            }
        }
    }

    public IEnumerable<Position> PiecePositionsFor(PlayerColor player)
    {
        return PiecePositions().Where(pos => this[pos].Color == player);
    }

    public bool IsInCheck(PlayerColor player)
    {
        return PiecePositionsFor(player.Opponent()).Any(pos =>
        {
            Piece piece = this[pos];
            return piece.CanCaptureOpponentKing(pos, this);
        });
    }

    public Board Copy()
    {
        Board copy = new Board();

        foreach(Position pos in PiecePositions())
        {
            copy[pos] = this[pos].Copy();
        }

        copy.SetPawnSkipPosition(PlayerColor.White, pawn_skip_postions[PlayerColor.White]);
        copy.SetPawnSkipPosition(PlayerColor.Black, pawn_skip_postions[PlayerColor.Black]);

        return copy;
    }

    public Counting CountPieces()
    {
        Counting counting = new Counting();

        foreach(Position pos in PiecePositions())
        {
            Piece piece = this[pos];
            counting.Increment(piece.Color, piece.Type);
        }

        return counting;
    }

    public bool InsufficientMaterial()
    {
        Counting counting = CountPieces();

        return IsKingVKing(counting) || IsKingBishopVKing(counting) ||
            IsKingKnightVKing(counting) || IsKingBishopVKingBishop(counting);
    }

    private bool IsKingVKing(Counting counting)
    {
        return counting.TotalCount == 2;
    }

    private bool IsKingBishopVKing(Counting counting)
    {
        return counting.TotalCount == 3 && (counting.GetWhiteCount(PieceType.Bishop) == 1 || counting.GetBlackCount(PieceType.Bishop) == 1);
    }

    private bool IsKingKnightVKing(Counting counting)
    {
        return counting.TotalCount == 3 && (counting.GetWhiteCount(PieceType.Knight) == 1 || counting.GetBlackCount(PieceType.Knight) == 1);
    }

    private bool IsKingBishopVKingBishop(Counting counting)
    {
        if(counting.TotalCount != 4)
        {
            return false;
        }

        if(counting.GetWhiteCount(PieceType.Bishop) != 1 || counting.GetBlackCount(PieceType.Bishop) != 1)
        {
            return false;
        }

        Position w_bishop_pos = FindPiece(PlayerColor.White, PieceType.Bishop);
        Position b_bishop_pos = FindPiece(PlayerColor.Black, PieceType.Bishop);

        return w_bishop_pos.SquareColor() == b_bishop_pos.SquareColor();
    }

    private Position FindPiece(PlayerColor color, PieceType type)
    {
        return PiecePositionsFor(color).First(pos => this[pos].Type == type);
    }
}
