using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using AIEngine.Algorithms;
using AIEngine.Utilities;

namespace AIEngine.Tests
{
    public class AlphaBetaTests
    {
        private AlphaBeta _alphaBeta;

        [SetUp]
        public void SetUp()
        {
            _alphaBeta = new AlphaBeta(timeLimitMs: 1000);
        }
        // 1. ทดสอบว่า AlphaBeta สามารถคืนค่าการเคลื่อนไหวที่ถูกต้องจากตำแหน่งเริ่มต้นได้หรือไม่
        [Test]
        public void AlphaBeta_Returns_Legal_Move_From_Starting_Position()
        {
            var board = new ChessBoardModel();
            var legalMoves = MoveGenerator.GenerateMoves(board);

            var result = _alphaBeta.FindBestMoveWithMetrics(board, 2);

            Assert.IsNotNull(result.Move, "Expected a move from starting position.");
            Assert.IsTrue(legalMoves.Any(m => MoveOrderer.IsSameMove(m, result.Move)),
                "Returned move must be legal.");
        }
        // 2. ทดสอบว่า AlphaBeta รายงานจำนวนโหนดที่ประเมินได้อย่างถูกต้อง
        [Test]
        public void AlphaBeta_Reports_Nodes_Evaluated()
        {
            var board = new ChessBoardModel();

            var result = _alphaBeta.FindBestMoveWithMetrics(board, 1);

            Assert.Greater(result.NodesEvaluated, 0, "NodesEvaluated should be > 0.");
        }
        // 3. เพิ่มการทดสอบสำหรับการเคลียร์กระดานและตรวจสอบว่า AlphaBeta สามารถจัดการกับสถานการณ์ที่ไม่มีชิ้นส่วนได้อย่างถูกต้อง

        [Test]
        public void AlphaBeta_Reports_Depth_Within_Max()
        {
            var board = new ChessBoardModel();

            var result = _alphaBeta.FindBestMoveWithMetrics(board, 1);

            Assert.AreEqual(1, result.Depth, "Depth should reach maxDepth when time allows.");
        }
      //  4. เพิ่มการทดสอบสำหรับการเคลียร์กระดานและตรวจสอบว่า AlphaBeta สามารถจัดการกับสถานการณ์ที่ไม่มีชิ้นส่วนได้อย่างถูกต้อง

        [Test]
        public void AlphaBeta_Respects_Time_Limit()
        {
            var board = new ChessBoardModel();
            var ai = new AlphaBeta(timeLimitMs: 50);

            var stopwatch = Stopwatch.StartNew();
            ai.FindBestMoveWithMetrics(board, 6);
            stopwatch.Stop();

            Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 1000,
                "Search should stop within a reasonable buffer of the time limit.");
        }
        //5. เพิ่มการทดสอบสำหรับกรณีที่ไม่มีชิ้นส่วนบนกระดาน
        [Test]
        public void AlphaBeta_Returns_Null_When_No_Pieces()
        {
            var board = new ChessBoardModel();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    board.Board[i, j] = 0;
                }
            }

            var result = _alphaBeta.FindBestMoveWithMetrics(board, 2);

            Assert.IsNull(result.Move, "Expected null when no legal moves exist.");
        }
    }
}
