using System;
using System.Diagnostics;
using System.Linq;
using AIEngine.Algorithms;
using AIEngine.Utilities;

namespace AIEngine.Core
{
    public class AICore
    {
        public enum Difficulty { Easy, Normal, Hard }
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
                case Difficulty.Normal:
                    algorithm = new AlphaBeta(); // มี pruning
                    depth = 4;
                    break;
                case Difficulty.Hard:
                    algorithm = new AlphaBeta(); // ลึกและวิเคราะห์เยอะ
                    depth = 6;
                    break;
                default:
                    throw new ArgumentException("Invalid difficulty level");
            }

            MoveModel bestMove = algorithm.FindBestMove(board, depth);
            return OptimizeMoveForDraw(board, bestMove);
        }


        private MoveModel OptimizeMoveForDraw(ChessBoardModel board, MoveModel bestMove)
        {
            if (board.FiftyMoveCounter > 90)
            {
                var captureMoves = MoveGenerator.GenerateMoves(board)
                    .Where(m => board.Board[m.ToX, m.ToY] != 0)
                    .ToList();

                if (captureMoves.Count > 0)
                {
                    return captureMoves.OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY])).First();
                }

                // 🔹 ถ้าไม่มีการกิน ลองเดินเบี้ยแทน
                var pawnMoves = MoveGenerator.GenerateMoves(board)
                    .Where(m => Math.Abs(board.Board[m.FromX, m.FromY]) == 1)
                    .ToList();

                if (pawnMoves.Count > 0)
                {
                    return pawnMoves.First(); // หรือเลือก pawn push ที่ดีที่สุด
                }
            }
            return bestMove;
        }



    }
}
