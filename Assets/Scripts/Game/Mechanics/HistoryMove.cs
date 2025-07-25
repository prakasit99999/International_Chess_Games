using System.Collections.Generic;
using UnityEngine;

public class HistoryMove : MonoBehaviour
{

    private Stack<HistoryMoveData> moveHistory = new Stack<HistoryMoveData>();

    public struct HistoryMoveData
    {

        public Vector2Int startPosition; //ตำแหน่งเริ่มต้น
        public Vector2Int endPosition; //ตำแหน่งสุดท้าย
        public Vector2Int capturedPiecePosition;
        public Vector2Int promotedPosition;
        public Vector2Int? previousEnPassantTarget;

        public ChessPiece.PieceType pieceType; //ประเภทของเบี้ยที่เดิน
        public ChessPiece.PieceType capturedPieceType; //ประเภทของเบี้ยที่ถูกกิน
        public ChessPiece.Team capturedPieceTeam; //ทีมของเบี้ยที่ถูกกิน
        public ChessPiece.PieceType promotedTo;
        public ChessPiece.PieceType promotedFrom;
        public ChessPiece.Team team;

        public bool isCastling; //การเดินแบบ Castling
        public bool isEnPassant;
        public bool isCapture; //การเดินที่ทำให้เกิดการกินเบี้ย
        public bool isCheck;//การเดินที่ทำให้เกิด Check
        public bool isPawnTwoStep;
        public bool pieceHasMovedBefore;

        public int fiftyMoveCounter;
        public HistoryMoveData(Vector2Int start, Vector2Int end, ChessPiece.PieceType piece,
                               ChessPiece.PieceType captured, ChessPiece.Team capturedTeam,
                               bool castling, bool enPassant, ChessPiece.PieceType promoted,
                               bool check, bool isPawnTwoStep, bool pieceHasMovedBefore,
                               Vector2Int capturedPiecePosition, ChessPiece.PieceType promotedFrom,
                               Vector2Int promotedPosition, Vector2Int? previousEnPassantTarget, bool isCapture,
                               ChessPiece.Team team, int fiftyMoveCounter // ✅ เพิ่มตรงนี้
        )
        {
            startPosition = start;
            endPosition = end;
            pieceType = piece;
            capturedPieceType = captured;
            capturedPieceTeam = capturedTeam;
            isCastling = castling;
            isEnPassant = enPassant;
            promotedTo = promoted;
            isCheck = check;
            this.isCapture = isCapture;
            this.isPawnTwoStep = isPawnTwoStep;
            this.pieceHasMovedBefore = pieceHasMovedBefore;
            this.capturedPiecePosition = capturedPiecePosition;
            this.promotedFrom = promotedFrom;
            this.promotedPosition = promotedPosition;
            this.previousEnPassantTarget = previousEnPassantTarget;
            this.team = team;
            this.fiftyMoveCounter = fiftyMoveCounter;
        }

    }

    // เพิ่มการเดินเข้าไปในประวัติ
    public void AddMove(
      Vector2Int start, Vector2Int end,
      ChessPiece.PieceType piece,
      ChessPiece.PieceType captured,
      ChessPiece.Team capturedTeam,
      bool castling, bool enPassant,
      bool check, bool isPawnTwoStep,
      bool isCapture, bool pieceHasMovedBefore,
      ChessPiece.PieceType promoted,
      Vector2Int capturedPiecePosition,
      ChessPiece.PieceType promotedFrom,
      Vector2Int promotedPosition,
      Vector2Int? previousEnPassantTarget,
      ChessPiece.Team team,
      int fiftyMoveCounter // ✅ เพิ่มตรงนี้
  )
    {
        HistoryMoveData move = new HistoryMoveData(
            start, end, piece, captured, capturedTeam,
            castling, enPassant, promoted, check,
            isPawnTwoStep, pieceHasMovedBefore, capturedPiecePosition,
            promotedFrom, promotedPosition, previousEnPassantTarget,
            isCapture, team, fiftyMoveCounter 
        );

        moveHistory.Push(move);

        Debug.Log($"Move created: startPosition={move.startPosition}, endPosition={move.endPosition}, pieceType={move.pieceType}, " +
                    $"capturedPieceType={move.capturedPieceType}, capturedPieceTeam={move.capturedPieceTeam}, isCastling={move.isCastling}, " +
                    $"isEnPassant={move.isEnPassant}, promotedTo={move.promotedTo}, isCheck={move.isCheck}, isPawnTwoStep={move.isPawnTwoStep}, " +
                    $" pieceHasMovedBefore={move.pieceHasMovedBefore},capturedPiecePosition={move.capturedPiecePosition}, promotedFrom={move.promotedFrom}," +
                     $"promotedPosition={move.promotedPosition}, isCapture={move.isCapture},team={move.team},fiftyMoveCounter={fiftyMoveCounter} "
                     );


    }

    // คืนค่าประวัติทั้งหมด
    public Stack<HistoryMoveData> GetMoveHistory()
    {
        return moveHistory;
    }

    // เมธอดเพื่อย้อนกลับการเดินล่าสุด
    public void UndoMove()
    {
        if (moveHistory.Count == 0)
        {
            Debug.Log("ไม่มีการเดินที่ต้องย้อนกลับ");
            return;
        }
        if (moveHistory.Count > 0)
        {
            HistoryMoveData lastMove = moveHistory.Pop();
            Debug.Log($"ย้อนกลับการเดิน: {lastMove.startPosition} -> " +
                $"{lastMove.endPosition}, {lastMove.pieceType} " +
                $"กิน {lastMove.capturedPieceType}, Castling: " +
                $"{lastMove.isCastling}, En Passant: {lastMove.isEnPassant}, " +
                $"Promote: {lastMove.promotedTo}, Check: {lastMove.isCheck}" +
                $"\n"
                );

        }
        else
        {
            Debug.Log("ไม่มีการเดินที่ต้องย้อนกลับ");
        }
    }

    public void ClearHistory()
    {
        moveHistory.Clear();
        Debug.Log("ประวัติการเดินถูกล้างเรียบร้อยแล้ว");
    }

}
