using AIEngine.Utilities;

public abstract class SearchAlgorithm
{
    public abstract MoveModel FindBestMove(ChessBoardModel board, int depth);
    
    // Overload สำหรับ return metrics
    public virtual SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int depth)
    {
        var move = FindBestMove(board, depth);
        return new SearchResult
        {
            Move = move,
            Depth = depth,
            NodesEvaluated = 0,
            TimeMs = 0
        };
    }
}
