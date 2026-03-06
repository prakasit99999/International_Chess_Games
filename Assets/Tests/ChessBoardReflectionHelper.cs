using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using static ChessPiece;

/// <summary>
/// คลาสผู้ช่วยสำหรับ Unit Test ที่ใช้ Reflection
/// เพื่อเรียกใช้เมธอด private ของ ChessBoard โดยไม่ต้องแก้ไขไฟล์นั้น
/// </summary>
public static class ChessBoardReflectionHelper
{
    /// <summary>
    /// ใช้ Reflection เพื่อสั่งเดินหมาก
    /// </summary>
    public static void MovePiece(ChessBoard board, Vector2Int from, Vector2Int to)
    {
        // 1. หาเมธอด private "SelectPiece"
        var selectPieceMethod = typeof(ChessBoard).GetMethod("SelectPiece", BindingFlags.NonPublic | BindingFlags.Instance);
        if (selectPieceMethod == null)
        {
            Debug.LogError("ไม่พบเมธอด private 'SelectPiece' ใน ChessBoard.cs");
            return;
        }

        // 2. หาเมธอด private "MoveSelectedPiece"
        var moveSelectedPieceMethod = typeof(ChessBoard).GetMethod("MoveSelectedPiece", BindingFlags.NonPublic | BindingFlags.Instance);
        if (moveSelectedPieceMethod == null)
        {
            Debug.LogError("ไม่พบเมธอด private 'MoveSelectedPiece' ใน ChessBoard.cs");
            return;
        }

        // 3. ทำการเดินหมาก
        if (board.PiecesOnBoard.TryGetValue(from, out ChessPiece piece))
        {
            selectPieceMethod.Invoke(board, new object[] { piece });
            moveSelectedPieceMethod.Invoke(board, new object[] { to });
        }
        else
        {
            Debug.LogError($"[Test Helper] ไม่พบหมากที่ตำแหน่ง {from}");
        }
    }

    /// <summary>
    /// รวบรวมตาเดินที่ถูกกฎหมายทั้งหมด (เรียกใช้เมธอด public ของ ChessPiece)
    /// </summary>
    public static List<Vector2Int> GetAllLegalMovesForTeam(ChessBoard board, Team team)
    {
        var allMoves = new List<Vector2Int>();
        foreach (var piece in board.PiecesOnBoard.Values)
        {
            if (piece != null && piece.team == team)
            {
                // GetValidMoves() เป็น public อยู่แล้วใน ChessPiece.cs จึงเรียกใช้ได้โดยตรง
                allMoves.AddRange(piece.GetValidMoves());
            }
        }
        return allMoves;
    }
}