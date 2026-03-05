using AIEngine.Evaluation;
using NUnit.Framework;
using UnityEngine;

namespace AIEngine.Test
{
    [TestFixture]
    public class EvaluationTests
    {
        // Helper: สร้างกระดานเปล่าที่มีแค่ King สองตัว
        private ChessBoardModel CreateSimpleBoard()
        {
            var board = new ChessBoardModel();
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    board.Board[i, j] = 0;

            board.Board[7, 4] = 6;  // White King
            board.Board[0, 4] = -6; // Black King
            return board;
        }

        [Test]
        public void Test_KingVsKing_ReturnsZero()
        {
            var board = CreateSimpleBoard();
            float score = Evaluation.Evaluate(board);
            // ควรได้คะแนนใกล้ศูนย์ (ยกเว้นค่า Tempo เล็กน้อย)
            Assert.IsTrue(Mathf.Abs(score) < 20, "Board with only kings should evaluate near zero.");
        }

        [Test]
        public void Test_Evaluation_RespectsSettings()
        {
            var board = CreateSimpleBoard();
            board.Board[4, 4] = 5; // White Queen

            // 1. ใช้ค่าเริ่มต้น (Queen = 900)
            float defaultScore = Evaluation.Evaluate(board, new EvaluationSettings());

            // 2. ใช้ค่าที่ปรับแต่ง (Queen = 500)
            var customSettings = new EvaluationSettings { QueenValue = 500 };
            float customScore = Evaluation.Evaluate(board, customSettings);

            Assert.AreNotEqual(defaultScore, customScore, "Score should change based on EvaluationSettings.");
            Assert.IsTrue(customScore < defaultScore, "Score with 500-value Queen should be lower than 900-value Queen.");
        }

        [Test]
        public void Test_WhiteHasExtraQueen_ReturnsPositiveScore()
        {
            var board = CreateSimpleBoard();
            board.Board[4, 4] = 5;
            board.IsWhiteTurn = true;
            float score = Evaluation.Evaluate(board);
            Assert.Greater(score, 800, "White having an extra queen should result in a high positive score.");
        }

        [Test]
        public void Test_BlackHasExtraQueen_ReturnsNegativeScore()
        {
            var board = CreateSimpleBoard();
            board.Board[4, 4] = -5;
            board.IsWhiteTurn = true;
            float score = Evaluation.Evaluate(board);
            Assert.Less(score, -800, "Black having an extra queen should result in a high negative score for White.");
        }

        [Test]
        public void Test_Symmetry_ScoreIsNegatedWhenColorsSwapped()
        {
            var board1 = CreateSimpleBoard();
            board1.Board[4, 3] = 5; board1.IsWhiteTurn = true;
            float score1 = Evaluation.Evaluate(board1);

            var board2 = CreateSimpleBoard();
            board2.Board[3, 3] = -5; board2.IsWhiteTurn = false;
            float score2 = Evaluation.Evaluate(board2);

            Assert.AreEqual(score1, score2, 1.0f, "Evaluation should be symmetric for both colors.");
        }

        [Test]
        public void Test_PassedPawn_AddsBonus()
        {
            var board = CreateSimpleBoard();
            board.Board[2, 0] = 1;
            float score = Evaluation.Evaluate(board);
            Assert.Greater(score, 120, "Passed pawn should receive a bonus.");
        }

        [Test]
        public void Test_IsolatedPawn_AppliesPenalty()
        {
            var board1 = CreateSimpleBoard();
            board1.Board[6, 3] = 1; board1.Board[6, 4] = 1;
            float score1 = Evaluation.Evaluate(board1);

            var board2 = CreateSimpleBoard();
            board2.Board[6, 3] = 1; board2.Board[6, 5] = 1;
            float score2 = Evaluation.Evaluate(board2);

            Assert.Greater(score1, score2, "Isolated pawns should be penalized compared to supported pawns.");
        }

        [Test]
        public void Test_FiftyMoveRule_ReturnsDraw()
        {
            var board = new ChessBoardModel();
            board.SetFiftyMoveCounter(100);
            float score = Evaluation.Evaluate(board);
            Assert.AreEqual(0f, score, "Fifty-move rule should result in a draw (0.00 evaluation).");
        }
    }
}
