using UnityEngine;

public interface IChessEngine
{
    Move GetBestMove(Board board, PlayerColor side_to_move, int depth);
}
