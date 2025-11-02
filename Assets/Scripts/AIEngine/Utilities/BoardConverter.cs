using UnityEngine;
using static ChessPiece;

namespace AI.Utilities
{
    public static class BoardConverter
    {
        /// <summary>
        /// แปลง Unity ChessBoard → AI ChessBoardModel
        /// </summary>
        /// <param name="unityBoard">ChessBoard จาก Unity</param>
        /// <param name="team">ทีมปัจจุบันที่กำลังเดิน</param>
        /// <returns>ChessBoardModel สำหรับ AI</returns>
        public static ChessBoardModel Convert(ChessBoard unityBoard, Team aiTeam, Team currentTurn)
        {
            var model = new ChessBoardModel();
            model.IsWhiteTurn = (currentTurn == Team.White);
            Debug.Log($"[BoardConverter] Convert called with aiTeam={aiTeam}, currentTurn={currentTurn}, model.IsWhiteTurn={model.IsWhiteTurn}");

            model.Board = new int[8, 8];
            foreach (var entry in unityBoard.PiecesOnBoard)
            {
                Vector2Int unityPos = entry.Key;
                ChessPiece piece = entry.Value;
                if (piece == null) continue;

                Vector2Int aiPos = ConvertPositionToAI(unityPos);
                int pieceValue = ConvertPieceType(piece.pieceType, piece.team);

                model.Board[aiPos.y, aiPos.x] = pieceValue;

                Debug.Log($"[CONVERT] Unity {piece.team} {piece.pieceType} at {unityPos} → AI {aiPos} = {pieceValue}, IsWhiteTurn={model.IsWhiteTurn}");
            }

            model.SetFiftyMoveCounter(unityBoard.FiftyMoveCounter);
            return model;
        }

   
        /// <summary>
        /// แปลงตำแหน่ง Unity → AI
        /// </summary>
        public static Vector2Int ConvertPositionToAI(Vector2Int unityPosition)
        {
            // Unity (0,0) = A1 → AI (0,0) = A8
            return new Vector2Int(unityPosition.x, 7 - unityPosition.y);
        }

        /// <summary>
        /// แปลงตำแหน่ง AI → Unity
        /// </summary>
        public static Vector2Int ConvertPositionToUnity(Vector2Int aiPosition)
        {
            return new Vector2Int(aiPosition.x, 7 - aiPosition.y);
        }

        /// <summary>
        /// แปลงประเภทหมาก Unity → ค่า AI
        /// </summary>
        private static int ConvertPieceType(PieceType type, Team team)
        {
            int sign = (team == Team.White) ? 1 : -1;

            return type switch
            {
                PieceType.Pawn => 1 * sign,
                PieceType.Knight => 2 * sign,
                PieceType.Bishop => 3 * sign,
                PieceType.Rook => 4 * sign,
                PieceType.Queen => 5 * sign,
                PieceType.King => 6 * sign,
                _ => 0
            };
        }
    }
}
