using AIEngine.Evaluation;
using AIEngine.Utilities;

public abstract class SearchAlgorithm
{
    public abstract MoveModel FindBestMove(ChessBoardModel board, int depth, EvaluationSettings settings = null);
    // Overload สำหรับ return metrics
    public virtual SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int depth, EvaluationSettings settings = null)
    {
        var move = FindBestMove(board, depth, settings);
        return new SearchResult
        {
            Move = move,
            Depth = depth,
            NodesEvaluated = 0,
            TimeMs = 0,
            Score = 0
        };
    }
}
