using System.Collections.Generic;
using System.Linq;

public class GameState
{
    public Board Board { get; }
    public PlayerColor CurrnetPlayer { get; private set; }
    
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
        return piece.GetMoves(pos, Board);
    }

    public void MakeMove(Move move)
    {
        move.Execute(Board);
        CurrnetPlayer = CurrnetPlayer.Opponent();
    }
}
