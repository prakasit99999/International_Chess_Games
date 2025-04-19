using static HistoryMove;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class UndoMove : MonoBehaviour
{
    public GameObject piecePrefab;
    public Stack<HistoryMove.HistoryMoveData> moveHistory; 



    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UndoLastMove(Dictionary<Vector2Int, ChessPiece> piecesOnBoard, Transform boardParent)
    {
        if (moveHistory.Count > 0)
        {
            HistoryMoveData lastMove = moveHistory.Pop();
            Vector2Int start = lastMove.startPosition;
            Vector2Int end = lastMove.endPosition;

            if (piecesOnBoard.TryGetValue(end, out ChessPiece movedPiece))
            {
                // นำตัวหมากกลับไปที่ตำแหน่งเริ่มต้น
                movedPiece.transform.position = new Vector3(start.x, start.y, 0);
                piecesOnBoard.Remove(end);
                piecesOnBoard[start] = movedPiece;

                Debug.Log($"🔄 ย้อนกลับ {movedPiece.pieceType} จาก {end} -> {start}");
            }

            // นำตัวหมากที่ถูกกินคืนมา ถ้ามี
            if (lastMove.capturedPieceType != ChessPiece.PieceType.Pawn) // Assuming Pawn is the default piece type
            {
                GameObject capturedPieceObj = InstantiatePiece(lastMove.capturedPieceType, end, boardParent);
                ChessPiece capturedPiece = capturedPieceObj.GetComponent<ChessPiece>();
                piecesOnBoard[end] = capturedPiece;

                Debug.Log($"♟️ คืนค่า {capturedPiece.pieceType} ที่ {end}");
            }

            // ตรวจสอบกรณีพิเศษ (Castling, Promote, En Passant)
            if (lastMove.isCastling)
            {
                UndoCastling(lastMove, piecesOnBoard);
            }
            else if (lastMove.promotedTo != ChessPiece.PieceType.Pawn) // Assuming Pawn is the default piece type
            {
                UndoPromotion(lastMove, piecesOnBoard);
            }
            else if (lastMove.isEnPassant)
            {
                UndoEnPassant(lastMove, piecesOnBoard, boardParent);
            }
        }
        else
        {
            Debug.Log("⚠️ ไม่มีประวัติการเดินให้ย้อนกลับ");
        }
    }

    private GameObject InstantiatePiece(ChessPiece.PieceType pieceType, Vector2Int position, Transform parent)
    {
        GameObject pieceObj = Instantiate(piecePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity, parent);
        ChessPiece newPiece = pieceObj.GetComponent<ChessPiece>();
        newPiece.pieceType = pieceType;
        return pieceObj;
    }

    private void UndoPromotion(HistoryMoveData lastMove, Dictionary<Vector2Int, ChessPiece> piecesOnBoard)
    {
        if (piecesOnBoard.TryGetValue(lastMove.endPosition, out ChessPiece promotedPiece))
        {
            promotedPiece.pieceType = ChessPiece.PieceType.Pawn; // กลับเป็นเบี้ย
            Debug.Log($"🔄 ย้อนกลับการเลื่อนขั้นของ {promotedPiece.pieceType} ที่ {lastMove.endPosition}");
        }
    }

    private void UndoCastling(HistoryMoveData lastMove, Dictionary<Vector2Int, ChessPiece> piecesOnBoard)
    {
        int rookStartX = lastMove.endPosition.x > 4 ? 7 : 0; // หาตำแหน่งเดิมของเรือ
        int rookEndX = lastMove.endPosition.x > 4 ? 5 : 3;

        Vector2Int rookStartPos = new Vector2Int(rookStartX, lastMove.endPosition.y);
        Vector2Int rookEndPos = new Vector2Int(rookEndX, lastMove.endPosition.y);

        if (piecesOnBoard.TryGetValue(rookEndPos, out ChessPiece rook))
        {
            piecesOnBoard.Remove(rookEndPos);
            rook.transform.position = new Vector3(rookStartPos.x, rookStartPos.y, 0);
            piecesOnBoard[rookStartPos] = rook;
            Debug.Log($"🏰 ย้อนกลับ Castling: คืนค่าเรือจาก {rookEndPos} -> {rookStartPos}");
        }
    }

    private void UndoEnPassant(HistoryMoveData lastMove, Dictionary<Vector2Int, ChessPiece> piecesOnBoard, Transform boardParent)
    {
        Vector2Int capturedPawnPosition = new Vector2Int(lastMove.endPosition.x, lastMove.startPosition.y);
        if (piecesOnBoard.TryGetValue(capturedPawnPosition, out ChessPiece capturedPawn))
        {
            piecesOnBoard.Remove(capturedPawnPosition);
            Destroy(capturedPawn.gameObject);
        }

        if (piecesOnBoard.TryGetValue(lastMove.endPosition, out ChessPiece movedPawn))
        {
            movedPawn.transform.position = new Vector3(lastMove.startPosition.x, lastMove.startPosition.y, 0);
            piecesOnBoard.Remove(lastMove.endPosition);
            piecesOnBoard[lastMove.startPosition] = movedPawn;
        }

        Debug.Log($"🔄 ย้อนกลับ En Passant: คืนค่าหมากจาก {lastMove.endPosition} -> {lastMove.startPosition}");
    }
}
