//using AI.Models;
#nullable enable
using Codice.CM.Client.Differences;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIEngine.Utilities
{
    public class MoveOrderer
    {
        // [ส่วนที่ 1] กำหนดค่าคะแนนคงที่ (Constants)
        // เพื่อให้เราปรับจูนความฉลาดของ AI ได้ง่ายๆ ในที่เดียวครับ
        private const int TT_MOVE_SCORE = 1000000;      // สำคัญที่สุด! (หลักล้าน)
        private const int WINNING_CAPTURE_BIAS = 8000;  // คะแนนพื้นฐานของการกิน
        private const int KILLER_MOVE_SCORE = 4000;     // คะแนนท่า Killer
        // [ส่วนที่ 2] ตารางคะแนนตัวหมากสำหรับ MVV-LVA (Array เข้าถึงเร็วกว่า Dictionary)
        // Index: 0=None, 1=Pawn, 2=Knight, 3=Bishop, 4=Rook, 5=Queen, 6=King
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
            // เรียงลำดับท่าที่ได้คะแนนสูงสุดไปต่ำสุด
            moves.Sort((a, b) => b.Score.CompareTo(a.Score));
        }
        private static int GetMoveScore(
                MoveModel move,
                ChessBoardModel board,
                MoveModel ttMove,
                MoveModel[] killerMoves,
                int[,,,] historyMoves)
        {
            // [ลำดับ 1] ตรวจสอบว่าเป็นท่าจาก Transposition Table หรือไม่ (สำคัญที่สุด!)
            if (ttMove != null && IsSameMove(move, ttMove))
            {
                return TT_MOVE_SCORE; // 1,000,000 คะแนน
            }

            // [ลำดับ 2] ตรวจสอบว่าเป็นท่ากิน (Captures) -> ใช้สูตร MVV-LVA ที่เราเพิ่งคำนวณ
            int capturedPiece = board.Board[move.ToX, move.ToY];
            if (capturedPiece != 0)
            {
                int attacker = board.Board[move.FromX, move.FromY];
                int victimValue = PieceValues[Math.Abs(capturedPiece)];
                int attackerValue = PieceValues[Math.Abs(attacker)];
                // สูตร: (เหยื่อ x 10) - ผู้โจมตี + คะแนนฐาน(8000)
                return (victimValue * 10) - attackerValue + WINNING_CAPTURE_BIAS;
            }
                // [ลำดับ 3] ตรวจสอบว่าเป็น Killer Move หรือไม่
                if (killerMoves != null)
                {
                    // ถ้าตรงกับ Killer ตัวที่ 1
                    if (killerMoves[0] != null && IsSameMove(move, killerMoves[0]))
                        return KILLER_MOVE_SCORE;

                    // ถ้าตรงกับ Killer ตัวที่ 2 (ให้คะแนนรองลงมาหน่อย)
                    if (killerMoves[1] != null && IsSameMove(move, killerMoves[1]))
                        return KILLER_MOVE_SCORE - 100;
                }
                // [ลำดับ 4] ตรวจสอบประวัติการเดิน (History Heuristic)
                if (historyMoves != null)
                {
                    // ดึงคะแนนจากสถิติที่เราเก็บสะสมมา
                    return historyMoves[move.FromX, move.FromY, move.ToX, move.ToY];
                }

                return 0; // ท่าเดินปกติ ไม่มีอะไรพิเศษ
            }

        // Helper สำหรับเปรียบเทียบว่าใช่ท่าเดียวกันไหม
        public static bool IsSameMove(MoveModel a, MoveModel b)
        {
            return a.FromX == b.FromX && a.FromY == b.FromY && a.ToX == b.ToX && a.ToY == b.ToY;
        }
    }
}