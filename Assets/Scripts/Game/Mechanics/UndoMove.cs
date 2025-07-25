using System.Collections.Generic;
using UnityEngine;
using static HistoryMove;

public class UndoMove : MonoBehaviour
{
    public static UndoMove Instance; // Singleton Pattern
    public GameObject piecePrefab;
    public Stack<HistoryMove.HistoryMoveData> moveHistory;

    private ChessBoard chessBoard;
    private HistoryMove historyMove;
    public HistoryMove HistoryMove;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            chessBoard = ChessBoard.Instance; // ใช้ Singleton
            historyMove = FindObjectOfType<HistoryMove>();
            chessBoard = FindObjectOfType<ChessBoard>();

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UndoLastMove()
    {

        ChessPiece movedPiece = null;
        if (PauseManager.isPaused) return;
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentMode() == GameManager.GameModes.AIVsAI ) {
            return;
        }
        if (historyMove.GetMoveHistory().Count == 0) return;

        HistoryMove.HistoryMoveData lastMove = historyMove.GetMoveHistory().Pop();

        // ✅ ย้อนค่า 50-move counter
        chessBoard.SetFiftyMoveCounter(lastMove.fiftyMoveCounter);

        if (lastMove.fiftyMoveCounter == 0)
            GameManager.Instance.ResetFiftyMoveUI();
        else
            GameManager.Instance.UpdateFiftyMoveCounter(lastMove.fiftyMoveCounter);

        Vector2Int destinationPosition = lastMove.endPosition;
        Vector2Int originalPosition = lastMove.startPosition;

        // ✅ Undo กรณีพิเศษก่อน
        if (lastMove.isEnPassant)
        {
            UndoEnPassant(lastMove, chessBoard.GetPiecesOnBoard(), chessBoard.transform);
            Debug.Log($"🔄 ย้อน En Passant: {originalPosition} → {destinationPosition} (คืนเบี้ยที่ถูกกินที่ {lastMove.capturedPiecePosition})");
        }
        else if (lastMove.isCastling)
        {
            UndoCastling(lastMove, chessBoard.GetPiecesOnBoard(), chessBoard.transform);
        }
        else if ((movedPiece = UndoPromotion(lastMove, chessBoard.GetPiecesOnBoard())) == null)
        {
            // ❌ ไม่ใช่โปรโมท → เอาหมากเดิมกลับ
            if (chessBoard.GetPiecesOnBoard().TryGetValue(destinationPosition, out movedPiece))
            {
                chessBoard.GetPiecesOnBoard().Remove(destinationPosition);
                movedPiece.SetPosition(originalPosition.x, originalPosition.y);
                movedPiece.HasMoved = lastMove.pieceHasMovedBefore;
                chessBoard.GetPiecesOnBoard()[originalPosition] = movedPiece;
            }
            else
            {
                Debug.LogWarning($"❌ ไม่พบหมากที่ {destinationPosition} ตอน Undo");
                return;
            }
        }
        else
        {
            // ✅ โปรโมท → คืนเบี้ยเดิม
            movedPiece.SetPosition(originalPosition.x, originalPosition.y);
            movedPiece.HasMoved = lastMove.pieceHasMovedBefore;
            chessBoard.GetPiecesOnBoard()[originalPosition] = movedPiece;
        }

        // ✅ คืนหมากที่ถูกกิน
        if (lastMove.isCapture && lastMove.capturedPieceTeam != ChessPiece.Team.None)
        {
            chessBoard.SpawnPiece(
                lastMove.capturedPieceType,
                lastMove.capturedPieceTeam,
                lastMove.capturedPiecePosition
            );
            Debug.Log($"✅ คืนหมากที่ถูกกิน: {lastMove.capturedPieceType} ที่ {lastMove.capturedPiecePosition}");
        }

        // ✅ รีเซ็ต En Passant target
        chessBoard.SetEnPassantTarget(lastMove.previousEnPassantTarget);

        // ✅ ล้างตัวเลือก และสลับตา
        chessBoard.SetselectedPiece(null);
        GameManager.Instance.SwitchTurn(true);

        Debug.Log($"🔙 Undo การเดิน: {lastMove.startPosition} → {lastMove.endPosition}");
    }

    private ChessPiece UndoPromotion(HistoryMoveData lastMove, Dictionary<Vector2Int, ChessPiece> piecesOnBoard)
    {
        // ตรวจสอบว่ามีการเลื่อนขั้นหรือไม่
        if (lastMove.promotedFrom != ChessPiece.PieceType.Pawn ||
             lastMove.promotedTo == ChessPiece.PieceType.Pawn)
        {
            return null;
        }


        // ตำแหน่งที่เกิดการเลื่อนขั้น (ตำแหน่งปลายทางของเบี้ย)
        Vector2Int promoPos = lastMove.endPosition;

        if (piecesOnBoard.TryGetValue(promoPos, out ChessPiece promotedPiece))
        {
            // ลบตัวหมากที่ถูกเลื่อนขั้น
            Destroy(promotedPiece.gameObject);
            piecesOnBoard.Remove(promoPos);

            // สร้างเบี้ยกลับมาที่ตำแหน่งเริ่มต้นก่อนเลื่อนขั้น (startPosition)
            ChessPiece newPawn = chessBoard.SpawnPiece(
                ChessPiece.PieceType.Pawn,
                lastMove.team, // ใช้ทีมเดิมจากประวัติ
                lastMove.startPosition // วางเบี้ยที่ตำแหน่งเริ่มต้น
            );

            // คืนค่าสถานะ HasMoved
            newPawn.HasMoved = lastMove.pieceHasMovedBefore;

            Debug.Log($"✅ ย้อนเลื่อนขั้น: สร้างเบี้ยกลับที่ {lastMove.startPosition}");
            return newPawn;
        }
        else
        {
            Debug.LogWarning($"❌ ไม่พบหมากที่ตำแหน่ง {promoPos} สำหรับย้อนเลื่อนขั้น");
            return null;
        }
    }

    private void UndoCastling(HistoryMove.HistoryMoveData lastMove, Dictionary<Vector2Int, ChessPiece> piecesOnBoard, Transform boardParent)
    {
        int row = lastMove.endPosition.y;

        // ตำแหน่ง Rook
        int rookStartX = lastMove.endPosition.x > 4 ? 7 : 0;
        int rookEndX = lastMove.endPosition.x > 4 ? 5 : 3;
        Vector2Int rookStartPos = new Vector2Int(rookStartX, row);
        Vector2Int rookEndPos = new Vector2Int(rookEndX, row);

        // ✅ ย้าย King กลับ
        Vector2Int kingStartPos = lastMove.startPosition;
        Vector2Int kingEndPos = lastMove.endPosition;

        if (piecesOnBoard.TryGetValue(kingEndPos, out ChessPiece king))
        {
            piecesOnBoard.Remove(kingEndPos);
            king.SetPosition(kingStartPos.x, kingStartPos.y);
            king.HasMoved = false;
            piecesOnBoard[kingStartPos] = king;
        }

        // ✅ ย้าย Rook กลับ
        if (piecesOnBoard.TryGetValue(rookEndPos, out ChessPiece rook))
        {
            piecesOnBoard.Remove(rookEndPos);
            rook.SetPosition(rookStartPos.x, rookStartPos.y);
            rook.HasMoved = false;
            piecesOnBoard[rookStartPos] = rook;
        }
        // ✅ คืนสิทธิ์ให้ Castling ได้อีก
        if (lastMove.team == ChessPiece.Team.White)
        {
            if (lastMove.endPosition.x > 4)
                chessBoard.SetCanCastleKingSide(ChessPiece.Team.White, true);
            else
                chessBoard.SetCanCastleQueenSide(ChessPiece.Team.White, true);
        }
        else if (lastMove.team == ChessPiece.Team.Black)
        {
            if (lastMove.endPosition.x > 4)
                chessBoard.SetCanCastleKingSide(ChessPiece.Team.Black, true);
            else
                chessBoard.SetCanCastleQueenSide(ChessPiece.Team.Black, true);
        }


        Debug.Log($"🔙 ย้อนกลับ Castling: King {kingEndPos} → {kingStartPos}, Rook {rookEndPos} → {rookStartPos}");
    }

    private void UndoEnPassant(HistoryMoveData lastMove, Dictionary<Vector2Int, ChessPiece> piecesOnBoard, Transform boardParent)
    {
        Vector2Int capturedPawnPosition = lastMove.capturedPiecePosition;
        Vector2Int movedPawnOldPos = lastMove.startPosition;
        Vector2Int movedPawnNewPos = lastMove.endPosition;

        // 🔁 ย้ายหมากที่กินกลับ
        if (piecesOnBoard.TryGetValue(movedPawnNewPos, out ChessPiece movedPawn))
        {
            piecesOnBoard.Remove(movedPawnNewPos);
            movedPawn.SetPosition(movedPawnOldPos.x, movedPawnOldPos.y);
            movedPawn.HasMoved = lastMove.pieceHasMovedBefore;
            piecesOnBoard[movedPawnOldPos] = movedPawn;
        }

        // ✅ คืนหมากที่ถูกกิน
        ChessPiece restoredPawn = ChessBoard.Instance.SpawnPiece(
            lastMove.capturedPieceType,
            lastMove.capturedPieceTeam,
            capturedPawnPosition
        );

        Debug.Log($"🔄 ย้อนกลับ En Passant: {movedPawnNewPos} → {movedPawnOldPos} (คืนเบี้ยที่ถูกกินที่ {capturedPawnPosition})");
    }

}
