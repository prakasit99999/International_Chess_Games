using System;
using System.Linq;
using AIEngine.Algorithms;
using AIEngine.Utilities;

namespace AIEngine.Core
{
    public class AICore
    {
        public enum Difficulty { Easy, Medium, Hard }
        public static AICore Instance { get; } = new AICore();


        public MoveModel FindBestMove(ChessBoardModel board, Difficulty difficulty)
        {
            SearchAlgorithm algorithm;
            int depth;

            switch (difficulty)
            {
                case Difficulty.Easy:
                    algorithm = new Minimax(); // ธรรมดา
                    depth = 2;
                    break;
                case Difficulty.Medium:
                    algorithm = new AlphaBeta(); // มี pruning
                    depth = 4;
                    break;
                case Difficulty.Hard:
                    algorithm = new AlphaBeta(); // ลึกและวิเคราะห์เยอะ
                    depth = 5;
                    break;
                default:
                    throw new ArgumentException("Invalid difficulty level");
            }

            MoveModel bestMove = algorithm.FindBestMove(board, depth);
            return OptimizeMoveForDraw(board, bestMove);
        }


        private MoveModel OptimizeMoveForDraw(ChessBoardModel board, MoveModel bestMove)
        {
            // ถ้าใกล้ครบ 50 เดิน ให้พยายามหลีกเลี่ยงการเสมอ
            if (board.FiftyMoveCounter > 90)
            {
                var captureMoves = MoveGenerator.GenerateMoves(board)
                    .Where(m => board.Board[m.ToX, m.ToY] != 0) // มีตัวอยู่ที่เป้าหมาย
                    .ToList();

                if (captureMoves.Count > 0)
                {
                    // เลือกการกินหมากค่ามากที่สุด
                    return captureMoves.OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY])).First();
                }
            }
            return bestMove;
        }
    }
}
