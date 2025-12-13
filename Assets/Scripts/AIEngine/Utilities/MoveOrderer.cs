using System;
using System.Collections.Generic;
using System.Linq;

namespace AIEngine.Utilities
{
    public class MoveOrderer
    {
        // Scoring Constants
        private const int TT_MOVE_SCORE = 1_000_000;
        private const int CAPTURE_BASE = 8000;
        private const int KILLER_1_SCORE = 4000;
        private const int KILLER_2_SCORE = 3900;

        // Piece Values: None, Pawn, Knight, Bishop, Rook, Queen, King
        private static readonly int[] PieceValues = { 0, 100, 300, 310, 500, 900, 20000 };

        public static void OrderMoves(
            List<MoveModel> moves,
            ChessBoardModel board,
            MoveModel ttMove,
            MoveModel[] killerMoves,
            int[,,,] historyMoves)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                moves[i].Score = GetMoveScore(moves[i], board, ttMove, killerMoves, historyMoves);
            }

            // Sort by Score Descending
            moves.Sort((a, b) => b.Score.CompareTo(a.Score));
        }

        private static int GetMoveScore(
                MoveModel move,
                ChessBoardModel board,
                MoveModel ttMove,
                MoveModel[] killerMoves,
                int[,,,] historyMoves)
        {
            int score = 0;

            // 1. TT Move (Best from previous search)
            if (ttMove != null && IsSameMove(move, ttMove))
                score += TT_MOVE_SCORE;

            // 2. Captures (MVV-LVA)
            int captured = board.Board[move.ToX, move.ToY];
            if (captured != 0)
            {
                int attacker = board.Board[move.FromX, move.FromY];
                int victimValue = PieceValues[Math.Abs(captured)];
                int attackerValue = PieceValues[Math.Abs(attacker)];
                //
                score += CAPTURE_BASE + (victimValue * 10) - attackerValue;
            }
            else
            {
                // 3. Killer Moves (Quiet moves only)
                if (killerMoves != null)
                {
                    if (killerMoves[0] != null && IsSameMove(move, killerMoves[0]))
                        score += KILLER_1_SCORE;

                    if (killerMoves[1] != null && IsSameMove(move, killerMoves[1]))
                        score += KILLER_2_SCORE;
                }

                // 4. History Heuristic
                if (historyMoves != null)
                    score += historyMoves[move.FromX, move.FromY, move.ToX, move.ToY];
            }

            return score;
        }

        public static bool IsSameMove(MoveModel a, MoveModel b)
        {
            return a.FromX == b.FromX && a.FromY == b.FromY && a.ToX == b.ToX && a.ToY == b.ToY;
        }
    }
}