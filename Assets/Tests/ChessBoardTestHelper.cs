using System.Collections.Generic;
using UnityEngine;
using static ChessPiece;

/// <summary>
/// คลาสนี้เป็นผู้ช่วยสำหรับ Unit Test เท่านั้น
/// มีหน้าที่ในการตั้งค่าสถานะกระดาน (ChessBoard) จากภายนอก
/// โดยไม่จำเป็นต้องแก้ไขโค้ด ChessBoard.cs เลย
/// </summary>
public static class ChessBoardTestHelper
{
    private static readonly Dictionary<char, (PieceType, Team)> fenCharToPiece = new Dictionary<char, (PieceType, Team)>
    {
        { 'p', (PieceType.Pawn, Team.Black) }, { 'n', (PieceType.Knight, Team.Black) },
        { 'b', (PieceType.Bishop, Team.Black) }, { 'r', (PieceType.Rook, Team.Black) },
        { 'q', (PieceType.Queen, Team.Black) }, { 'k', (PieceType.King, Team.Black) },
        { 'P', (PieceType.Pawn, Team.White) }, { 'N', (PieceType.Knight, Team.White) },
        { 'B', (PieceType.Bishop, Team.White) }, { 'R', (PieceType.Rook, Team.White) },
        { 'Q', (PieceType.Queen, Team.White) }, { 'K', (PieceType.King, Team.White) }
    };

    /// <summary>
    /// ตั้งค่ากระดานตาม FEN String
    /// </summary>
    public static void LoadFen(ChessBoard board, string fen)
    {
        board.ResetBoard(); // เรียกเมธอดสาธารณะเพื่อล้างกระดาน
        string[] parts = fen.Split(' ');
        string position = parts[0];
        int row = 7, col = 0;

        foreach (char c in position)
        {
            if (c == '/')
            {
                row--;
                col = 0;
            }
            else if (char.IsDigit(c))
            {
                col += (int)char.GetNumericValue(c);
            }
            else
            {
                var (type, team) = fenCharToPiece[c];
                board.SpawnPiece(type, team, new Vector2Int(col, row)); // เรียกเมธอดสาธารณะเพื่อวางหมาก
                col++;
            }
        }

        if (parts.Length > 1 && parts[1] == "b")
        {
            GameManager.Instance.SwitchTurn(); // สลับเป็นตาของสีดำ
        }
    }
}