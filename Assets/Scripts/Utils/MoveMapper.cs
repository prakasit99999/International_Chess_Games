using System; // เพิ่ม System เพื่อใช้ Math หรือ Exception ถ้าจำเป็น
using UnityEngine;

public static class MoveMapper
{
    public static MoveCreateDto ToDto(
        HistoryMove.HistoryMoveData move,
        int gameId,
        int moveNumber,
        AiPerformanceData ai
    )
    {
        bool isValidPromotion = (
            move.promotedTo == ChessPiece.PieceType.Rook ||
            move.promotedTo == ChessPiece.PieceType.Knight ||
            move.promotedTo == ChessPiece.PieceType.Bishop ||
            move.promotedTo == ChessPiece.PieceType.Queen
        );

        return new MoveCreateDto
        {
            // --- Common Fields ---
            GameId = gameId,
            MoveNumber = moveNumber,
            StartX = move.startPosition.x,
            StartY = move.startPosition.y,
            EndX = move.endPosition.x,
            EndY = move.endPosition.y,

            // ใช้ตำแหน่งที่ถูกกินจริงๆ (รองรับ En Passant)
            CapturedX = move.isCapture ? move.capturedPiecePosition.x : 0,
            CapturedY = move.isCapture ? move.capturedPiecePosition.y : 0,

            PieceType = (int)move.pieceType,

            PlayerTurn = (int)move.team - 1,

            CapturedPieceType = (move.capturedPieceType == ChessPiece.PieceType.None)
                                ? 0
                                : (int)move.capturedPieceType + 1,

            CapturedPieceTeam = (int)move.capturedPieceTeam,

            PromotedTo = isValidPromotion ? (int)move.promotedTo : 0,

            PromotedFrom = isValidPromotion ? 1 : 0,

            // ----------------------------------------------------------------

            // --- Flags ---
            IsCastling = move.isCastling,
            IsEnPassant = move.isEnPassant,
            IsCapture = move.isCapture,
            IsCheck = move.isCheck,
            IsPawnTwoStep = move.isPawnTwoStep,
            PieceHasMovedBefore = move.pieceHasMovedBefore,

            // --- AI Stats ---
            AlgorithmType = MapAlgorithmTypeToInt((ai != null) ? ai.AlgorithmType : null),

            // ✅ ใช้ double ถูกต้องแล้ว
            AiEvaluationScore = (ai != null) ? (int)ai.Score : 0,
            AiDepthSearched = (ai != null) ? ai.Depth : 0,
            AiNodesEvaluated = (ai != null) ? ai.Nodes : 0,
            MoveTimeMilliseconds = (ai != null) ? ai.MoveTimeMs : 0
        };
    }

    private static int MapAlgorithmTypeToInt(string algo)
    {
        if (string.IsNullOrEmpty(algo) || algo.ToLower() == "none") return 0; // Human

        string lowerAlgo = algo.ToLower();

        if (lowerAlgo.Contains("minimax")) return 1;    // Easy -> Minimax
        if (lowerAlgo.Contains("alpha")) return 2;      // Normal/Hard -> AlphaBeta

        return 0; // Default fallback
    }
}