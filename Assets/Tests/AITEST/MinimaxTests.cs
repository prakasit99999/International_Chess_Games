using AIEngine.Evaluation;
using AIEngine.Algorithms;
using NUnit.Framework;

[TestFixture]
public class MinimaxTests
{
    [Test]
    public void TC_AI02_Minimax_ReturnsValidMove()
    {
        var board = new ChessBoardModel();
        var minimax = new Minimax(timeLimitMs: 2000);

        var result = minimax.FindBestMoveWithMetrics(board, depth: 2);

        Assert.IsNotNull(result.Move, "Minimax should return a move.");
        Assert.DoesNotThrow(() => {
            var clone = board.Clone();
            clone.MakeMove(result.Move);
        }, "The move returned by Minimax should be valid.");
    }
}
