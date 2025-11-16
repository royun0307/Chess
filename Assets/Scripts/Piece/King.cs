using System.Collections.Generic;
using System.Linq;

public class King : Piece
{
    public override PieceType Type => PieceType.King;
    public override PlayerColor Color { get; }

    private static readonly Direction[] dirs = new Direction[]
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
        Direction.NorthEast,
        Direction.NorthWest,
        Direction.SouthEast,
        Direction.SouthWest,
    };

    public King(PlayerColor color)
    {
        this.Color = color;
    }

    private static bool IsUnmovedRook(Position pos, Board board)
    {
        if (board.IsEmpty(pos))
        {
            return false;
        }

        Piece piece = board[pos];
        return piece.Type == PieceType.Rook && !piece.hasMoved;
    }

    private static bool AllEmpty(IEnumerable<Position> positions, Board board)
    {
        return positions.All(pos => board.IsEmpty(pos));
    }

    private bool CanCastleKingSide(Position from, Board board)
    {
        if(hasMoved)
        {
            return false;
        }

        Position rook_pos = new Position(from.row, 7);
        Position[] between_positions = new Position[] { new(from.row, 5), new(from.row, 6) };

        return IsUnmovedRook(rook_pos, board) && AllEmpty(between_positions, board);
    }

    private bool CanCastleQueenSide(Position from, Board board)
    {
        if (hasMoved)
        {
            return false;
        }

        Position rook_pos = new Position(from.row, 0);
        Position[] between_positions = new Position[] { new(from.row, 1), new(from.row, 2), new(from.row, 3) };

        return IsUnmovedRook(rook_pos, board) && AllEmpty(between_positions, board);
    }

    public override Piece Copy()
    {
        King copy = new King(Color);
        copy.hasMoved = hasMoved;
        return copy;
    }

    private IEnumerable<Position> MovePositions(Position from, Board board)
    {
        foreach (Direction dir in dirs)
        { 
            Position to = from + dir;

            if (!Board.IsInside(to))
            {
                continue;
            }

            if(board.IsEmpty(to) || board[to].Color != Color)
            {
                yield return to;
            }
        }
    }

    public override IEnumerable<Move> GetMoves(Position from, Board board)
    {
        foreach (Position to in MovePositions(from, board))
        {
            yield return new NormalMove(from, to);
        }

        if(CanCastleKingSide(from, board))
        {
            yield return new Castle(MoveType.CastleKS, from);
        }

        if(CanCastleQueenSide(from, board))
        {
            yield return new Castle(MoveType.CastleQS, from);
        }
    }

    public override bool CanCaptureOpponentKing(Position from, Board board)
    {
        return MovePositions(from, board).Any(to =>
        {
            Piece piece = board[to];
            return piece != null && piece.Type == PieceType.King;
        });
    }
}
