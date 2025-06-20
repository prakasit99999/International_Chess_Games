using System.Threading.Tasks;
using AI.Utilities;
using AIEngine.Core;
using Game.Interfaces;
using UnityEngine;

namespace AI.Adapters
{
    public class UnityAIBoardAdapter : IChessAI
    {
        public async Task<Vector2Int[]> CalculateMoveAsync(ChessBoard unityBoard, ChessPiece.Team currentTeam, AIDifficulty difficulty)
        {
            // แปลงกระดาน Unity เป็นโมเดล AI
            ChessBoardModel aiBoard = BoardConverter.Convert(unityBoard, currentTeam);

            // คำนวณการเดินใน thread background
            MoveModel aiMove = await Task.Run(() =>
                AICore.Instance.FindBestMove(aiBoard, (AICore.Difficulty)difficulty));

            // ตรวจ null ก่อนเข้าถึง
            if (aiMove == null)
            {
                Debug.LogWarning("AI ไม่สามารถหาการเดินที่เหมาะสมได้ (aiMove == null)");
                return null;
            }

            // แปลงตำแหน่งกลับเป็นพิกัด Unity
            Vector2Int aiFrom = new Vector2Int(aiMove.FromX, aiMove.FromY);
            Vector2Int aiTo = new Vector2Int(aiMove.ToX, aiMove.ToY);

            Vector2Int unityFrom = BoardConverter.ConvertPositionToUnity(aiFrom);
            Vector2Int unityTo = BoardConverter.ConvertPositionToUnity(aiTo);

            if (IsValidMove(unityBoard, unityFrom, unityTo))
            {
                Debug.Log($"AI ({difficulty}) เดิน: {unityFrom} → {unityTo}");
                return new Vector2Int[] { unityFrom, unityTo };
            }

            Debug.LogWarning($"การเดินของ AI ไม่ถูกต้อง: {unityFrom} → {unityTo}");
            return null;
        }

        private bool IsValidMove(ChessBoard board, Vector2Int from, Vector2Int to)
        {
            if (!board.PiecesOnBoard.ContainsKey(from))
            {
                Debug.LogWarning($"ไม่มีหมากที่ตำแหน่งเริ่มต้น: {from}");
                return false;
            }

            if (to.x < 0 || to.x >= 8 || to.y < 0 || to.y >= 8)
            {
                Debug.LogWarning($"ตำแหน่งปลายทางอยู่นอกกระดาน: {to}");
                return false;
            }

            ChessPiece piece = board.PiecesOnBoard[from];
            if (piece.team != board.GetGameManager().GetCurrentTurn())
            {
                Debug.LogWarning($"หมากไม่ใช่ของทีมปัจจุบัน: {piece.team}");
                return false;
            }

            return true;
        }
    }
}
