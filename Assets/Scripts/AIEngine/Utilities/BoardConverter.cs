using UnityEngine;

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
        public static ChessBoardModel Convert(ChessBoard unityBoard, ChessPiece.Team currentTeam)
        {
            ChessBoardModel aiBoard = new ChessBoardModel();
            aiBoard.Board = new int[8, 8];

            // ตั้งค่าการเดินปัจจุบัน
            aiBoard.IsWhiteTurn = currentTeam == ChessPiece.Team.White;

            // กำหนดค่าหมากทั้งหมดเป็น 0 (ช่องว่าง)
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    aiBoard.Board[x, y] = 0;
                }
            }

            // กรอกข้อมูลหมาก
            foreach (var entry in unityBoard.PiecesOnBoard)
            {
                Vector2Int unityPosition = entry.Key;
                ChessPiece piece = entry.Value;

                // แปลงตำแหน่ง Unity เป็น AI
                Vector2Int aiPosition = ConvertPositionToAI(unityPosition);

                // แปลงประเภทหมาก
                int pieceValue = ConvertPieceType(piece.pieceType, piece.team);

                aiBoard.Board[aiPosition.x, aiPosition.y] = pieceValue;
            }

            return aiBoard;
        }

        /// <summary>
        /// แปลงตำแหน่งจากระบบ Unity เป็นระบบ AI
        /// </summary>
        /// <param name="unityPosition">ตำแหน่งใน Unity (x: 0-7, y: 0-7)</param>
        /// <returns>ตำแหน่งใน AI (x: 0-7, y: 0-7)</returns>
        public static Vector2Int ConvertPositionToAI(Vector2Int unityPosition)
        {
            // Unity: (0,0) = A1, (7,7) = H8
            // AI:    (0,0) = A8, (7,7) = H1

            int aiX = 7 - unityPosition.y;  // แกน Y ของ Unity กลายเป็นแกน X ของ AI (พลิกแนวตั้ง)
            int aiY = unityPosition.x;      // แกน X ของ Unity กลายเป็นแกน Y ของ AI

            return new Vector2Int(aiX, aiY);
        }

        /// <summary>
        /// แปลงตำแหน่งจากระบบ AI เป็นระบบ Unity
        /// </summary>
        /// <param name="aiPosition">ตำแหน่งใน AI (x: 0-7, y: 0-7)</param>
        /// <returns>ตำแหน่งใน Unity (x: 0-7, y: 0-7)</returns>
        public static Vector2Int ConvertPositionToUnity(Vector2Int aiPosition)
        {
            int unityX = aiPosition.y;      // แกน Y ของ AI กลายเป็นแกน X ของ Unity
            int unityY = 7 - aiPosition.x;  // แกน X ของ AI กลายเป็นแกน Y ของ Unity (พลิกแนวตั้ง)

            return new Vector2Int(unityX, unityY);
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