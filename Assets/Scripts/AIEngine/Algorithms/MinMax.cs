using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AIEngine.Evaluation;
using AIEngine.Utilities;

namespace AIEngine.Algorithms
{
    public class Minimax : SearchAlgorithm
    {
        private int _nodesEvaluated = 0;


        public override MoveModel FindBestMove(ChessBoardModel board, int depth, EvaluationSettings settings = null)
        {
            var result = FindBestMoveWithMetrics(board, depth, settings);
            return result.Move;
        }

        public override SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int depth, EvaluationSettings settings = null)
        {
            _nodesEvaluated = 0;
            var stopwatch = Stopwatch.StartNew();
            var moves = MoveGenerator.GenerateMoves(board);
            if (moves == null || moves.Count == 0)
                throw new InvalidOperationException("No valid moves found.");

            List<MoveModel> bestMoves = new List<MoveModel>();
            float bestScore = float.MinValue;

            foreach (MoveModel move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                float score = MinimaxRecursive(newBoard, depth - 1, false, settings);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (Math.Abs(score - bestScore) < 0.001f) // ใช้ epsilon สำหรับ float comparison
                {
                    bestMoves.Add(move);
                }
            }

            stopwatch.Stop();
            var elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            var finalMove = bestMoves.Count > 0 ? bestMoves[new Random().Next(bestMoves.Count)] : moves.First();

            return new SearchResult
            {
                Move = finalMove,
                Depth = depth,
                NodesEvaluated = _nodesEvaluated,
                TimeMs = elapsedMs,
                Score = bestScore
            };
        }

        protected float MinimaxRecursive(ChessBoardModel board, int depth, bool isMaximizing, EvaluationSettings settings)
        {
            _nodesEvaluated++; // นับ node ที่ evaluate

            if (depth == 0 || board.IsGameOver())
                return AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            if (board.RepetitionCount >= 3)
                return 0f;
            var moves = MoveGenerator.GenerateMoves(board);
            float bestScore = isMaximizing ? float.MinValue : float.MaxValue;

            foreach (var move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                float score = MinimaxRecursive(newBoard, depth - 1, !isMaximizing, settings);
                bestScore = isMaximizing
                    ? Math.Max(bestScore, score)
                    : Math.Min(bestScore, score);
            }

            return bestScore;
        }
    }
}
