using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistoryMove : MonoBehaviour
{

    private Stack<HistoryMoveData> moveHistory = new Stack<HistoryMoveData>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public struct HistoryMoveData
    {

        public Vector2Int startPosition;
        public Vector2Int endPosition;
        public ChessPiece.PieceType pieceType;
        public ChessPiece.PieceType capturedPieceType;
        public bool isCastling;
        public bool isEnPassant;
        public ChessPiece.PieceType promotedTo;
        public bool isCheck;

        public HistoryMoveData(Vector2Int start, Vector2Int end, ChessPiece.PieceType piece,
                               ChessPiece.PieceType captured, bool castling, bool enPassant,
                               ChessPiece.PieceType promoted, bool check)
        {       
            startPosition = start;
            endPosition = end;
            pieceType = piece;
            capturedPieceType = captured;
            isCastling = castling;
            isEnPassant = enPassant;
            promotedTo = promoted;
            isCheck = check;
        }
    }
    // เพิ่มการเดินเข้าไปในประวัติ
    public void AddMove(Vector2Int start, Vector2Int end, ChessPiece.PieceType piece, ChessPiece.PieceType captured,
                        bool castling, bool enPassant, ChessPiece.PieceType promoted, bool check)
    {
        HistoryMoveData move = new HistoryMoveData(start, end, piece, captured, castling, enPassant, promoted, check);
        moveHistory.Push(move);

        Debug.Log($"บันทึกการเดิน: {start} -> {end}, {piece} กิน {captured}, Castling: {castling}, En Passant: {enPassant}, Promote: {promoted}, Check: {check}");
    }

  
    // คืนค่าประวัติทั้งหมด
    public Stack<HistoryMoveData> GetMoveHistory()
    {
        return moveHistory;
    }

    // เมธอดเพื่อย้อนกลับการเดินล่าสุด
    public void UndoMove()
    {

        if (moveHistory.Count > 0)
        {
            HistoryMoveData lastMove = moveHistory.Pop();
            Debug.Log($"ย้อนกลับการเดิน: {lastMove.startPosition} -> {lastMove.endPosition}, {lastMove.pieceType} กิน {lastMove.capturedPieceType}, Castling: {lastMove.isCastling}, En Passant: {lastMove.isEnPassant}, Promote: {lastMove.promotedTo}, Check: {lastMove.isCheck}");
        }
        else
        {
            Debug.Log("ไม่มีการเดินที่ต้องย้อนกลับ");
        }
    }


}
