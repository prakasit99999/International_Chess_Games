using AIEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using AIEngine.Evaluation;
namespace AIEngine.Algorithms
{
    public class AlphaBeta : SearchAlgorithm
    {
        private readonly Dictionary<string, int> _transpositionTable = new Dictionary<string, int>();
        private const int TimeLimitMs = 10000;

        public override MoveModel FindBestMove(ChessBoardModel board, int maxDepth)
        {
            var startTime = DateTime.Now;
            MoveModel bestMove = null;
            var currentDepth = 1;

            while (currentDepth <= maxDepth && (DateTime.Now - startTime).TotalMilliseconds < TimeLimitMs)
            {
                var iterativeBest = AlphaBetaSearch(board, currentDepth, startTime, bestMove);
                if (iterativeBest != null)
                {
                    bestMove = iterativeBest;
                }
                currentDepth++;
            }
            return bestMove ?? MoveGenerator.GenerateMoves(board).FirstOrDefault() ?? throw new InvalidOperationException("No valid moves found.");
        }


        private MoveModel AlphaBetaSearch(ChessBoardModel board, int depth, DateTime startTime, MoveModel previousBest)
        {
            var moves = MoveOrderer.OrderMoves(MoveGenerator.GenerateMoves(board), board, null);
            var bestScore = int.MinValue;
            MoveModel bestMove = null;

            foreach (var move in moves)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) break;

                var newBoard = board.Clone();
                newBoard.MakeMove(move);
                var score = -AlphaBetaRecursive(newBoard, depth - 1, int.MinValue, int.MaxValue, false, startTime);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return bestMove;
        }

        private int AlphaBetaRecursive(ChessBoardModel board, int depth, int alpha, int beta, bool maximizingPlayer, DateTime startTime)
        {
            if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) return 0;
            if (board.IsGameOver()) return AIEngine.Evaluation.Evaluation.Evaluate(board) * (maximizingPlayer ? 1 : -1);

            var boardKey = $"{board.SerializeBoard()}{maximizingPlayer}";
            if (_transpositionTable.TryGetValue(boardKey, out var cached)) return cached;

            if (depth <= 0)
            {
                var score = QuiescenceSearch(board, alpha, beta, startTime);
                _transpositionTable[boardKey] = score;
                return score;
            }

            var moves = MoveOrderer.OrderMoves(MoveGenerator.GenerateMoves(board), board, null);
            var bestValue = maximizingPlayer ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);
                var value = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, !maximizingPlayer, startTime);

                bestValue = maximizingPlayer
                    ? Math.Max(bestValue, value)
                    : Math.Min(bestValue, value);

                if (maximizingPlayer)
                    alpha = Math.Max(alpha, bestValue);
                else
                    beta = Math.Min(beta, bestValue);

                if (beta <= alpha) break;
            }

            _transpositionTable[boardKey] = bestValue;
            return bestValue;
        }

        private int QuiescenceSearch(ChessBoardModel board, int alpha, int beta, DateTime startTime)
        {
            var standPat = AIEngine.Evaluation.Evaluation.Evaluate(board);
            if (standPat >= beta) return beta;
            alpha = Math.Max(alpha, standPat);

            var captures = MoveGenerator.GenerateMoves(board)
                .Where(m => board.Board[m.ToX, m.ToY] != 0)
                .OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY]));

            foreach (var move in captures)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) return alpha;

                var newBoard = board.Clone();
                newBoard.MakeMove(move);
                var score = -QuiescenceSearch(newBoard, -beta, -alpha, startTime);

                alpha = Math.Max(alpha, -score);
                if (alpha >= beta) break;
            }
            return alpha;
        }

    }
}
