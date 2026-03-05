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

        private EvaluationSettings CreateSettingsForDifficulty(Difficulty difficulty)
        {
           switch (difficulty)
            {
                case Difficulty.Easy:
                    settings.Name = "Easy";
                    settings.PositionalFactor = 0.7f;
                    settings.PassedPawnBonus = 10;
                    settings.IsolatedPawnPenalty = -8;
                    settings.DoubledPawnPenalty = -5;
                    settings.UseTempo = false;
                    settings.UseKingSafety = false;
                    settings.UseThreats = false;
                    settings.UseSpace = false;
                    settings.MobilityWeight = 1;
                    break;
                case Difficulty.Normal:
                    settings.Name = "Normal";
                    settings.PositionalFactor = 1.0f;
                    settings.UseTempo = false;
                    settings.MobilityWeight = 2;
                    break;
                case Difficulty.Hard:
                    settings.Name = "Hard";
                    settings.PositionalFactor = 1.15f;
                    settings.PassedPawnBonus = 30;
                    settings.IsolatedPawnPenalty = -18;
                    settings.DoubledPawnPenalty = -14;
                    settings.UseTempo = true;
                    settings.MobilityWeight = 3;
                    settings.PawnShieldBonus = 15;
                    settings.TropismWeight = 6;
                    settings.BishopPairBonus = 35;
                    settings.RookOpenFileBonus = 24;
                    settings.RookSemiOpenFileBonus = 14;
                    settings.KnightOutpostBonus = 24;
                    settings.SpaceWeight = 2;
                    settings.HangingPiecePenalty = -28;
                    break;
                default:
                    throw new ArgumentException("Invalid difficulty level");
            }
            return settings;
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
