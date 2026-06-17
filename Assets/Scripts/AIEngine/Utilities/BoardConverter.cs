using UnityEngine;
using static ChessPiece;

namespace AI.Utilities
{
    public static class BoardConverter
    {
        /// แปลง Unity ChessBoard → AI ChessBoardModel
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

            // ✅ Sync Castling Rights
            SyncCastlingFlags(unityBoard, model);

            // ✅ Sync En Passant Target
            var ep = unityBoard.GetEnPassantTarget();
            if (ep.HasValue)
            {
                // Convert Unity Pos -> AI Pos
                Vector2Int aiPos = ConvertPositionToAI(ep.Value);
                model.EnPassantTarget = new Square(aiPos.x, aiPos.y);
            }
            else
            {
                model.EnPassantTarget = null;
            }

            return model;
        }


        /// แปลงตำแหน่ง Unity → AI
        public static Vector2Int ConvertPositionToAI(Vector2Int unityPosition)
        {
            // Unity (0,0) = A1 → AI (0,0) = A8
            return new Vector2Int(unityPosition.x, 7 - unityPosition.y);
        }

        /// แปลงตำแหน่ง AI → Unity
        public static Vector2Int ConvertPositionToUnity(Vector2Int aiPosition)
        {
            return new Vector2Int(aiPosition.x, 7 - aiPosition.y);
        }

        /// แปลงประเภทหมาก Unity → ค่า AI
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

        private static void SyncCastlingFlags(ChessBoard unityBoard, ChessBoardModel model)
        {
            // หมายเหตุ: ใน AI Model, "RookMoved/KingMoved = true" หมายถึง "หมดสิทธิ์เข้าป้อมทางนั้น"
            bool whiteKingMoved = IsKingMoved(unityBoard, Team.White, new Vector2Int(4, 0));
            bool blackKingMoved = IsKingMoved(unityBoard, Team.Black, new Vector2Int(4, 7));

            bool whiteRookKingSideMoved = IsRookMoved(unityBoard, Team.White, new Vector2Int(7, 0));
            bool whiteRookQueenSideMoved = IsRookMoved(unityBoard, Team.White, new Vector2Int(0, 0));
            bool blackRookKingSideMoved = IsRookMoved(unityBoard, Team.Black, new Vector2Int(7, 7));
            bool blackRookQueenSideMoved = IsRookMoved(unityBoard, Team.Black, new Vector2Int(0, 7));

            model.WhiteKingMoved = whiteKingMoved || (!unityBoard.WhiteCanCastleKingSide && !unityBoard.WhiteCanCastleQueenSide);
            model.BlackKingMoved = blackKingMoved || (!unityBoard.BlackCanCastleKingSide && !unityBoard.BlackCanCastleQueenSide);
            model.WhiteRookKingSideMoved = whiteRookKingSideMoved || !unityBoard.WhiteCanCastleKingSide;
            model.WhiteRookQueenSideMoved = whiteRookQueenSideMoved || !unityBoard.WhiteCanCastleQueenSide;
            model.BlackRookKingSideMoved = blackRookKingSideMoved || !unityBoard.BlackCanCastleKingSide;
            model.BlackRookQueenSideMoved = blackRookQueenSideMoved || !unityBoard.BlackCanCastleQueenSide;
        }

        private static bool IsKingMoved(ChessBoard unityBoard, Team team, Vector2Int expectedPosition)
        {
            if (unityBoard.PiecesOnBoard.TryGetValue(expectedPosition, out ChessPiece piece)
                && piece != null
                && piece.team == team
                && piece.pieceType == PieceType.King)
            {
                return piece.HasMoved;
            }

            return true;
        }

        private static bool IsRookMoved(ChessBoard unityBoard, Team team, Vector2Int expectedPosition)
        {
            if (unityBoard.PiecesOnBoard.TryGetValue(expectedPosition, out ChessPiece piece)
                && piece != null
                && piece.team == team
                && piece.pieceType == PieceType.Rook)
            {
                return piece.HasMoved;
            }

            return true;
        }
    }
}
