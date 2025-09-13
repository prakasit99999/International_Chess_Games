using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static AIEngine.Core.AICore;
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
        // เริ่มให้ AI คำนวณ แต่ไม่ใช้ async
        void StartCalculateMove(ChessBoard board, ChessPiece.Team team, AIDifficulty difficulty);
        // เรียกมาดูว่า AI คิดเสร็จแล้วหรือยัง
        Vector2Int[] GetCalculatedMove();
    }
}