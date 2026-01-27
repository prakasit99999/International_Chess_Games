using AIEngine.Utilities;
using UnityEngine;
using static ChessPiece;

namespace Game.Interfaces
{
    public enum AIDifficulty
    {
        None,
        Easy,
        Normal,
        Hard
    }

    public interface IChessAI
    {
        /// เริ่มให้ AI คำนวณตาเดิน (ใช้ Coroutine ไม่ block main thread)
        void StartCalculateMove(ChessBoard board, ChessPiece.Team aiTeam, Team currentTurn, AIDifficulty difficulty);
        /// คืนค่าตาเดินที่ AI คำนวณเสร็จแล้ว เป็น (from, to) tuple (null ถ้ายังไม่เสร็จ)
        (Vector2Int from, Vector2Int to)? GetCalculatedMove();
        /// ล้างค่าหลังจากนำ move ไปใช้งานแล้ว
        void ClearCalculatedMove();
    }
}
