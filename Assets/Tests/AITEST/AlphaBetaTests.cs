using AIEngine.Evaluation;
using AIEngine.Algorithms;
using NUnit.Framework;

[TestFixture]
public class AlphaBetaTests
{
    [Test]
    public void TC_AI03_AlphaBeta_Vs_Minimax_Efficiency()
    {
        var board = new ChessBoardModel();
        var minimax = new Minimax(timeLimitMs: 5000);
        var alphaBeta = new AlphaBeta(timeLimitMs: 5000);
        int depth = 3;

        var mmResult = minimax.FindBestMoveWithMetrics(board, depth);
        var abResult = alphaBeta.FindBestMoveWithMetrics(board, depth);

        Assert.AreEqual(mmResult.Score, abResult.Score, 0.1f, "AlphaBeta should return the same score as Minimax.");
        Assert.LessOrEqual(abResult.NodesEvaluated, mmResult.NodesEvaluated, "AlphaBeta should be more efficient.");
    }
}
