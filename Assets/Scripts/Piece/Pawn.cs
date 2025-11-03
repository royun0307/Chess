using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Pawn : Piece
{
    public override PieceType Type => PieceType.Pawn;
    public override PlayerColor Color { get; }

    private readonly Direction forward;

    public Pawn(PlayerColor color)
    {
        this.Color = color;

        if (color == PlayerColor.White)
        { 
            forward = Direction.North;
        }
        else if (color == PlayerColor.Black) 
        {
            forward = Direction.South;
        }
    }

    public override Piece Copy()
    {
        Pawn copy = new Pawn(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }

    private static bool CanMoveTo(Position pos, Board board)
    { 
        return Board.IsInside(pos) && board.IsEmpty(pos);
    }

    private bool CanCaptureAt(Position pos, Board board)
    { 
        if(!Board.IsInside(pos) || board.IsEmpty(pos))
        {
            return false;
        }

        return board[pos].Color != Color;
    }

    private IEnumerable<Move> ForwardMoves(Position from, Board board)
    {
        Position one_move_pos = from + forward;

        if(CanMoveTo(one_move_pos, board))
        {
            yield return new NormalMove(from, one_move_pos);

            Position two_move_pos = one_move_pos + forward;

            if(!hasMoved && CanMoveTo(two_move_pos, board))
            {
                yield return new NormalMove(from, two_move_pos);
            }
        }
    }

    private IEnumerable<Move> DiagonalMoves(Position from, Board board) 
    {
        foreach(Direction dir in new Direction[] { Direction.West, Direction.East })
        {
            Position to = from + forward + dir;

            if(CanCaptureAt(to, board))
            {
                yield return new NormalMove(from, to);
            }
        }
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        return ForwardMoves(from, board).Concat(DiagonalMoves(from, board));
    }

    public override bool CanCaptureOpponentKing(Position from, Board board)
    {
        return DiagonalMoves(from, board).Any(move =>
        {
            Piece piece = board[move.ToPos];
            return piece != null && piece.Type == PieceType.King;
        });
    }
}