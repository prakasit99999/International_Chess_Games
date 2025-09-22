using AI.Utilities;
using AI.Utilities;
using AIEngine.Core;
using AIEngine.Core;
using Game.Interfaces;
using Game.Interfaces;   // ใช้ AIDifficulty
using System.Collections;
using UnityEngine;
using static ChessPiece;
namespace AIEngine.Adapters
{
    /// <summary>
    /// Adapter ระหว่าง Unity ChessBoard กับ AIEngine
    /// </summary>

    public class UnityAIBoardAdapter : MonoBehaviour, IChessAI
    {
        private Vector2Int[] _calculatedMove;
        private bool _isCalculating = false;

        public void StartCalculateMove(ChessBoard board, Team team, AIDifficulty difficulty)
        {
            if (_isCalculating) return; // ป้องกันการเริ่มคำนวณซ้ำซ้อน

            StartCoroutine(CalculateMoveCoroutine(board, difficulty));
        }

        //public void StartCalculateMove(ChessBoard board, ChessPiece.Team team, AIDifficulty difficulty)
        //{
        //    if (_isCalculating) return;
        //    var model = BoardConverter.Convert(board, team);
        //    _isCalculating = true;
        //    _calculatedMove = null;
        //    // แปลง difficulty ของ Game.Interfaces → AICore.Difficulty
        //    var aiDifficulty = ConvertDifficulty(difficulty);

        //    var bestMove = AICore.Instance.FindBestMove(model, aiDifficulty);

        //    if (bestMove != null)
        //    {
        //        _calculatedMove = new[]
        //        {
        //        BoardConverter.ConvertPositionToUnity(new Vector2Int(bestMove.FromX, bestMove.FromY)),
        //        BoardConverter.ConvertPositionToUnity(new Vector2Int(bestMove.ToX, bestMove.ToY))
        //    };
        //    }
        //}

        private IEnumerator CalculateMoveCoroutine(ChessBoard board, AIDifficulty difficulty)
        {
            _isCalculating = true;
            _calculatedMove = null;

            // 1. ✅ แก้ไข: แปลงกระดานโดยดึงสถานะจาก board โดยตรง
            ChessBoardModel aiBoard = BoardConverter.Convert(board);

            // 2. ✅ แก้ไข: รอจบเฟรมปัจจุบัน เพื่อไม่ให้เกมค้าง
            yield return null;

            // 3. เริ่มคำนวณ (ส่วนนี้จะกินเวลา แต่ไม่ทำให้เกมค้าง)
            var aiDifficulty = ConvertDifficulty(difficulty);
            MoveModel bestMove = AICore.Instance.FindBestMove(aiBoard, aiDifficulty);

            // 4. แปลงผลลัพธ์กลับมา
            if (bestMove != null)
            {
                // ✅ แก้ไข: สร้าง Vector2Int ให้ถูกต้อง (คอลัมน์, แถว)
                var fromAI = new Vector2Int(bestMove.FromY, bestMove.FromX);
                var toAI = new Vector2Int(bestMove.ToY, bestMove.ToX);

                _calculatedMove = new Vector2Int[]
                {
                    BoardConverter.ConvertPositionToUnity(fromAI),
                    BoardConverter.ConvertPositionToUnity(toAI)
                };
            }
            else
            {
                Debug.LogWarning("AI could not find a valid move.");
            }

            _isCalculating = false;
        }

        public Vector2Int[] GetCalculatedMove()
        {
            if (_isCalculating)
            {
                return null;
            }
            return _calculatedMove;
        }

        public void ClearCalculatedMove()
        {
            _calculatedMove = null;
        }

        // 🔹 ตัวช่วยแปลง enum
        private AICore.Difficulty ConvertDifficulty(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => AICore.Difficulty.Easy,
                AIDifficulty.Normal => AICore.Difficulty.Normal,
                AIDifficulty.Hard => AICore.Difficulty.Hard,
                _ => AICore.Difficulty.Normal
            };
        }
    }
}
