using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AIEngine.Utilities;

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

public class ChessBoardModel
{
    // ======== Properties ========
    private List<ulong> _positionHistory = new List<ulong>();//เก็บประวัติตำแหน่ง
    private Stack<MoveState> _moveHistory = new Stack<MoveState>(256);
    private Dictionary<ulong, int> _positionCounts = new Dictionary<ulong, int>();
    public int[,] Board { get; set; }
    public bool IsWhiteTurn { get; set; }
    public bool WhiteKingMoved { get; set; }     // Castling Flags
    public bool WhiteRookKingSideMoved { get; set; }
    public bool WhiteRookQueenSideMoved { get; private set; }
    public bool BlackKingMoved { get; set; }
    public bool BlackRookKingSideMoved { get; private set; }
    public bool BlackRookQueenSideMoved { get; set; }
    public enum CastleIndex { WK = 0, WQ = 1, BK = 2, BQ = 3 }
    public Square? EnPassantTarget { get; set; }   // En Passant
    public int FiftyMoveCounter { get; private set; }     // Draw Conditions
    public int CurrentTurn { get; internal set; }
    public ulong ZobristKey { get; private set; }
    private void DebugCheckZobrist(string context)
    {
#if DEBUG
        ulong recomputed = Zobrist.ComputeKeyFromModel(this);
        Debug.Assert(ZobristKey == recomputed, $"[Zobrist] {context}: stored={ZobristKey}, recomputed={recomputed}");
#endif
    }
    // ======== Constructor ========
    private ChessBoardModel(bool empty) { }

    public ChessBoardModel()
    {
        Board = new int[8, 8];
        InitializeBoard();
        WhiteKingMoved = false;
        WhiteRookKingSideMoved = false;
        WhiteRookQueenSideMoved = false;
        BlackKingMoved = false;
        BlackRookKingSideMoved = false;
        BlackRookQueenSideMoved = false;
        EnPassantTarget = null;
        // compute initial Zobrist based on model state
        ZobristKey = Zobrist.ComputeKeyFromModel(this);
        _positionHistory = new List<ulong>();
        _positionCounts = new Dictionary<ulong, int>();
        _positionHistory.Add(ZobristKey);
        _positionCounts[ZobristKey] = 1;
    }

    public int RepetitionCount
    {
        get
        {
            var key = GetPositionKey();
            return _positionCounts.TryGetValue(key, out var c) ? c : 0;
        }
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

    private int GetCastlingMask()
    {
        int mask = 0;
        if (!WhiteKingMoved && !WhiteRookKingSideMoved) mask |= 1;
        if (!WhiteKingMoved && !WhiteRookQueenSideMoved) mask |= 2;
        if (!BlackKingMoved && !BlackRookKingSideMoved) mask |= 4;
        if (!BlackKingMoved && !BlackRookQueenSideMoved) mask |= 8;
        return mask;
    }

    public void ClearPositionHistory()
    {
        _positionHistory.Clear();
        _positionCounts.Clear();
    }

    public bool IsThreefoldRepetition()
    {
        if (_positionHistory.Count == 0) return false;

        var last = _positionHistory[_positionHistory.Count - 1];
        return _positionCounts.TryGetValue(last, out var c) && c >= 3;

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

    private ulong GetPositionKey()
    {
        return ZobristKey;
    }

    private void UpdateCastlingFlags(MoveModel move, int piece, int targetPiece)
    {
        // 1. กรณี King เดิน (เสียสิทธิ์ทั้ง 2 ฝั่ง)
        if (piece == 6) { WhiteKingMoved = true; }
        else if (piece == -6) { BlackKingMoved = true; }

        // 2. กรณี Rook เดิน (เสียสิทธิ์ฝั่งนั้น)
        if (piece == 4) // White Rook
        {
            if (move.FromX == 7 && move.FromY == 7) WhiteRookKingSideMoved = true;
            else if (move.FromX == 7 && move.FromY == 0) WhiteRookQueenSideMoved = true;
        }
        else if (piece == -4) // Black Rook
        {
            if (move.FromX == 0 && move.FromY == 7) BlackRookKingSideMoved = true;
            else if (move.FromX == 0 && move.FromY == 0) BlackRookQueenSideMoved = true;
        }

        // 3. [สำคัญ] กรณี Rook ถูกกิน (เสียสิทธิ์ฝั่งที่โดนกิน)
        if (targetPiece == 4) // White Rook ถูกกิน
        {
            if (move.ToX == 7 && move.ToY == 7) WhiteRookKingSideMoved = true;
            else if (move.ToX == 7 && move.ToY == 0) WhiteRookQueenSideMoved = true;
        }
        else if (targetPiece == -4) // Black Rook ถูกกิน
        {
            if (move.ToX == 0 && move.ToY == 7) BlackRookKingSideMoved = true;
            else if (move.ToX == 0 && move.ToY == 0) BlackRookQueenSideMoved = true;
        }
    }

    private void XORCastling(int mask)
    {
        if ((mask & 1) != 0) ZobristKey ^= Zobrist.Castling[(int)CastleIndex.WK];
        if ((mask & 2) != 0) ZobristKey ^= Zobrist.Castling[(int)CastleIndex.WQ];
        if ((mask & 4) != 0) ZobristKey ^= Zobrist.Castling[(int)CastleIndex.BK];
        if ((mask & 8) != 0) ZobristKey ^= Zobrist.Castling[(int)CastleIndex.BQ];
    }

    private void HandleEnPassantCapture(MoveModel move, int piece)
    {
        if (Math.Abs(piece) == 1 && EnPassantTarget.HasValue &&
            move.ToX == EnPassantTarget.Value.X &&
            move.ToY == EnPassantTarget.Value.Y)
        {
            int direction = (piece > 0) ? -1 : 1;
            int capturedPawnX = move.ToX - direction;
            int capturedPawn = Board[capturedPawnX, move.ToY];
            if (capturedPawn != 0)
            {
                int idx = Zobrist.PieceToIndex(capturedPawn);
                ZobristKey ^= Zobrist.PieceSquare[capturedPawnX, move.ToY, idx];
                Board[capturedPawnX, move.ToY] = 0;
            }
        }
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

    private bool HandleCastling(MoveModel move, int piece)
    {
        try
        {
            if (Math.Abs(piece) == 6 && Math.Abs(move.FromY - move.ToY) == 2)
            {
                bool isWhite = piece > 0;
                int row = isWhite ? 7 : 0;
                bool isKingside = move.ToY == 6;
                int rookFromY = isKingside ? 7 : 0;
                int rookToY = isKingside ? 5 : 3;
                int rookPiece = Board[row, rookFromY];
                // move rook on board
                Board[row, rookToY] = rookPiece;
                Board[row, rookFromY] = 0;
                if (rookPiece != 0)
                {
                    int rookIdx = Zobrist.PieceToIndex(rookPiece);
                    ZobristKey ^= Zobrist.PieceSquare[row, rookFromY, rookIdx];
                    ZobristKey ^= Zobrist.PieceSquare[row, rookToY, rookIdx];
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
            EnPassantTarget = new Square(enPassantX, move.ToY);
        }
        else
        {
            EnPassantTarget = null;
        }
    }

    private void PushMoveState(MoveModel move)
    {
        MoveState st = new MoveState();
        st.FromX = move.FromX;
        st.FromY = move.FromY;
        st.ToX = move.ToX;
        st.ToY = move.ToY;

        st.MovingPiece = Board[move.FromX, move.FromY];
        st.PromotionPiece = move.PromotionPiece;

        // detect captured piece (handle en-passant specially)
        int captured = Board[move.ToX, move.ToY];
        if (Math.Abs(st.MovingPiece) == 1 && EnPassantTarget.HasValue &&
            move.ToX == EnPassantTarget.Value.X && move.ToY == EnPassantTarget.Value.Y && captured == 0)
        {
            // en-passant capture: captured pawn sits behind the to-square
            int direction = (st.MovingPiece > 0) ? -1 : 1;
            int capX = move.ToX - direction;
            captured = Board[capX, move.ToY];
        }
        st.CapturedPiece = captured;

        st.OldEnPassantTarget = EnPassantTarget;
        st.OldFiftyMoveCounter = FiftyMoveCounter;
        st.OldCastlingMask = GetCastlingMask();
        st.OldZobristKey = ZobristKey;

        // save castling flags explicitly
        st.WKingMoved = WhiteKingMoved;
        st.WRookKingMoved = WhiteRookKingSideMoved;
        st.WRookQueenMoved = WhiteRookQueenSideMoved;
        st.BKingMoved = BlackKingMoved;
        st.BRookKingMoved = BlackRookKingSideMoved;
        st.BRookQueenMoved = BlackRookQueenSideMoved;

        _moveHistory.Push(st);
    }
    // ======== Helper Methods ========
    private bool HasInsufficientMaterial()
    {
        // เก็บรายการตัวหมากที่เหลือ (ไม่นับ King)
        List<int> pieces = new List<int>();
        // เก็บสีของช่องที่ตัวหมากนั้นยืนอยู่ (0 หรือ 1) เอาไว้เช็คกรณี Bishop
        List<int> squareColors = new List<int>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                int piece = Board[x, y];
                // ถ้าเป็นช่องว่าง ข้ามไป
                if (piece == 0) continue;
                // ถ้าเจอ Pawn (1), Rook (4), หรือ Queen (5) เกมยังไม่จบแน่นอน (มีโอกาสชนะ)
                int absPiece = Math.Abs(piece);
                if (absPiece == 1 || absPiece == 4 || absPiece == 5)
                    return false;

                // เก็บข้อมูลตัวหมากที่ไม่ใช่ King (เพราะ King มีอยู่เสมอ)
                if (absPiece != 6)
                {
                    pieces.Add(piece);
                    squareColors.Add((x + y) % 2); // สูตรเช็คสีช่อง: (Row + Col) % 2
                }
            }
        }

        // กรณี 1: เหลือแค่ King 2 ตัว (pieces ว่างเปล่า) -> เสมอ
        if (pieces.Count == 0) return true;
        // กรณี 2: เหลือหมาก 1 ตัว (นอกจาก King)
        if (pieces.Count == 1)
        {
            int type = Math.Abs(pieces[0]);
            // ถ้าเป็น Knight (2) หรือ Bishop (3) -> เสมอ (K+N vs K หรือ K+B vs K)
            if (type == 2 || type == 3) return true;
        }
        // กรณี 3: เหลือหมาก 2 ตัว (K+B vs K+B)
        if (pieces.Count == 2)
        {
            // ถ้าทั้งคู่เป็น Bishop (3)
            if (Math.Abs(pieces[0]) == 3 && Math.Abs(pieces[1]) == 3)
            {
                // เช็คว่าเป็น Bishop ที่วิ่งอยู่บนช่องสีเดียวกันหรือไม่
                // ถ้าสีช่อง (squareColors) เหมือนกัน แสดงว่าไม่มีทางกินกันลง -> เสมอ
                if (squareColors[0] == squareColors[1])
                    return true;
            }
        }
        // กรณีอื่นๆ ถือว่ายังไม่เสมอ (เช่น K+N vs K+N หรือ K+B vs K+N ในบางกติกาอาจเล่นต่อได้)
        return false;
    }
    // ======== Clone Board ========
    public ChessBoardModel Clone()
    {
        ChessBoardModel newBoard = new ChessBoardModel(true);
        newBoard.Board = (int[,])this.Board.Clone();
        newBoard.IsWhiteTurn = this.IsWhiteTurn;
        newBoard.ZobristKey = this.ZobristKey;
        newBoard._positionHistory = new List<ulong>(this._positionHistory);
        newBoard._positionCounts = new Dictionary<ulong, int>(this._positionCounts);
        newBoard.WhiteKingMoved = this.WhiteKingMoved;
        newBoard.WhiteRookKingSideMoved = this.WhiteRookKingSideMoved;
        newBoard.WhiteRookQueenSideMoved = this.WhiteRookQueenSideMoved;
        newBoard.BlackKingMoved = this.BlackKingMoved;
        newBoard.BlackRookKingSideMoved = this.BlackRookKingSideMoved;
        newBoard.BlackRookQueenSideMoved = this.BlackRookQueenSideMoved;
        newBoard.FiftyMoveCounter = this.FiftyMoveCounter;
        newBoard.CurrentTurn = this.CurrentTurn;
        newBoard.EnPassantTarget = this.EnPassantTarget;

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

    public void PushCurrentPosition()
    {
        ulong key = ZobristKey;
        _positionHistory.Add(key);
        if (_positionCounts.TryGetValue(key, out var c))
            _positionCounts[key] = c + 1;
        else
            _positionCounts[key] = 1;
        Debug.WriteLine($"Pushed position: {key}, Count: {_positionCounts[key]}");
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
    public void InitializePositionHistory()
    {
        _positionHistory = new List<ulong>();
        _positionCounts = new Dictionary<ulong, int>();
        RecordPosition();     // บันทึกสถานะเริ่มต้นด้วย
    }
    public void RecordPosition()
    {
        ulong key = ZobristKey;
        _positionHistory.Add(key);
        if (_positionCounts.TryGetValue(key, out var c))
            _positionCounts[key] = c + 1;
        else
            _positionCounts[key] = 1;
    }
    // ======== Make Move Unsafe (internal helper) ========
    public void MakeMoveUnsafe(MoveModel move)
    {
        try
        {
            // SAVE state first
            PushMoveState(move);

            int oldMask = GetCastlingMask();
            XORCastling(oldMask);

            int piece = Board[move.FromX, move.FromY];
            int targetPiece = Board[move.ToX, move.ToY];

            // ========= XOR ออก En Passant Target ถ้ามี ========
            if (EnPassantTarget != null)
            {
                ZobristKey ^= Zobrist.EnPassantFile[EnPassantTarget.Value.Y];
            }

            // ======== อัปเดต Zobrist Key ========
            if (piece != 0)
            {
                int idx = Zobrist.PieceToIndex(piece);
                ZobristKey ^= Zobrist.PieceSquare[move.FromX, move.FromY, idx]; // XOR out moving piece from source

            }

            if (targetPiece != 0)
            {
                int idx = Zobrist.PieceToIndex(targetPiece);
                ZobristKey ^= Zobrist.PieceSquare[move.ToX, move.ToY, idx]; // XOR out captured piece from destination
            }

            // ======== อัปเดตกฎ 50 การเดิน ========
            if (Math.Abs(piece) == 1 || targetPiece != 0)
                FiftyMoveCounter = 0;
            else
                FiftyMoveCounter++;

            // ======== [จัดการ En Passant Capture] **ทำก่อน** ย้ายหมากทับช่องเป้าหมาย ========
            HandleEnPassantCapture(move, piece);

            // ======== ย้ายหมากจริงบนกระดาน ========
            Board[move.ToX, move.ToY] = piece;
            Board[move.FromX, move.FromY] = 0;

            // ======== [อัปเดตเป้าหมาย En Passant สำหรับตาถัดไป] ========
            UpdateEnPassantTarget(move, piece);
            // ======== อัปเดต Zobrist Key สำหรับ En Passant Target ใหม่ ========
            if (EnPassantTarget != null)
            {
                ZobristKey ^= Zobrist.EnPassantFile[EnPassantTarget.Value.Y];
            }
            // ======== ตรวจจับ Castling และย้าย Rook ถ้าเกี่ยวข้อง ========
            bool isCastling = HandleCastling(move, piece);
            // ======== การ Promote เบี้ย ========
            if (move.PromotionPiece != 0)
            {
                Board[move.ToX, move.ToY] = move.PromotionPiece;
                int idx = Zobrist.PieceToIndex(move.PromotionPiece);
                ZobristKey ^= Zobrist.PieceSquare[move.ToX, move.ToY, idx];
            }
            else
            {
                if (piece != 0)
                {
                    int idx = Zobrist.PieceToIndex(piece);
                    ZobristKey ^= Zobrist.PieceSquare[move.ToX, move.ToY, idx];
                }
            }

            // ======== อัปเดตสถานะ King และ Rook (เฉพาะไม่ใช่ Castling) ========
            UpdateCastlingFlags(move, piece, targetPiece);
            int newMask = GetCastlingMask();
            XORCastling(newMask);
            // ======== อัปเดต Zobrist Key สำหรับ side-to-move ========
            ZobristKey ^= Zobrist.SideToMove;
            // ======== สลับตาเล่น ========
            IsWhiteTurn = !IsWhiteTurn;
            //RecordPosition();
            // ======== บันทึกตำแหน่งใหม่หลังจากเดินหมาก ========
            PushCurrentPosition();
        DebugCheckZobrist("MakeMoveUnsafe end");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MakeMoveUnsafe: {ex.Message}");
            throw;
        }
    }
    // ======== Robust MakeMove that returns a MoveRecord used for Undo ========
    public void UndoMove(MoveRecord rec)
    {
        int px = rec.FromX;
        int py = rec.FromY;
        int tx = rec.ToX;
        int ty = rec.ToY;

        int piece = rec.MovedPiece;
        int captured = rec.CapturedPiece;

        // ============================
        // 1) Flip turn back
        // ============================
        IsWhiteTurn = !IsWhiteTurn;

        // side-to-move was XOR'ed BEFORE MakeMove()
        ZobristKey ^= Zobrist.SideToMove;

        // ============================
        // 2) Undo EnPassantTarget hash (current)
        // ============================
        if (EnPassantTarget.HasValue)
            ZobristKey ^= Zobrist.EnPassantFile[EnPassantTarget.Value.Y];

        // restore old EP target
        EnPassantTarget = rec.PreviousEnPassant;

        if (EnPassantTarget.HasValue)
            ZobristKey ^= Zobrist.EnPassantFile[EnPassantTarget.Value.Y];

        // ============================
        // 3) Undo Promotion
        // ============================
        if (rec.WasPromotion)
        {
            // remove promoted piece
            ZobristKey ^= Zobrist.PieceSquare[tx, ty, Zobrist.PieceToIndex(rec.PromotionPiece)];

            Board[tx, ty] = rec.MovedPiece;   // restore pawn

            ZobristKey ^= Zobrist.PieceSquare[tx, ty, Zobrist.PieceToIndex(rec.MovedPiece)];
        }

        // ============================
        // 4) Undo King / Rook movement (Castling)
        // ============================

        bool isWhite = rec.MovedPiece > 0;

        // ← King moved? might be castling
        if (Math.Abs(piece) == 6)
        {
            if (isWhite)
            {
                // KING SIDE CASTLE
                if (px == 7 && py == 4 && tx == 7 && ty == 6)
                {
                    // move rook back
                    Board[7, 5] = 0;
                    Board[7, 7] = 4;

                    ZobristKey ^= Zobrist.PieceSquare[7, 5, Zobrist.PieceToIndex(4)];
                    ZobristKey ^= Zobrist.PieceSquare[7, 7, Zobrist.PieceToIndex(4)];
                }
                // QUEEN SIDE CASTLE
                else if (px == 7 && py == 4 && tx == 7 && ty == 2)
                {
                    Board[7, 3] = 0;
                    Board[7, 0] = 4;

                    ZobristKey ^= Zobrist.PieceSquare[7, 3, Zobrist.PieceToIndex(4)];
                    ZobristKey ^= Zobrist.PieceSquare[7, 0, Zobrist.PieceToIndex(4)];
                }
            }
            else
            {
                // KING SIDE
                if (px == 0 && py == 4 && tx == 0 && ty == 6)
                {
                    Board[0, 5] = 0;
                    Board[0, 7] = -4;

                    ZobristKey ^= Zobrist.PieceSquare[0, 5, Zobrist.PieceToIndex(-4)];
                    ZobristKey ^= Zobrist.PieceSquare[0, 7, Zobrist.PieceToIndex(-4)];
                }
                // QUEEN SIDE
                else if (px == 0 && py == 4 && tx == 0 && ty == 2)
                {
                    Board[0, 3] = 0;
                    Board[0, 0] = -4;

                    ZobristKey ^= Zobrist.PieceSquare[0, 3, Zobrist.PieceToIndex(-4)];
                    ZobristKey ^= Zobrist.PieceSquare[0, 0, Zobrist.PieceToIndex(-4)];
                }
            }
        }

        // ============================
        // 5) Remove piece from new square
        // ============================
        if (!rec.WasPromotion)
        {
            ZobristKey ^= Zobrist.PieceSquare[tx, ty, Zobrist.PieceToIndex(piece)];
        }

        // ============================
        // 6) Restore piece to old square
        // ============================
        Board[tx, ty] = 0;
        Board[px, py] = piece;

        ZobristKey ^= Zobrist.PieceSquare[px, py, Zobrist.PieceToIndex(piece)];

        // ============================
        // 7) Restore captured piece (normal or en-passant)
        // ============================

        if (rec.EnPassantCapture)
        {
            int cx = rec.EnPassantCapturedX;
            int cy = rec.EnPassantCapturedY;

            Board[cx, cy] = rec.CapturedPiece;

            ZobristKey ^= Zobrist.PieceSquare[cx, cy, Zobrist.PieceToIndex(rec.CapturedPiece)];
        }
        else if (captured != 0)
        {
            Board[tx, ty] = captured;

            ZobristKey ^= Zobrist.PieceSquare[tx, ty, Zobrist.PieceToIndex(captured)];
        }

        // ============================
        // 8) Restore Castling Rights
        // ============================

        // WHITE
        WhiteKingMoved = rec.PrevWhiteKingMoved;
        WhiteRookKingSideMoved = rec.PrevWhiteRookKingMoved;
        WhiteRookQueenSideMoved = rec.PrevWhiteRookQueenMoved;

        // BLACK
        BlackKingMoved = rec.PrevBlackKingMoved;
        BlackRookKingSideMoved = rec.PrevBlackRookKingMoved;
        BlackRookQueenSideMoved = rec.PrevBlackRookQueenMoved;

        // ============================
        // 9) Update Zobrist key for castling rights
        // ============================
        // mask ก่อน (ค่าที่อยู่ใน rec — สร้างจากค่า prev flags ที่เก็บไว้ตอน MakeMove)
        int prevMask = 0;
        if (!rec.PrevWhiteKingMoved && !rec.PrevWhiteRookKingMoved) prevMask |= 1;
        if (!rec.PrevWhiteKingMoved && !rec.PrevWhiteRookQueenMoved) prevMask |= 2;
        if (!rec.PrevBlackKingMoved && !rec.PrevBlackRookKingMoved) prevMask |= 4;
        if (!rec.PrevBlackKingMoved && !rec.PrevBlackRookQueenMoved) prevMask |= 8;

        // mask ปัจจุบันหลัง restore (GetCastlingMask อ่านจากฟิลด์ที่เพิ่งถูกเซ็ตเรียบร้อย)
        int currentMask = GetCastlingMask();

        // XOR เฉพาะบิตที่ต่าง (prevMask XOR currentMask)
        int diff = prevMask ^ currentMask;
        XORCastling(diff);
        
    }

    public void UndoMoveUnsafe()
    {
        // ตรวจสอบความปลอดภัยพื้นฐาน (Safety Check)
        if (_moveHistory.Count == 0) throw new InvalidOperationException("No move to undo");

        // 1. ดึงข้อมูล State ล่าสุดออกจาก Stack (O(1) Retrieval)
        var st = _moveHistory.Pop();

        // 2. จัดการเรื่อง Threefold Repetition (ถ้าใช้ระบบนี้ภายใน Undo)
        // หมายเหตุ: ต้องมั่นใจว่า MakeMove ได้ Push ไว้ ถ้าไม่ได้ Push บรรทัดนี้จะ Error
        PopLastPosition();

        // 3. กู้คืนค่า Irreversible State ทันที (Fast Restoration)
        // แทนที่จะคำนวณ Zobrist ใหม่แบบ XOR ย้อนกลับ เราใช้ค่าที่จดไว้ทับลงไปเลย (เร็วกว่ามาก)
        ZobristKey = st.OldZobristKey;

        // กู้คืนกฎ 50 ตา และเป้า En Passant
        FiftyMoveCounter = st.OldFiftyMoveCounter;
        EnPassantTarget = st.OldEnPassantTarget;

        // 4. กู้คืน Flags สิทธิ์การเข้าป้อม (Castling Rights)
        // การใช้ bool แยกกัน 6 ตัวแบบนี้ ปลอดภัยและชัดเจนที่สุด
        WhiteKingMoved = st.WKingMoved;
        WhiteRookKingSideMoved = st.WRookKingMoved;
        WhiteRookQueenSideMoved = st.WRookQueenMoved;

        BlackKingMoved = st.BKingMoved;
        BlackRookKingSideMoved = st.BRookKingMoved;
        BlackRookQueenSideMoved = st.BRookQueenMoved;

        // 5. กู้คืนตัวหมากบนกระดาน (Board Restoration)
        int moving = st.MovingPiece;

        // นำตัวเดินกลับไปที่จุดเริ่มต้น (From-Square)
        // *เทคนิค*: ไม่ต้องเช็คว่าเป็น Promotion หรือไม่ เพราะเราเก็บชิ้นส่วนเดิม (Pawn) ไว้ใน st.MovingPiece แล้ว
        Board[st.FromX, st.FromY] = st.MovingPiece;

        // 6. จัดการกรณี En Passant (จุดที่ซับซ้อนที่สุด)
        // เงื่อนไข: เป็นเบี้ย + เป้า En Passant ตรงกัน + มีการกินเกิดขึ้น
        bool wasEnPassantCapture = Math.Abs(st.MovingPiece) == 1
            && st.OldEnPassantTarget.HasValue
            && st.ToX == st.OldEnPassantTarget.Value.X
            && st.ToY == st.OldEnPassantTarget.Value.Y
            && st.CapturedPiece != 0;

        if (wasEnPassantCapture)
        {
            // กรณี En Passant:
            // A. ช่องปลายทาง (To-Square) ต้องกลับเป็นว่างเปล่า (เพราะเบี้ยเดินเฉียงไปที่ว่าง)
            Board[st.ToX, st.ToY] = 0;

            // B. คืนชีพเบี้ยที่ถูกกิน ไว้ที่ตำแหน่ง "ข้างหลัง" หรือตำแหน่งจริงของมัน
            int direction = (st.MovingPiece > 0) ? -1 : 1; // เช็คทิศทางตามระบบพิกัดของคุณ
            int capX = st.ToX - direction;
            Board[capX, st.ToY] = st.CapturedPiece;
        }
        else
        {
            // กรณีปกติ: วางตัวที่ถูกกินคืนที่ตำแหน่งปลายทาง (To-Square)
            // ถ้าไม่มีตัวถูกกิน (st.CapturedPiece == 0) ค่า 0 ก็จะถูกวางลงไป ซึ่งถูกต้อง
            Board[st.ToX, st.ToY] = st.CapturedPiece;
        }

        // 7. กู้คืนเรือกรณีเข้าป้อม (Castling Logic)
        // เช็คว่า King เดิน 2 ช่องแนวนอน
        if (Math.Abs(st.MovingPiece) == 6 && Math.Abs(st.FromY - st.ToY) == 2)
        {
            bool isKingside = st.ToY == 6; // ฝั่ง King คือไฟล์ G (index 6)
            int row = (st.MovingPiece > 0) ? 7 : 0; // ขาวแถว 7, ดำแถว 0

            int rookFromY = isKingside ? 7 : 0; // มุมกระดาน
            int rookToY = isKingside ? 5 : 3;   // ข้าง King

            // ย้ายเรือกลับที่เดิม
            Board[row, rookFromY] = Board[row, rookToY];
            Board[row, rookToY] = 0;
        }

        // 8. สลับตาเล่นกลับคืน
        IsWhiteTurn = !IsWhiteTurn;
    DebugCheckZobrist("UndoMoveUnsafe end");
    }
    public MoveRecord MakeMove(MoveModel move)
    {
        var record = new MoveRecord
        {
            FromX = move.FromX,
            FromY = move.FromY,
            ToX = move.ToX,
            ToY = move.ToY,
            MovedPiece = Board[move.FromX, move.FromY],
            CapturedPiece = Board[move.ToX, move.ToY],
            PreviousEnPassant = EnPassantTarget,
            PrevWhiteKingMoved = WhiteKingMoved,
            PrevWhiteRookKingMoved = WhiteRookKingSideMoved,
            PrevWhiteRookQueenMoved = WhiteRookQueenSideMoved,
            PrevBlackKingMoved = BlackKingMoved,
            PrevBlackRookKingMoved = BlackRookKingSideMoved,
            PrevBlackRookQueenMoved = BlackRookQueenSideMoved
        };

        int px = move.FromX;
        int py = move.FromY;
        int tx = move.ToX;
        int ty = move.ToY;

        int piece = record.MovedPiece;
        int targetPiece = Board[tx, ty]; // Read before any modifications
        int captured = targetPiece;

        // ============================
        // 1) XOR castling rights (old mask) - same as MakeMoveUnsafe
        // ============================
        int oldMask = GetCastlingMask();
        XORCastling(oldMask);

        // ============================
        // 2) Remove EnPassant hash (old) - same as MakeMoveUnsafe
        // ============================
        if (EnPassantTarget.HasValue)
            ZobristKey ^= Zobrist.EnPassantFile[EnPassantTarget.Value.Y];

        // ============================
        // 3) XOR side-to-move BEFORE move
        // ============================
        ZobristKey ^= Zobrist.SideToMove;

        // ============================
        // 4) Remove the piece from old square
        // ============================
        if (piece != 0)
        {
            ZobristKey ^= Zobrist.PieceSquare[px, py, Zobrist.PieceToIndex(piece)];
        }

        // ============================
        // 5) XOR out captured piece from destination (if any) - same as MakeMoveUnsafe
        // ============================
        if (targetPiece != 0)
        {
            ZobristKey ^= Zobrist.PieceSquare[tx, ty, Zobrist.PieceToIndex(targetPiece)];
        }

        // ============================
        // 6) Update Fifty Move Counter - same as MakeMoveUnsafe
        // ============================
        if (Math.Abs(piece) == 1 || targetPiece != 0)
            FiftyMoveCounter = 0;
        else
            FiftyMoveCounter++;

        // ============================
        // 7) Handle En Passant Capture - same as MakeMoveUnsafe (before moving piece)
        // ============================
        HandleEnPassantCapture(move, piece);
        
        // Update captured info for record if En Passant
        if (Math.Abs(piece) == 1 && record.PreviousEnPassant.HasValue &&
            tx == record.PreviousEnPassant.Value.X && ty == record.PreviousEnPassant.Value.Y)
        {
            int direction = (piece > 0) ? -1 : 1;
            int capX = tx - direction;
            int capY = ty; // En Passant capture uses same Y (file/column)
            captured = Board[capX, capY];
            record.CapturedPiece = captured;
            record.EnPassantCapture = true;
            record.EnPassantCapturedX = capX;
            record.EnPassantCapturedY = capY;
        }
        else
        {
            captured = targetPiece;
            record.CapturedPiece = captured;
        }

        // ============================
        // 8) Move piece on board - same as MakeMoveUnsafe
        // ============================
        Board[px, py] = 0;
        Board[tx, ty] = piece;

        // ============================
        // 9) Update En Passant Target - same as MakeMoveUnsafe
        // ============================
        UpdateEnPassantTarget(move, piece);
        
        // XOR in new EnPassantTarget if exists
        if (EnPassantTarget.HasValue)
        {
            ZobristKey ^= Zobrist.EnPassantFile[EnPassantTarget.Value.Y];
        }

        // ============================
        // 10) Handle Castling - same as MakeMoveUnsafe
        // ============================
        bool isCastling = HandleCastling(move, piece);

        // ============================
        // 11) Promotion - same as MakeMoveUnsafe
        // ============================
        if (move.PromotionPiece != 0)
        {
            Board[tx, ty] = move.PromotionPiece;
            int idx = Zobrist.PieceToIndex(move.PromotionPiece);
            ZobristKey ^= Zobrist.PieceSquare[tx, ty, idx];

            record.WasPromotion = true;
            record.PromotionPiece = move.PromotionPiece;
        }
        else
        {
            // XOR in piece to new square (if not promotion)
            if (piece != 0)
            {
                int idx = Zobrist.PieceToIndex(piece);
                ZobristKey ^= Zobrist.PieceSquare[tx, ty, idx];
            }
        }

        // ============================
        // 12) Update Castling Flags - same as MakeMoveUnsafe
        // ============================
        UpdateCastlingFlags(move, piece, targetPiece);
        int newMask = GetCastlingMask();
        XORCastling(newMask);

        // ============================
        // 13) Switch turn (side-to-move already XORed before)
        // ============================
        IsWhiteTurn = !IsWhiteTurn;

        DebugCheckZobrist("MakeMove (robust) end");
        return record;
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

        if (IsThreefoldRepetition())
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
    // ======== Special Move Handlers ========
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
            }
            sb.Append(IsWhiteTurn ? '1' : '0'); // เพิ่มสถานะตาเล่น (ต่อแถว)
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