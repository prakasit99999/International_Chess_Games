using System;
using System.Collections.Generic;
using System.Linq;

namespace AIEngine.Utilities
{
    public class MoveOrderer
    {
        // Scoring Constants
        private const int PV_MOVE_SCORE = 1_200_000; // Principal Variation move from previous iteration
        private const int TT_MOVE_SCORE = 1_000_000; // ค่า TT Move
        private const int CAPTURE_BASE = 8000; // ค่า Capture
        private const int SEE_LITE_WINNING_BONUS = 600;
        private const int SEE_LITE_TRADE_PENALTY = 300;
        private const int SEE_LITE_LOSING_PENALTY = 1800;
        private const int HISTORY_SCORE_SHIFT = 3;
        private const int HISTORY_SCORE_CAP = 4000;
        private const int PROMOTION_BONUS = 7000; // Bonus for promotion moves
        private const int KILLER_1_SCORE = 4000; // ค่า Killer Move 1
        private const int KILLER_2_SCORE = 3900; // ค่า Killer Move 2

        // Piece Values: None, Pawn, Knight, Bishop, Rook, Queen, King
        private static readonly int[] PieceValues = { 0, 100, 300, 310, 500, 900, 20000 };

        public static void OrderMoves(
            List<MoveModel> moves,
            ChessBoardModel board,
            MoveModel ttMove,
            MoveModel[] killerMoves,
            int[,,,] historyMoves,
            MoveModel pvMove = null)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                moves[i].Score = GetMoveScore(moves[i], board, ttMove, killerMoves, historyMoves, pvMove);
            }

            // Sort by Score Descending
            moves.Sort((a, b) => b.Score.CompareTo(a.Score));
        }

        public static void OrderCaptures(List<MoveModel> captures, ChessBoardModel board)
        {
            for (int i = 0; i < captures.Count; i++)
            {
                captures[i].Score = ScoreCaptureOnly(captures[i], board);
            }

            captures.Sort((a, b) => b.Score.CompareTo(a.Score));
        }

        private static int GetMoveScore(
                MoveModel move,
                ChessBoardModel board,
                MoveModel ttMove,
                MoveModel[] killerMoves,
                int[,,,] historyMoves,
                MoveModel pvMove)
        {
            int score = 0;

            // 0. PV move should always be searched first in next iterative-deepening iteration.
            if (pvMove != null && IsSameMove(move, pvMove))
                score += PV_MOVE_SCORE;

            // 1. TT Move (Best from previous search)
            if (ttMove != null && IsSameMove(move, ttMove))
                score += TT_MOVE_SCORE;

            // 2. Captures (MVV-LVA)
            int captured = board.Board[move.ToX, move.ToY];
            if (captured != 0)
            {
                score += ScoreCaptureOnly(move, board);
            }
            else
            {
                // 3. Killer Moves (Quiet moves only)
                if (killerMoves != null)
                {
                    if (killerMoves.Length > 0 && killerMoves[0] != null && IsSameMove(move, killerMoves[0]))
                        score += KILLER_1_SCORE;

                    if (killerMoves.Length > 1 && killerMoves[1] != null && IsSameMove(move, killerMoves[1]))
                        score += KILLER_2_SCORE;
                }

                // 4. History Heuristic
                if (historyMoves != null)
                {
                    int history = historyMoves[move.FromX, move.FromY, move.ToX, move.ToY] >> HISTORY_SCORE_SHIFT;
                    if (history > HISTORY_SCORE_CAP) history = HISTORY_SCORE_CAP;
                    score += history;
                }
            }

            // Promotion is tactical and should be tried early even if not a capture.
            if (move.PromotionPiece != 0)
            {
                int promotionAbs = Math.Abs(move.PromotionPiece);
                int bonus = promotionAbs == 5 ? 2000 : 500; // Queen promotion gets higher priority.
                score += PROMOTION_BONUS + bonus;
            }
            return score;
        }

        private static int ScoreCaptureOnly(MoveModel move, ChessBoardModel board)
        {
            int captured = board.Board[move.ToX, move.ToY];
            int attacker = board.Board[move.FromX, move.FromY];
            if (captured == 0)
            {
                // En passant: destination is empty but still captures a pawn.
                bool isPawn = Math.Abs(attacker) == 1;
                if (isPawn && board.EnPassantTarget.HasValue &&
                    move.ToX == board.EnPassantTarget.Value.X &&
                    move.ToY == board.EnPassantTarget.Value.Y)
                {
                    captured = attacker > 0 ? -1 : 1; // Captured pawn color is opposite.
                }
            }
            if (captured == 0)
                return 0;

            int victimValue = PieceValues[Math.Abs(captured)];
            int attackerValue = PieceValues[Math.Abs(attacker)];
            int seeLite = EvaluateSeeLite(board, move, attacker, victimValue, attackerValue);

            // Higher victim, lower attacker -> higher priority.
return CAPTURE_BASE + (victimValue * 100 - attackerValue * 10) + seeLite;        }

        private static int EvaluateSeeLite(ChessBoardModel board, MoveModel move, int attackerPiece, int victimValue, int attackerValue)
        {
            if (attackerPiece == 0)
                return 0;

            bool movingWhite = attackerPiece > 0;
            var target = new Square(move.ToX, move.ToY);
            bool enemyAttacksTarget = board.IsSquareUnderAttack(target, !movingWhite);
            bool ownAttacksTarget = board.IsSquareUnderAttack(target, movingWhite);

            int net = victimValue - attackerValue;
            if (net < 0 && enemyAttacksTarget)
                return -SEE_LITE_LOSING_PENALTY;

            if (enemyAttacksTarget && !ownAttacksTarget)
                return -SEE_LITE_TRADE_PENALTY;

            if (!enemyAttacksTarget)
                return SEE_LITE_WINNING_BONUS;

            return 0;
        }

        public static bool IsSameMove(MoveModel a, MoveModel b)
        {
            return a.FromX == b.FromX && a.FromY == b.FromY && a.ToX == b.ToX && a.ToY == b.ToY;
        }
    }
}
