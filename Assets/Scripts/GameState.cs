using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class GameState
{
    public Board Board { get; }
    public PlayerColor CurrnetPlayer { get; private set; }
    public Result Result { get; private set; } = null;

    private int no_capture_or_pawn_moves = 0;

    public GameState(PlayerColor player, Board board)
    {
        this.CurrnetPlayer = player;
        this.Board = board;
    }

    public IEnumerable<Move> LegalMoveForPiece(Position pos)
    {
        if(Board.IsEmpty(pos) || Board[pos].Color != CurrnetPlayer)
        {
            return Enumerable.Empty<Move>();
        }

        Piece piece = Board[pos];
        IEnumerable<Move> moveCandiates = piece.GetMoves(pos, Board);
        return moveCandiates.Where(move => move.IsLegal(Board));
    }

    public void MakeMove(Move move)
    {
        Board.SetPawnSkipPosition(CurrnetPlayer, null);
        bool capture_or_pawn_move = move.Execute(Board);

        if (capture_or_pawn_move)
        {
            no_capture_or_pawn_moves = 0;   
        }
        else
        {
            no_capture_or_pawn_moves++;
        }

        CurrnetPlayer = CurrnetPlayer.Opponent();
        CheckForGameOver();
    }

    public IEnumerable<Move> AllLegalMovesFor(PlayerColor player)
    {
        IEnumerable<Move> moveCandiates = Board.PiecePositionsFor(player).SelectMany(pos =>
        {
            Piece piece = Board[pos];
            return piece.GetMoves(pos, Board);
        });

        return moveCandiates.Where(move => move.IsLegal(Board));
    }

    private void CheckForGameOver()
    {
        if (!AllLegalMovesFor(CurrnetPlayer).Any())
        {
            if (Board.IsInCheck(CurrnetPlayer))
            {
                Result = Result.Win(CurrnetPlayer.Opponent());
                UIManager.Instance.resultUI.SetUI(CurrnetPlayer.Opponent(), Result.EndReason);
            }
            else
            {
                Result = Result.Draw(EndReason.Stalemate);
                UIManager.Instance.resultUI.SetUI(PlayerColor.None, Result.EndReason);
            }
            UIManager.Instance.ChangeState(UIState.Result);
        }
        else if (Board.InsufficientMaterial())
        {
            Result = Result.Draw(EndReason.InsufficientMaterial);
            UIManager.Instance.resultUI.SetUI(PlayerColor.None, Result.EndReason);
        }
        else if (FiftyMoveRules())
        {
            Result = Result.Draw(EndReason.FiftyMoveRule);
            UIManager.Instance.resultUI.SetUI(PlayerColor.None, Result.EndReason);
        }
    }

    public bool IsGameOver()
    {
        return Result != null;
    }
    
    private bool FiftyMoveRules()
    {
        int full_moves = no_capture_or_pawn_moves / 2;
        return full_moves == 50;
    }
}
