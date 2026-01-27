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
        private AICore _aiService = new AICore();

        public int LastDepth { get; private set; }
        public int NodesEvaluated { get; private set; }
        public float LastMoveTimeMs { get; private set; }
        public int LastEvalScore { get; private set; }

        public void StartCalculateMove(ChessBoard board, Team aiTeam, Team currentTurn, AIDifficulty difficulty)
        {
            if (!_isCalculating)
            {
                StartCoroutine(CalculateMoveCoroutine(board, aiTeam, currentTurn, difficulty));
            }
        }

        private IEnumerator CalculateMoveCoroutine(ChessBoard board, Team aiTeam, Team currentTurn, AIDifficulty difficulty)
        {
            Debug.Log($"[AI] Start Calculate Move for {aiTeam}");
            _isCalculating = true;

            // แปลงกระดาน Unity -> AI
            ChessBoardModel model = BoardConverter.Convert(board, aiTeam, currentTurn);

            // แปลงความยาก
            var aiCoreDifficulty = ConvertDifficulty(difficulty);

            Debug.Log($"[AI INPUT] Team={aiTeam}, Difficulty={aiCoreDifficulty}");

            //  แก้ไขจุดที่ 1: เปลี่ยนชื่อเมธอดให้ตรงกับ AICore
            SearchResult searchResult = _aiService.FindBestMoveWithMetrics(model, aiCoreDifficulty);

            if (searchResult.Move != null)
            {
                var move = searchResult.Move;

                //  แก้ไขจุดที่ 2: แปลงพิกัด AI (Row, Col) -> Unity (X, Y)
                // AI Model: [Row, Col] -> [X, Y] ใน MoveModel
                // Unity: X=Col, Y=Row (โดย Row 0 ของ AI คือ Y=7 ของ Unity)

                // AI.X (Row) -> Unity.Y (7 - Row)
                // AI.Y (Col) -> Unity.X (Col)

                int unityFromX = move.FromY;      // Col ตรงกัน
                int unityFromY = 7 - move.FromX;  // Row กลับด้าน

                int unityToX = move.ToY;
                int unityToY = 7 - move.ToX;

                Vector2Int from = new Vector2Int(unityFromX, unityFromY);
                Vector2Int to = new Vector2Int(unityToX, unityToY);
                // -----------------------------------------------------------

                // บันทึกค่าผลลัพธ์เพื่อส่งกลับ
                _calculatedResult = searchResult;
                LastDepth = searchResult.Depth;
                NodesEvaluated = searchResult.NodesEvaluated;
                LastMoveTimeMs = searchResult.TimeMs;
                LastEvalScore = (int)searchResult.Score; // สมมติว่าใน SearchResult มี Score
                // Debug.Log($"[AI UnityAIBoardAdapter] _calculatedResult {searchResult} | Depth: {LastDepth} | Nodes: {NodesEvaluated} | Time: {LastMoveTimeMs} | Score: {LastEvalScore}");

                Debug.Log($"[AI OUTPUT] Move: {from} -> {to} | Depth: {LastDepth} | Nodes: {NodesEvaluated}");
            }
            else
            {
                Debug.LogWarning("❌ AI ไม่พบ move ที่ถูกต้อง");
                _calculatedResult = new SearchResult(); // หรือ null
            }

            _isCalculating = false;
            yield break;
        }

        public (Vector2Int from, Vector2Int to)? GetCalculatedMove()
        {
            if (_calculatedResult.Value.Move != null)
            {
                var move = _calculatedResult.Value.Move;

                // แปลงสูตรเดียวกับด้านบน
                int ux1 = move.FromY;
                int uy1 = 7 - move.FromX;
                int ux2 = move.ToY;
                int uy2 = 7 - move.ToX;

                return (new Vector2Int(ux1, uy1), new Vector2Int(ux2, uy2));
            }
            return null;
        }

        public void ClearCalculatedMove()
        {
            _calculatedResult = new SearchResult();
        }

        // 🔹 ตัวช่วยแปลง enum
        private AICore.Difficulty ConvertDifficulty(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => AICore.Difficulty.Easy,
                AIDifficulty.Normal => AICore.Difficulty.Normal,
                AIDifficulty.Hard => AICore.Difficulty.Hard,
                _ => AICore.Difficulty.Easy
            };
        }
    }
}
