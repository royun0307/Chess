using System.Text;
using UnityEditor.Build;
using UnityEngine.Rendering;

public class StateString
{
    private readonly StringBuilder sb = new StringBuilder();

    public StateString(PlayerColor current_player, Board board)
    {
        AddPiecePlacement(board);
        sb.Append(' ');
        AddCurrentPlayer(current_player);
        sb.Append(' ');
        AddCastlingRights(board);
        sb.Append(' ');
        AddEnPassant(board, current_player);
    }

    public override string ToString()
    {
        return sb.ToString();
    }


    private static char PieceChar(Piece piece)
    {
        char c = piece.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => ' '
        };

        if (piece.Color == PlayerColor.White)
        {
            return char.ToUpper(c);
        }

        return c;
    }

    private void AddRowData(Board board, int row)
    {
        int empty = 0;

        for (int c = 0; c < 8; c++)
        {
            if (board[row, c] == null)
            {
                empty++;
                continue;
            }

            if(empty > 0)
            {
                sb.Append(empty);
                empty = 0;
            }

            sb.Append(PieceChar(board[row, c]));
        }

        if (empty > 0)
        {
            sb.Append(empty);
        }
    }

    private void AddPiecePlacement(Board board)
    {
        for (int r = 0; r < 8; r++)
        {
            if (r != 0)
            {
                sb.Append('/');
            }
            AddRowData(board, r);
        }
    }

    private void AddCurrentPlayer(PlayerColor current_player)
    {
        if(current_player == PlayerColor.White)
        {
            sb.Append('w');
        }
        else
        {
            sb.Append('b');
        }
    }

    private void AddCastlingRights(Board board)
    {
        bool castleWKS = board.CastleRightKS(PlayerColor.White);
        bool castleWQS = board.CastleRightQS(PlayerColor.White);
        bool castleBKS = board.CastleRightKS(PlayerColor.Black);
        bool castleBQS = board.CastleRightQS(PlayerColor.Black);

        if (!(castleWKS || castleWQS || castleBKS || castleBQS))
        {
            sb.Append('-');
            return;
        }

        if (castleWKS)
        {
            sb.Append('K');
        }
        if (castleWQS)
        {
            sb.Append('Q');
        }
        if (castleBKS)
        {
            sb.Append('k');
        }
        if (castleBQS)
        {
            sb.Append('q');
        }
    }

    private void AddEnPassant(Board board, PlayerColor current_player)
    {
        if (!board.CanCaptureEnPassant(current_player))
        {
            sb.Append('-');
            return;
        }

        Position pos = board.GetPawnSkipPosition(current_player.Opponent());
        char file = (char)('a' + pos.column);
        int rank = 8 - pos.row;
        sb.Append(file);
        sb.Append(rank);
    }
}
