using NUnit.Framework;
using AIEngine.Algorithms;
using AIEngine.Evaluation;

namespace AIEngine.Tests
{
    public class AlphaBetaTests
    {
        // Helper: สร้างกระดานเปล่าเพื่อทดสอบเฉพาะจุด
        private ChessBoardModel CreateEmptyBoard()
        {
            var board = new ChessBoardModel();
            System.Array.Clear(board.Board, 0, board.Board.Length);
            return board;
        }

        // 1. ทดสอบพื้นฐาน: AI ต้องหาท่า Checkmate ได้ (Mate in 1)
        [Test]
        public void Test_FindsMateInOne()
        {
            var board = CreateEmptyBoard();
            board.Board[7, 4] = 6;  // White King
            board.Board[0, 7] = -6; // Black King
            board.Board[7, 0] = 4;  // White Rook (a1)
            
            board.IsWhiteTurn = true;
            var ai = new AlphaBeta();
            
            // Depth 2 พอสำหรับ Mate in 1
            var move = ai.FindBestMove(board, 2);

            Assert.IsNotNull(move);
            Assert.AreEqual(0, move.ToX); // ต้องเดินไปแถว 0 (a8) เพื่อรุกฆาต
            Assert.AreEqual(0, move.ToY);
        }

        // 2. ทดสอบพื้นฐาน: AI ต้องเลือกกินตัวฟรี (Material Advantage)
        [Test]
        public void Test_TakesHangingPiece()
        {
            var board = CreateEmptyBoard();
            board.Board[7, 4] = 6; board.Board[0, 4] = -6; // Kings
            board.Board[4, 4] = 5;  // White Queen (e4)
            board.Board[4, 5] = -4; // Black Rook (f4) - ยืนให้กินฟรี

            board.IsWhiteTurn = true;
            var ai = new AlphaBeta();
            var move = ai.FindBestMove(board, 2);

            Assert.AreEqual(4, move.ToX);
            Assert.AreEqual(5, move.ToY); // ต้องกินที่ f4
        }

        
    }
}