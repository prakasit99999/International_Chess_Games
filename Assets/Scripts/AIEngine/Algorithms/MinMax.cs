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
        private const int NoTimeLimitMs = 0;
        private const int DefaultDepth = 1000;
        private int _nodesEvaluated = 0;
        private readonly int _timeLimitMs;
        private int _actualDepth = 0;

        public Minimax(int timeLimitMs = NoTimeLimitMs)
        {
            // 0 means search without a time limit.
            _timeLimitMs = timeLimitMs;
        }

        private bool HasTimeLimit => _timeLimitMs > NoTimeLimitMs;


        public override MoveModel FindBestMove(ChessBoardModel board, int depth, EvaluationSettings settings = null)
        {
            var result = FindBestMoveWithMetrics(board, depth, settings);
            return result.Move;
        }

        public override SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int depth, EvaluationSettings settings = null)
        {
            _nodesEvaluated = 0;
            _actualDepth = 0;

            var stopwatch = Stopwatch.StartNew();
            var rootMoves = MoveGenerator.GenerateMoves(board);
            if (rootMoves == null || rootMoves.Count == 0)
            {
                stopwatch.Stop();
                return new SearchResult
                {
                    Move = null,
                    Depth = 0,
                    NodesEvaluated = _nodesEvaluated,
                    TimeMs = (float)stopwatch.Elapsed.TotalMilliseconds,
                    Score = 0
                };
            }

            List<MoveModel> bestMoves = new List<MoveModel>();
            float bestScore = 0f;

            // Use iterative deepening so "Depth" means completed depth (same meaning as AlphaBeta).
            int currentDepth = 1;
            while (currentDepth <= depth && (!HasTimeLimit || stopwatch.Elapsed.TotalMilliseconds < _timeLimitMs))
            {
                bool timedOut;
                var (iterativeBestMoves, iterativeBestScore) = SearchRootAtDepth(board, currentDepth, settings, stopwatch, out timedOut);

                if (!timedOut && iterativeBestMoves.Count > 0)
                {
                    bestMoves = iterativeBestMoves;
                    bestScore = iterativeBestScore;
                    _actualDepth = currentDepth;
                }
                else
                {
                    break;
                }

                currentDepth++;
            }

            stopwatch.Stop();
            var elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            var finalMove = bestMoves.Count > 0 ? bestMoves[new Random().Next(bestMoves.Count)] : rootMoves.First();

            return new SearchResult
            {
                Move = finalMove,
                Depth = _actualDepth,
                NodesEvaluated = _nodesEvaluated,
                TimeMs = elapsedMs,
                Score = bestScore
            };
        }

        private (List<MoveModel> bestMoves, float bestScore) SearchRootAtDepth(
            ChessBoardModel board,
            int depth,
            EvaluationSettings settings,
            Stopwatch stopwatch,
            out bool timedOut)
        {
            timedOut = false;

            var moves = MoveGenerator.GenerateMoves(board);
            if (moves == null || moves.Count == 0)
                return (new List<MoveModel>(), 0f);

            bool isWhiteRoot = board.IsWhiteTurn;
            float bestScore = isWhiteRoot ? float.MinValue : float.MaxValue;
            List<MoveModel> bestMoves = new List<MoveModel>();

            foreach (var move in moves)
            {
                if (HasTimeLimit && stopwatch.Elapsed.TotalMilliseconds > _timeLimitMs)
                {
                    timedOut = true;
                    break;
                }

                var newBoard = board.CloneForSearch();
                newBoard.MakeMove(move);

                float score = MinimaxRecursive(newBoard, depth - 1, !isWhiteRoot, settings, stopwatch, ref timedOut);
                if (timedOut)
                    break;

                bool isBetter = isWhiteRoot ? score > bestScore : score < bestScore;
                if (isBetter)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (Math.Abs(score - bestScore) < 0.001f)
                {
                    bestMoves.Add(move);
                }
            }

            return (bestMoves, bestScore);
        }

        private float MinimaxRecursive(
            ChessBoardModel board,
            int depth,
            bool isMaximizing,
            EvaluationSettings settings,
            Stopwatch stopwatch,
            ref bool timedOut)
        {
            _nodesEvaluated++;

            if (HasTimeLimit && stopwatch.Elapsed.TotalMilliseconds > _timeLimitMs)
            {
                timedOut = true;
                return 0f;
            }

            if (depth == 0 || board.IsGameOver())
                return AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            if (board.RepetitionCount >= 3)
                return 0f;

            var moves = MoveGenerator.GenerateMoves(board);
            if (moves == null || moves.Count == 0)
                return AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            float bestScore = isMaximizing ? float.MinValue : float.MaxValue;

            foreach (var move in moves)
            {
                var newBoard = board.CloneForSearch();
                newBoard.MakeMove(move);

                float score = MinimaxRecursive(newBoard, depth - 1, !isMaximizing, settings, stopwatch, ref timedOut);
                if (timedOut)
                    return AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

                bestScore = isMaximizing
                    ? Math.Max(bestScore, score)
                    : Math.Min(bestScore, score);
            }

            return bestScore;
        }
    }
}
