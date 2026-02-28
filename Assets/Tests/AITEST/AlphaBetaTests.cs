using AIEngine.Evaluation;
using AIEngine.Algorithms;
using AIEngine.Core;
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

    [Test]
    public void Test_HardMode_UsesOpeningBook_AtStart()
    {
        var board = new ChessBoardModel();
        var aiCore = new AICore();

        // ในระดับ Hard ตาแรกควรใช้เวลา 0ms เพราะมาจาก Opening Book
        var result = aiCore.FindBestMoveWithMetrics(board, AICore.Difficulty.Hard);

        Assert.IsNotNull(result.Move, "Should find a move from opening book.");
        Assert.AreEqual(0, result.NodesEvaluated, "Opening book move should not evaluate any nodes.");
        Assert.AreEqual(0, result.Depth, "Opening book move should have depth 0.");
    }
}
