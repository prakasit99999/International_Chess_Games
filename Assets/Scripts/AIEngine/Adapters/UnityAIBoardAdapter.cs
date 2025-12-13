using System.Collections;
using System.Linq;
using AI.Utilities;
using AIEngine.Core;
using AIEngine.Utilities;
using Game.Interfaces;
using UnityEngine;
using static ChessPiece;
using Debug = UnityEngine.Debug;
namespace AIEngine.Adapters
{
    public class UnityAIBoardAdapter : MonoBehaviour, IChessAI
    {
        private SearchResult? _calculatedResult;
        private bool _isCalculating = false;

        public void StartCalculateMove(ChessBoard board, Team aiTeam, Team currentTurn, AIDifficulty difficulty)
        {
            if (!_isCalculating)
            {
                StartCoroutine(CalculateMoveCoroutine(board, aiTeam, currentTurn, difficulty));
            }
        }

        private IEnumerator CalculateMoveCoroutine(ChessBoard board, Team aiTeam, Team currentTurn, AIDifficulty difficulty)
        {
            _isCalculating = true;

            // ใช้ค่า team ที่ส่งมาจาก GameManager (ถูกต้องเสมอ)
            ChessBoardModel model = BoardConverter.Convert(board, aiTeam, currentTurn);
            var aiDifficulty = ConvertDifficulty(difficulty);

            Debug.Log($"[AI INPUT] Team={aiTeam}, CurrentTurn={GameManager.Instance.CurrentTurn}");

            // เรียก FindBestMoveWithMetrics เพื่อได้ metrics ที่ถูกต้อง
            var searchResult = AICore.Instance.FindBestMoveWithMetrics(model, aiDifficulty);
            var move = searchResult.Move;

            // 🟢 บันทึกค่าประสิทธิภาพของ AI ที่ถูกต้อง
            if (PerformanceTracker.Instance != null)
            {
                PerformanceTracker.Instance.AddMove(
                    searchResult.Depth,           // ความลึกที่ AI คิดจริงๆ
                    searchResult.NodesEvaluated,  // จำนวน nodes ที่ evaluate จริงๆ
                    searchResult.TimeMs           // เวลาในการคิด 1 ตา (ms)
                );

                Debug.Log($"[PERFORMANCE] Depth={searchResult.Depth}, Nodes={searchResult.NodesEvaluated}, Time={searchResult.TimeMs:F2}ms");
            }

            if (move != null)
            {
                // แก้เป็น (row,col) → (y,x)
                var from = BoardConverter.ConvertPositionToUnity(new Vector2Int(move.FromY, move.FromX));
                var to = BoardConverter.ConvertPositionToUnity(new Vector2Int(move.ToY, move.ToX));

                // ✅ Set Unity Vector2Int positions in MoveModel
                move.From = from;
                move.To = to;

                _calculatedResult = searchResult;

                Debug.Log($"[AI RAW MOVE] From=({move.FromX},{move.FromY}) To=({move.ToX},{move.ToY})");
                Debug.Log($"[UNITY MOVE] From={from} To={to}, CurrentTurn={aiTeam}");

            }
            else
            {
                Debug.LogWarning("❌ AI ไม่พบ move ที่ถูกต้อง");
                _calculatedResult = null;
            }

            _isCalculating = false;
            yield break;
        }


        //private IEnumerator CalculateMoveCoroutine(ChessBoard board, Team team, AIDifficulty difficulty)
        //{
        //    _isCalculating = true;

        //    // Unity → AI Model
        //    ChessBoardModel model = BoardConverter.Convert(board, team);
        //    var aiDifficulty = ConvertDifficulty(difficulty);
        //    Debug.Log($"[AI INPUT] Team={team}");

        //    // รัน AI หาตาเดิน
        //    var move = AICore.Instance.FindBestMove(model, aiDifficulty);

        //    if (move != null)
        //    {
        //        // ✅ MoveModel เก็บเป็น [row,col] → ต้องแปลงเป็น (x=col, y=row)
        //        var from = BoardConverter.ConvertPositionToUnity(new Vector2Int(move.FromY, move.FromX));
        //        var to = BoardConverter.ConvertPositionToUnity(new Vector2Int(move.ToY, move.ToX));
        //        //var from = BoardConverter.ConvertPositionToUnity(new Vector2Int(move.FromX, move.FromY));
        //        //var to = BoardConverter.ConvertPositionToUnity(new Vector2Int(move.ToX, move.ToY));

        //        _calculatedMove = new Vector2Int[] { from, to };

        //        //Debug.Log($"[AI MOVE MODEL] From=({move.FromX},{move.FromY}) To=({move.ToX},{move.ToY})");
        //        //Debug.Log($"[UNITY MOVE] From={from} To={to}");
        //        Debug.Log($"[AI RAW MOVE] From=({move.FromX},{move.FromY}) To=({move.ToX},{move.ToY})");
        //        Debug.Log($"[UNITY MOVE] From={from} To={to}, CurrentTurn={team}");

        //    }
        //    else
        //    {
        //        Debug.LogWarning("❌ AI ไม่พบ move ที่ถูกต้อง");
        //        _calculatedMove = null;
        //    }

        //    _isCalculating = false;
        //    yield break;
        //}

        //private IEnumerator CalculateMoveCoroutine(ChessBoard board, Team team, AIDifficulty difficulty)
        //{
        //    _calculatedMove = null;
        //    _isCalculating = true;

        //    // 1. ✅ แก้ไข: แปลงกระดานโดยดึงสถานะจาก board โดยตรง
        //    ChessBoardModel aiBoard = BoardConverter.Convert(board, team);

        //    // 2. ✅ แก้ไข: รอจบเฟรมปัจจุบัน เพื่อไม่ให้เกมค้าง
        //    yield return null;

        //    // 3. เริ่มคำนวณ (ส่วนนี้จะกินเวลา แต่ไม่ทำให้เกมค้าง)
        //    var aiDifficulty = ConvertDifficulty(difficulty);
        //    MoveModel bestMove = AICore.Instance.FindBestMove(aiBoard, aiDifficulty);

        //    // 4. แปลงผลลัพธ์กลับมา
        //    if (bestMove != null)
        //    {
        //        // ✅ แก้ไข: สร้าง Vector2Int ให้ถูกต้อง (คอลัมน์, แถว)
        //        var fromAI = new Vector2Int(bestMove.FromY, bestMove.FromX);
        //        var toAI = new Vector2Int(bestMove.ToY, bestMove.ToX);

        //        _calculatedMove = new Vector2Int[]
        //        {
        //            BoardConverter.ConvertPositionToUnity(fromAI),
        //            BoardConverter.ConvertPositionToUnity(toAI)
        //        };
        //    }
        //    else
        //    {
        //        Debug.LogWarning("AI could not find a valid move.");
        //    }

        //    _isCalculating = false;
        //}

        public (Vector2Int from, Vector2Int to)? GetCalculatedMove()
        {
            if (_calculatedResult.HasValue && _calculatedResult.Value.Move != null)
            {
                return (_calculatedResult.Value.Move.From, _calculatedResult.Value.Move.To);
            }
            return null;
        }

        public void ClearCalculatedMove()
        {
            _calculatedResult = null;
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
