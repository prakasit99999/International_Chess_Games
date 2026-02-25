public class MoveState
{
    // ข้อมูลการเดิน (เพื่อเอาไว้ถอยกลับ)
    public int FromX, FromY, ToX, ToY;
    public int MovingPiece;
    public int CapturedPiece;
    public int PromotionPiece;
    public bool WKingMoved, WRookKingMoved, WRookQueenMoved;
    public bool BKingMoved, BRookKingMoved, BRookQueenMoved;

    // State ที่คำนวณย้อนกลับไม่ได้ (Irreversible State)    public Square? OldEnPassant;
    public Square? OldEnPassantTarget;
    public int OldFiftyMoveCounter;
    public int OldCastlingMask; // เก็บเป็น int ตัวเดียว เร็วกว่า bool 6 ตัว
    public ulong OldZobristKey; // หัวใจสำคัญ: จำ Key เดิมไว้ ไม่ต้องคำนวณใหม่ตอน Undo
}
