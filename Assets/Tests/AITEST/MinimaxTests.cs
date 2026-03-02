using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using AIEngine.Algorithms;
using AIEngine.Utilities;
namespace AIEngine.Tests
{
    public class MinimaxTests
    {
        private Minimax _minimax;
        [SetUp]
        public void SetUp()
        {
            _minimax = new Minimax(timeLimitMs: 1000);
        }
        [Test]
        public void Minimax_Returns_Legal_Move_From_Starting_Position()
        {
            var board = new ChessBoardModel();
            var legalMoves = MoveGenerator.GenerateMoves(board);

            var result = _minimax.FindBestMoveWithMetrics(board, 2);

            Assert.IsNotNull(result.Move, "Expected a move from starting position.");
            Assert.IsTrue(legalMoves.Any(m => MoveOrderer.IsSameMove(m, result.Move)),
                "Returned move must be legal.");
        }

        [Test]
        public void Minimax_Evaluates_Nodes()
        {
            var board = new ChessBoardModel();
            var result = _minimax.FindBestMoveWithMetrics(board, 2);

            Assert.Greater(result.NodesEvaluated, 0,
                "Minimax should evaluate at least one node.");
        }

        [Test]
        public void Minimax_Returns_Correct_Depth()
        {
            var board = new ChessBoardModel();
            var result = _minimax.FindBestMoveWithMetrics(board, 3);

            Assert.AreEqual(3, result.Depth,
                "Returned depth should match requested depth.");
        }
    }
}

