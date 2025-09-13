using UnityEngine;
using static ChessPiece;

namespace AI.Utilities
{
    public static class BoardConverter
    {
        /// <summary>
        /// แปลง Unity ChessBoard เป็น AI ChessBoardModel
        /// </summary>
        /// <param name="unityBoard">ChessBoard จาก Unity</param>
        /// <param name="currentTeam">ทีมปัจจุบันที่กำลังเดิน</param>
        /// <returns>ChessBoardModel สำหรับ AI</returns>
        public static ChessBoardModel Convert(ChessBoard unityBoard, Team team)
        {
            var model = new ChessBoardModel();
            model.IsWhiteTurn = (team == Team.White);
            model.Board = new int[8, 8];

            foreach (var entry in unityBoard.PiecesOnBoard)
            {
                Vector2Int unityPos = entry.Key;
                ChessPiece piece = entry.Value;
                if (piece == null) continue;
                // แปลงตำแหน่ง Unity → AI
                Vector2Int aiPos = ConvertPositionToAI(unityPos);

                // แปลงชิ้นหมาก
                int pieceValue = ConvertPieceType(piece.pieceType, piece.team);
                Debug.Log($"[CONVERT] Unity {piece.team} {piece.pieceType} at {unityPos} → AI {aiPos} = {pieceValue}");


                model.Board[aiPos.x, aiPos.y] = pieceValue;
            }

            // ✅ ใช้ Setter ใน Model แทนการ set โดยตรง
            model.SetFiftyMoveCounter(unityBoard.FiftyMoveCounter);

            return model;
        }


        /// <summary>
        /// แปลงตำแหน่งจากระบบ Unity เป็นระบบ AI
        /// </summary>
        /// <param name="unityPosition">ตำแหน่งใน Unity (x: 0-7, y: 0-7)</param>
        /// <returns>ตำแหน่งใน AI (x: 0-7, y: 0-7)</returns>
        public static Vector2Int ConvertPositionToAI(Vector2Int unityPosition)
        {
            // Unity (0,0) = A1, AI (0,0) = A8
            return new Vector2Int(unityPosition.x, 7 - unityPosition.y);
        }

        /// <summary>
        /// แปลงตำแหน่งจากระบบ AI เป็นระบบ Unity
        /// </summary>
        /// <param name="aiPosition">ตำแหน่งใน AI (x: 0-7, y: 0-7)</param>
        /// <returns>ตำแหน่งใน Unity (x: 0-7, y: 0-7)</returns>
        public static Vector2Int ConvertPositionToUnity(Vector2Int aiPosition)
        {
            return new Vector2Int(aiPosition.x, 7 - aiPosition.y);
        }


        /// <summary>
        /// แปลงประเภทหมากจาก Unity เป็นค่าตัวเลขสำหรับ AI
        /// </summary>
        private static int ConvertPieceType(ChessPiece.PieceType type, ChessPiece.Team team)
        {
            int sign = (team == ChessPiece.Team.White) ? 1 : -1;

            return type switch
            {
                ChessPiece.PieceType.Pawn => 1 * sign,
                ChessPiece.PieceType.Rook => 4 * sign,
                ChessPiece.PieceType.Knight => 2 * sign,
                ChessPiece.PieceType.Bishop => 3 * sign,
                ChessPiece.PieceType.Queen => 5 * sign,
                ChessPiece.PieceType.King => 6 * sign,
                _ => 0
            };
        }
    }
}