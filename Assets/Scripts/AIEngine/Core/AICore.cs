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
            var result = FindBestMoveWithMetrics(board, difficulty);
            return result.Move;
        }

        public SearchResult FindBestMoveWithMetrics(ChessBoardModel board, Difficulty difficulty)
        {
            // 🔹 1. Check Opening Book (Only for Hard difficulty in early game)
            if (difficulty == Difficulty.Hard)
            {
                var bookMove = OpeningBook.GetMove(board);
                if (bookMove != null)
                {
                    UnityEngine.Debug.Log("[AICore] Opening book move found: " + bookMove);
                    return new SearchResult
                    {
                        Move = bookMove,
                        Depth = 0, // No search needed
                        NodesEvaluated = 0,
                        TimeMs = 0,
                        Score = 0
                    };
                }
            }

            SearchAlgorithm algorithm;
            int depth;
            switch (difficulty)
            {
                case Difficulty.Easy:
                    algorithm = new Minimax(1000); // 1 sec limit
                    depth = 2;
                    break;
                case Difficulty.Normal:
                    algorithm = new AlphaBeta(3000); // 3 sec limit
                    depth = 4;
                    break;
                case Difficulty.Hard:
                    algorithm = new AlphaBeta(10000); // 10 sec limit
                    depth = 6;
                    break;
                default:
                    throw new ArgumentException("Invalid difficulty level");
            }

            var result = algorithm.FindBestMoveWithMetrics(board, depth);
            var optimizedMove = OptimizeMoveForDraw(board, result.Move);

            return new SearchResult
            {
                Move = optimizedMove,
                Depth = result.Depth,
                NodesEvaluated = result.NodesEvaluated,
                TimeMs = result.TimeMs,
                Score = result.Score
            };
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
