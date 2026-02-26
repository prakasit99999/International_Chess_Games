using System.Collections;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace AIEngine.Test
{
    public class EvaluationTests
    {
        private ChessBoardModel CreateSimpleBoard()
        {
            var board = new ChessBoardModel();
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    board.Board[i, j] = 0;

            board.Board[7, 4] = 6;  // คิงขาว
            board.Board[0, 4] = -6; // คิงดำ
            return board;
        }
        //1
        [Test]
        public void Test_EmptyBoard_ReturnsZero()
        {
            var board = CreateEmptyBoard();
            float score = Evaluation.Evaluate(board);
            Assert.AreEqual(0f, score, 0.001f);
        }
        //2
        [Test]
        public void Test_IsolatedPawn_AppliesPenalty()
        {

        }
        //3
        [Test]
        public void Test_PassedPawn_AppliesBonus()
        {

        }
        //4
        [Test]
        public void Test_PassedPawn_GivesBonus()
        {

        }
        //5
        [Test]
        public void Test_BlackHasExtraQueen_ReturnsNegative()
        {

        }
        //6
        [Test]
        public void Evaluate_WhiteHasExtraQueen_ReturnsPositiveScore()
        {

        }

        //7
        [Test]
        public void Test_WhiteHasExtraQueen_ReturnsPositive()
        {

        }
    }
}
