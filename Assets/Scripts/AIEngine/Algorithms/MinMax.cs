using System;
using AIEngine.Utilities;
using AIEngine.Evaluation;
using System.Linq;

namespace AIEngine.Algorithms
{
    public class Minimax : SearchAlgorithm
    {
        public override MoveModel FindBestMove(ChessBoardModel board, int depth)
        {
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

            return bestMove ?? moves.First();
        }

        protected int MinimaxRecursive(ChessBoardModel board, int depth, bool isMaximizing)
        {
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
