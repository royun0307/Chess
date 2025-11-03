
public enum EndReason
{
    Checkmate,
    Stalemate,
    FiftyMoveRule,
    InsufficientMaterial,
    ThreefoldRepetition
};

public class Result
{

    public PlayerColor Winner { get; }
    public EndReason EndReason { get; }

    public Result(PlayerColor winner, EndReason endReason)
    {
        this.Winner = winner;
        this.EndReason = endReason;
    }

    public static Result Win(PlayerColor winner)
    {
        return new Result(winner, EndReason.Checkmate);
    }

    public static Result Draw(EndReason reason)
    {
        return new Result(PlayerColor.None, reason);
    }
}