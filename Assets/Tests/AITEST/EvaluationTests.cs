using UnityEngine;
using NUnit.Framework;
namespace AIEngine.Evaluation
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
        //1 ทดสอบ การประเมินค่าของกระดานที่มีเพียงคิงทั้งสองฝ่าย โดยคาดว่าคะแนนจะเป็นศูนย์เนื่องจากไม่มีฝ่ายใดได้เปรียบ
        [Test]
        public void Test_KingVsKing_ReturnsZero()
        {
            var board = CreateSimpleBoard();
            board.IsWhiteTurn = false; // ปิด tempo
            float score = Evaluation.Evaluate(board);
            Assert.AreEqual(0, score, 0.01f);
        }
        //

        [Test]
        public void Test_WhiteHasExtraQueen_ReturnsPositiveScore()
        {
            var board = CreateSimpleBoard();
            board.IsWhiteTurn = false;
            board.Board[6, 3] = 5;
            float score = Evaluation.Evaluate(board);
            Assert.Greater(score, 0);

        }

        [Test]
        public void Test_BlackHasExtraQueen_ReturnsNegativeScore()
        {
            var board = CreateSimpleBoard();
            board.IsWhiteTurn = false;
            board.Board[1, 3] = -5; // วางควีนดำที่ d7
            float score = Evaluation.Evaluate(board);
            Assert.Less(score, 0);


        }

        [Test]
        public void Test_Symmetry_ScoreIsNegatedWhenColorsSwapped()
        {
            var board1 = CreateSimpleBoard();
            board1.IsWhiteTurn = false;
            board1.Board[6, 3] = 5; // white queen
            float score1 = Evaluation.Evaluate(board1);

            var board2 = CreateSimpleBoard();
            board2.IsWhiteTurn = false;
            board2.Board[6, 3] = -5; // black queen
            float score2 = Evaluation.Evaluate(board2);

            Assert.AreEqual(score1, -score2, 0.01f);
        }

        [Test]
        public void Test_PassedPawn_AddsBonus()
        {
            var board = CreateSimpleBoard();
            board.IsWhiteTurn = false;
            board.Board[6, 3] = 1;
            float score = Evaluation.Evaluate(board);
            Assert.Greater(score, 0);
        }

        [Test]
        public void Test_IsolatedPawn_AppliesPenalty()
        {
            var b1 = CreateSimpleBoard();
            b1.IsWhiteTurn = false;
            b1.Board[6, 3] = 1;
            b1.Board[6, 4] = 1; // connected pawns

            var b2 = CreateSimpleBoard();
            b2.IsWhiteTurn = false;
            b2.Board[6, 3] = 1;
            b2.Board[6, 5] = 1; // isolated pawns

            float score1 = Evaluation.Evaluate(b1);
            float score2 = Evaluation.Evaluate(b2);

            Assert.Greater(score1, score2);
        }

        [Test]
        public void Test_FiftyMoveRule_ReturnsDraw()
        {
            var board = CreateSimpleBoard();
            board.SetFiftyMoveCounter(100);
            Assert.AreEqual(0f, Evaluation.Evaluate(board));
        }
    }
}
