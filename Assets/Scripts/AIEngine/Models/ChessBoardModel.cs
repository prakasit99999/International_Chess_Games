using AIEngine.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class ChessBoardModel
{
    // ======== Properties ========
    public int[,] Board { get; set; }
    public bool IsWhiteTurn { get; set; }
    //เก็บประวัติตำแหน่ง
    private List<string> _positionHistory = new List<string>();
    // นับจำนวนครั้งที่แต่ละ position-key ปรากฏ
    private Dictionary<string, int> _positionCounts = new Dictionary<string, int>();
    // Castling Flags
    public bool WhiteKingMoved { get; set; }
    public bool WhiteRookKingSideMoved { get; set; }
    public bool WhiteRookQueenSideMoved { get; private set; }
    public bool BlackKingMoved { get; set; }
    public bool BlackRookKingSideMoved { get; private set; }
    public bool BlackRookQueenSideMoved { get; set; }

    // En Passant
    public Square? EnPassantTarget { get; set; }

    // Draw Conditions
    public List<string> PositionHistory { get; } = new List<string>();
    public int FiftyMoveCounter { get; private set; }
    public int CurrentTurn { get; internal set; }

    // ======== Constructor ========
    public ChessBoardModel()
    {
        Board = new int[8, 8];
        IsWhiteTurn = true;
        InitializeBoard();
    }

    public int RepetitionCount
    {
        get
        {
            var key = GetPositionKey();
            return _positionCounts.TryGetValue(key, out var c) ? c : 0;
        }
    }

    public void PopLastPosition()
    {
        if (_positionHistory == null || _positionHistory.Count == 0) return;

        var last = _positionHistory[_positionHistory.Count - 1];
        if (_positionCounts.TryGetValue(last, out var c))
        {
            if (c <= 1) _positionCounts.Remove(last);
            else _positionCounts[last] = c - 1;
        }
        _positionHistory.RemoveAt(_positionHistory.Count - 1);
    }

    private string GetPositionKey()
    {
        // ถ้า SerializeBoard() แสดงกระดานอย่างเดียว ให้เพิ่ม side-to-move
        // ถ้า SerializeBoard() รวมทุกอย่างแล้ว (castling, en-passant, turn) ก็ไม่จำเป็นเพิ่มอะไร
        string baseKey = SerializeBoard(); // ใช้เมธอดที่คุณมีอยู่แล้ว
        string turnKey = IsWhiteTurn ? "W" : "B"; // ปรับชื่อ property ตามที่มีจริง
        return $"{baseKey}|{turnKey}";
    }

    public void InitializePositionHistory()
    {
        _positionHistory = new List<string>();
        _positionCounts = new Dictionary<string, int>();
        // บันทึกสถานะเริ่มต้นด้วย
        RecordPosition();
    }

    public void RecordPosition()
    {
        var key = GetPositionKey();
        _positionHistory.Add(key);
        if (_positionCounts.TryGetValue(key, out var c))
            _positionCounts[key] = c + 1;
        else
            _positionCounts[key] = 1;
    }

    // ======== Initialize Board ========
    private void InitializeBoard()
    {
        // White Pieces (แถว 1 และ 2 ใน chess notation)
        Board[7, 0] = 4; // Rook (a1)
        Board[7, 1] = 2; // Knight (b1)
        Board[7, 2] = 3; // Bishop (c1)
        Board[7, 3] = 5; // Queen (d1)
        Board[7, 4] = 6; // King (e1)
        Board[7, 5] = 3; // Bishop (f1)
        Board[7, 6] = 2; // Knight (g1)
        Board[7, 7] = 4; // Rook (h1)

        // White Pawns (แถว 2 ใน chess notation)
        for (int i = 0; i < 8; i++)
            Board[6, i] = 1;

        // Black Pieces (แถว 8 และ 7 ใน chess notation)
        Board[0, 0] = -4; // Rook (a8)
        Board[0, 1] = -2; // Knight (b8)
        Board[0, 2] = -3; // Bishop (c8)
        Board[0, 3] = -5; // Queen (d8)
        Board[0, 4] = -6; // King (e8)
        Board[0, 5] = -3; // Bishop (f8)
        Board[0, 6] = -2; // Knight (g8)
        Board[0, 7] = -4; // Rook (h8)

        // Black Pawns (แถว 7 ใน chess notation)
        for (int i = 0; i < 8; i++)
            Board[1, i] = -1;
    }

    // ======== Make Move ========
    public void MakeMoveUnsafe(MoveModel move)
    {
        try
        {
            int piece = Board[move.FromX, move.FromY];
            int targetPiece = Board[move.ToX, move.ToY];

            // ======== อัปเดตกฎ 50 การเดิน ========
            if (Math.Abs(piece) == 1 || targetPiece != 0)
                FiftyMoveCounter = 0;
            else
                FiftyMoveCounter++;

         
            // ======== บันทึกประวัติกระดาน ========
            string currentPosition = SerializeBoard();
            PositionHistory.Add(currentPosition);

            // ======== ตรวจจับ En Passant target ก่อนทำ move ========
            UpdateEnPassantTarget(move, piece);

            // ======== ตรวจจับ Castling และย้าย Rook ถ้าเกี่ยวข้อง ========
            bool isCastling = HandleCastling(move, piece);

            // ======== ย้ายหมากจริงบนกระดาน ========
            Board[move.ToX, move.ToY] = piece;
            Board[move.FromX, move.FromY] = 0;

            // ======== จัดการ En Passant หลังจากย้ายเสร็จ ========
            HandleEnPassantCapture(move, piece);

            // ======== การ Promote เบี้ย ========
            if (move.PromotionPiece != 0)
                Board[move.ToX, move.ToY] = move.PromotionPiece;

            // ======== อัปเดตสถานะ King และ Rook (เฉพาะไม่ใช่ Castling) ========
            if (!isCastling)
            {
                if (Math.Abs(piece) == 6) // King เคลื่อนที่
                {
                    if (piece > 0) WhiteKingMoved = true;
                    else BlackKingMoved = true;
                }
                else if (Math.Abs(piece) == 4) // Rook เคลื่อนที่
                {
                    if (piece > 0)
                    {
                        if (move.FromX == 7 && move.FromY == 7)
                            WhiteRookKingSideMoved = true;
                        else if (move.FromX == 7 && move.FromY == 0)
                            WhiteRookQueenSideMoved = true;
                    }
                    else
                    {
                        if (move.FromX == 0 && move.FromY == 7)
                            BlackRookKingSideMoved = true;
                        else if (move.FromX == 0 && move.FromY == 0)
                            BlackRookQueenSideMoved = true;
                    }
                }
            }

            // ======== สลับตาเล่น ========
            IsWhiteTurn = !IsWhiteTurn;
            RecordPosition();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MakeMoveUnsafe: {ex.Message}");
            throw;
        }
    }

    public void MakeMove(MoveModel move)
    {
        // ===== ตรวจสอบ legality ก่อนทำ move =====
        var legalMoves = MoveGenerator.GenerateMoves(this);
        bool isLegal = legalMoves.Any(m =>
            m.FromX == move.FromX &&
            m.FromY == move.FromY &&
            m.ToX == move.ToX &&
            m.ToY == move.ToY &&
            m.PromotionPiece == move.PromotionPiece);
        if (!isLegal)
            throw new InvalidOperationException("Illegal move detected!");

        MakeMoveUnsafe(move);

    }

    // ======== Clone Board ========
    public ChessBoardModel Clone()
    {
        ChessBoardModel newBoard = new ChessBoardModel();
        newBoard.Board = (int[,])this.Board.Clone(); // Deep copy of the board array
        newBoard.IsWhiteTurn = this.IsWhiteTurn;

        //คัดลอกประวัติตำแหน่ง
        newBoard._positionHistory = new List<string>(this._positionHistory);
        newBoard._positionCounts = new Dictionary<string, int>(this._positionCounts);

        // Copy castling flags
        newBoard.WhiteKingMoved = this.WhiteKingMoved;
        newBoard.WhiteRookKingSideMoved = this.WhiteRookKingSideMoved;
        newBoard.WhiteRookQueenSideMoved = this.WhiteRookQueenSideMoved;
        newBoard.BlackKingMoved = this.BlackKingMoved;
        newBoard.BlackRookKingSideMoved = this.BlackRookKingSideMoved;
        newBoard.BlackRookQueenSideMoved = this.BlackRookQueenSideMoved;

        // Copy En Passant target
        newBoard.EnPassantTarget = this.EnPassantTarget;

        // ตรวจสอบให้แน่ใจว่า King อยู่ในกระดาน
        if (newBoard.Board.Cast<int>().All(p => Math.Abs(p) != 6))
            throw new InvalidOperationException("Invalid board state: King is missing!");


        return newBoard;
    }

    // เพิ่มเมธอด Clone พิเศษสำหรับการตรวจสอบการโจมตี
    public ChessBoardModel CloneWithTurn(bool isWhiteTurn)
    {
        ChessBoardModel clone = this.Clone();
        clone.IsWhiteTurn = isWhiteTurn; // อนุญาตให้ตั้งค่าในคลาสตัวเอง
        return clone;
    }

    public void SetFiftyMoveCounter(int value)
    {
        FiftyMoveCounter = value;
    }

    // ======== Check Game Over ========
    public bool IsGameOver()
    {
        return IsCheckmate() || IsDraw();
    }

    public bool IsCheckmate()
    {
        return IsInCheck(IsWhiteTurn) && MoveGenerator.GenerateMoves(this).Count == 0;
    }

    // ======== Check Draw ========
    public bool IsDraw()
    {
        // Stalemate
        if (!IsInCheck(IsWhiteTurn) && MoveGenerator.GenerateMoves(this).Count == 0)
            return true;

        // Threefold Repetition
        string currentPos = SerializeBoard();
        if (PositionHistory.FindAll(p => p == currentPos).Count >= 3)
            return true;

        // Fifty Move Rule
        if (FiftyMoveCounter >= 100)
            return true;

        // Insufficient Material
        return HasInsufficientMaterial();
    }

    // ======== Check King Safety ========
    public bool IsInCheck(bool isWhite)
    {
        int kingValue = isWhite ? 6 : -6;
        Square kingPos = FindKingPosition(kingValue);

        // หากไม่พบ King ให้รีเทิร์น false เพื่อหลีกเลี่ยง Exception
        if (kingPos.X == -1 || kingPos.Y == -1)
            return false;

        return IsSquareUnderAttack(kingPos, !isWhite);
    }

    // ======== Helper Methods ========
    private bool HasInsufficientMaterial()
    {
        int totalPieces = 0;
        bool hasPawnOrMajorPiece = false;

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                int piece = Math.Abs(Board[x, y]);
                if (piece == 0) continue;

                totalPieces++;
                if (piece == 1 || piece == 4 || piece == 5)
                    hasPawnOrMajorPiece = true;
            }
        }

        return totalPieces <= 4 && !hasPawnOrMajorPiece;
    }

    private Square FindKingPosition(int kingValue)
    {
        if (kingValue != 6 && kingValue != -6)
            return new Square(-1, -1); // หรือ throw แล้วแต่คุณต้องการ

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (Board[x, y] == kingValue)
                    return new Square(x, y);
            }
        }

        return new Square(-1, -1); // ไม่พบราชา
    }


    // ======== Special Move Handlers ========
    private bool HandleCastling(MoveModel move, int piece)
    {
        try
        {
            if (Math.Abs(piece) == 6 && Math.Abs(move.FromY - move.ToY) == 2)
            {
                bool isWhite = piece > 0;
                int row = isWhite ? 7 : 0;

                bool isKingside = move.ToY == 6;
                bool isQueenside = move.ToY == 2;


                bool kingMoved = isWhite ? WhiteKingMoved : BlackKingMoved;
                bool rookMoved = isWhite ?
                    (isKingside ? WhiteRookKingSideMoved : WhiteRookQueenSideMoved) :
                    (isKingside ? BlackRookKingSideMoved : BlackRookQueenSideMoved);


                bool isValid = isWhite ?
                    (isKingside && !WhiteKingMoved && !WhiteRookKingSideMoved) ||
                    (isQueenside && !WhiteKingMoved && !WhiteRookQueenSideMoved) :
                    (isKingside && !BlackKingMoved && !BlackRookKingSideMoved) ||
                    (isQueenside && !BlackKingMoved && !BlackRookQueenSideMoved);


                if (!isValid)
                    throw new InvalidOperationException("Invalid castling attempt detected!");

                // ย้าย Rook
                int rookFromY = move.ToY == 6 ? 7 : 0;
                int rookToY = move.ToY == 6 ? 5 : 3;

                Board[row, rookToY] = Board[row, rookFromY];
                Board[row, rookFromY] = 0;

                // อัปเดต flag
                if (isWhite)
                {
                    WhiteKingMoved = true;
                    if (rookFromY == 7) WhiteRookKingSideMoved = true;
                    if (rookFromY == 0) WhiteRookQueenSideMoved = true;
                }
                else
                {
                    BlackKingMoved = true;
                    if (rookFromY == 7) BlackRookKingSideMoved = true;
                    if (rookFromY == 0) BlackRookQueenSideMoved = true;
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleCastling: {ex.Message}");
            throw;
        }
    }

    private void UpdateEnPassantTarget(MoveModel move, int piece)
    {
        if (Math.Abs(piece) == 1 && Math.Abs(move.ToX - move.FromX) == 2)
        {
            // ตั้งค่า EnPassantTarget เป็นช่องที่เบี้ย "กระโดดข้าม"
            int enPassantX = (move.FromX + move.ToX) / 2;
            EnPassantTarget = new Square(enPassantX, move.FromY);
        }
        else
        {
            EnPassantTarget = null;
        }
    }

    private void HandleEnPassantCapture(MoveModel move, int piece)
    {
        if (Math.Abs(piece) == 1 && EnPassantTarget.HasValue &&
            move.ToX == EnPassantTarget.Value.X &&
            move.ToY == EnPassantTarget.Value.Y)
        {
            int capturedPawnX = move.ToX + (piece > 0 ? 1 : -1);
            Board[capturedPawnX, move.ToY] = 0;

        }
    }

    public bool IsSquareUnderAttack(Square square, bool byWhite)
    {
        try
        {
            int attackerColor = byWhite ? 1 : -1;

            // 1. ตรวจสอบ Knight (-2 หรือ 2)
            int[,] knightMoves = { { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 }, { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 } };
            for (int i = 0; i < knightMoves.GetLength(0); i++)
            {
                int dx = knightMoves[i, 0];
                int dy = knightMoves[i, 1];
                int x = square.X + dx;
                int y = square.Y + dy;
                if (x >= 0 && x < 8 && y >= 0 && y < 8)
                {
                    int attackerPiece = Board[x, y];
                    if (attackerPiece == 2 * attackerColor) return true;
                }
            }

            // 2. ตรวจสอบ Pawn (-1 หรือ 1)
            int pawnDir = byWhite ? -1 : 1; // ทิศทางถูกต้อง: ขาวเดินขึ้น, ดำเดินลง
            int[] pawnCaptureY = { square.Y - 1, square.Y + 1 };
            foreach (int y in pawnCaptureY)
            {
                int x = square.X + pawnDir;
                if (x >= 0 && x < 8 && y >= 0 && y < 8)
                {
                    int attackerPiece = Board[x, y];
                    if (attackerPiece == 1 * attackerColor) return true;
                }
            }

            // 3. ตรวจสอบ King (-6 หรือ 6)
            int[,] kingMoves = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
            for (int i = 0; i < kingMoves.GetLength(0); i++)
            {
                int dx = kingMoves[i, 0];
                int dy = kingMoves[i, 1];
                int x = square.X + dx;
                int y = square.Y + dy;
                if (x >= 0 && x < 8 && y >= 0 && y < 8)
                {
                    int attackerPiece = Board[x, y];
                    if (attackerPiece == 6 * attackerColor) return true;
                }
            }

            // 4. ตรวจสอบแนวตรง (Rook/Queen)
            if (CheckLineAttack(square, new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }, attackerColor, new[] { 4, 5 }))
                return true;

            // 5. ตรวจสอบแนวทแยง (Bishop/Queen)
            if (CheckLineAttack(square, new int[,] { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } }, attackerColor, new[] { 3, 5 }))
                return true;

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in IsSquareUnderAttack: {ex.Message}");
            return false;
        }
    }

    private bool CheckLineAttack(Square square, int[,] directions, int attackerColor, int[] validPieces)
    {
        try
        {
            for (int d = 0; d < directions.GetLength(0); d++)
            {
                int dx = directions[d, 0];
                int dy = directions[d, 1];
                for (int step = 1; step < 8; step++)
                {
                    int x = square.X + dx * step;
                    int y = square.Y + dy * step;
                    if (x < 0 || x >= 8 || y < 0 || y >= 8) break;

                    int piece = Board[x, y];
                    if (piece != 0)
                    {
                        if ((piece * attackerColor > 0) && validPieces.Contains(Math.Abs(piece)))
                            return true;
                        break;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CheckLineAttack: {ex.Message}");
            return false;
        }
    }

    public string SerializeBoard()
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    sb.Append(Board[x, y]); // เพิ่มค่าหมากในตำแหน่ง [x,y]
                }
                sb.Append(IsWhiteTurn ? '1' : '0'); // เพิ่มสถานะตาเล่น (ต่อแถว)
            }
            return sb.ToString();
        }
        catch (Exception ex) // แก้ไขการสะกด Exception
        {
            Console.WriteLine($"เกิดข้อผิดพลาดในการสร้างบอร์ด: {ex.Message}");
            return ""; // คืนค่าสตริงว่างหากเกิดข้อผิดพลาด
        }
    }

    // ======== Display Board ========
    public void PrintBoard()
    {
        Dictionary<int, string> symbols = new Dictionary<int, string>
        {
            { 1, "♙" }, { -1, "♟" }, { 2, "♘" }, { -2, "♞" },
            { 3, "♗" }, { -3, "♝" }, { 4, "♖" }, { -4, "♜" },
            { 5, "♕" }, { -5, "♛" }, { 6, "♔" }, { -6, "♚" },
            { 0, "·" }
        };

        Console.WriteLine("  a  b  c  d  e  f  g  h");
        for (int x = 0; x < 8; x++)
        {
            Console.Write($"{8 - x} ");
            for (int y = 0; y < 8; y++)
                Console.Write($"{symbols[Board[x, y]]} ");
            Console.WriteLine($"{8 - x}");
        }
        Console.WriteLine("  a  b  c  d  e  f  g  h");
        Console.WriteLine($"ตาเล่น: {(IsWhiteTurn ? "ขาว" : "ดำ")}\n");
    }
}

public struct Square
{
    public int X { get; }
    public int Y { get; }

    public Square(int x, int y)
    {
        X = x;
        Y = y;
    }
}