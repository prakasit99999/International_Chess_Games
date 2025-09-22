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
            int currentDepth = 1;

            while (currentDepth <= maxDepth && (DateTime.Now - startTime).TotalMilliseconds < TimeLimitMs)
            {
                var iterativeBest = AlphaBetaSearch(board, currentDepth, startTime, bestMove);
                if (iterativeBest != null)
                {
                    bestMove = iterativeBest;
                }
                currentDepth++;
            }

            return bestMove ?? MoveGenerator.GenerateMoves(board).FirstOrDefault()
                   ?? throw new InvalidOperationException("No valid moves found.");
        }

        private MoveModel AlphaBetaSearch(ChessBoardModel board, int depth, DateTime startTime, MoveModel previousBest)
        {
            var moves = MoveOrderer.OrderMoves(MoveGenerator.GenerateMoves(board), board, previousBest);
            int bestScore = int.MinValue;
            MoveModel bestMove = null;

            int alpha = int.MinValue;
            int beta = int.MaxValue;

            foreach (var move in moves)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) break;

                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int score;
                if (newBoard.RepetitionCount >= 3)
                    score = 0; // draw
                else
                    score = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, false, startTime);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                // ✅ update alpha ที่ root
                alpha = Math.Max(alpha, bestScore);

                // ✅ prune ที่ root
                if (alpha >= beta)
                    break;
            }
            return bestMove;
        }



        private int AlphaBetaRecursive(ChessBoardModel board, int depth, int alpha, int beta, bool maximizingPlayer, DateTime startTime)
        {

            // ✅ timeout -> return alpha (ไม่ใช่ 0)
            if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) return alpha;


            if (board.IsGameOver())
            {
                // ✅ Evaluation() ของคุณ normalize อยู่แล้ว ไม่ต้องคูณ maximizingPlayer
                return AIEngine.Evaluation.Evaluation.Evaluate(board);
            }

            var boardKey = $"{board.SerializeBoard()}{maximizingPlayer}";

            // ✅ threefold repetition -> draw
            if (board.RepetitionCount >= 3)
            {
                //_transpositionTable[boardKey] = 0;
                return 0;
            }

            if (_transpositionTable.TryGetValue(boardKey, out var cached)) return cached;

            if (depth <= 0)
            {
                var score = QuiescenceSearch(board, alpha, beta, maximizingPlayer, startTime);
                _transpositionTable[boardKey] = score;
                return score;
            }

            var moves = MoveOrderer.OrderMoves(MoveGenerator.GenerateMoves(board), board, null);
            var bestValue = maximizingPlayer ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int value = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, !maximizingPlayer, startTime);

                if (maximizingPlayer)
                {
                    bestValue = Math.Max(bestValue, value);
                    alpha = Math.Max(alpha, bestValue);
                }
                else
                {
                    bestValue = Math.Min(bestValue, value);
                    beta = Math.Min(beta, bestValue);
                }

                if (beta <= alpha) break;
            }

            _transpositionTable[boardKey] = bestValue;
            return bestValue;
        }

        private int QuiescenceSearch(ChessBoardModel board, int alpha, int beta, bool maximizingPlayer, DateTime startTime)
        {
            if (board.RepetitionCount >= 3)
            {
                return 0;
            }
            var standPat = AIEngine.Evaluation.Evaluation.Evaluate(board);

            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            var captures = MoveGenerator.GenerateMoves(board)
                .Where(m => board.Board[m.ToX, m.ToY] != 0)
                .OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY]));

         
            foreach (var move in captures)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) return alpha;

                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int score = -QuiescenceSearch(newBoard, alpha, beta, !maximizingPlayer, startTime);

                if (maximizingPlayer)
                {
                    if (score > alpha) alpha = score;
                    if (alpha >= beta) break;
                }
                else
                {
                    if (score < beta) beta = score;
                    if (beta <= alpha) break;
                }
            }

            return maximizingPlayer ? alpha : beta;
        }
    }
}
