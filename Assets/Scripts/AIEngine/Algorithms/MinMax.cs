using System;
using AIEngine.Utilities;
using System.Diagnostics;
using AIEngine.Evaluation;

using System.Linq;

namespace AIEngine.Algorithms
{
    public class Minimax : SearchAlgorithm
    {
        private int _nodesEvaluated = 0;

        public override MoveModel FindBestMove(ChessBoardModel board, int depth)
        {
            var result = FindBestMoveWithMetrics(board, depth);
            return result.Move;
        }

        public override SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int depth)
        {
            _nodesEvaluated = 0;
            var stopwatch = Stopwatch.StartNew();
            var moves = MoveGenerator.GenerateMoves(board);
            if (moves == null || moves.Count == 0)
                throw new InvalidOperationException("No valid moves found.");

            MoveModel bestMove = null;
            int bestScore = int.MinValue;

            foreach (MoveModel move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int score = MinimaxRecursive(newBoard, depth - 1, false);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            stopwatch.Stop();
            var elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            var finalMove = bestMove ?? moves.First();

            return new SearchResult
            {
                Move = finalMove,
                Depth = depth,
                NodesEvaluated = _nodesEvaluated,
                TimeMs = elapsedMs
            };
        }

        protected int MinimaxRecursive(ChessBoardModel board, int depth, bool isMaximizing)
        {
            _nodesEvaluated++; // นับ node ที่ evaluate
            
            if (depth == 0 || board.IsGameOver())
                return AIEngine.Evaluation.Evaluation.Evaluate(board);

            if (board.RepetitionCount >= 3)
                return 0;
            var moves = MoveGenerator.GenerateMoves(board);
            int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int score = MinimaxRecursive(newBoard, depth - 1, !isMaximizing);
                bestScore = isMaximizing
                    ? Math.Max(bestScore, score)
                    : Math.Min(bestScore, score);
            }

            return bestScore;
        }
    }
}
