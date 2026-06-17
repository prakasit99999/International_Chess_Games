public class MoveRecord
{
    public int FromX, FromY, ToX, ToY;
    public int MovedPiece;
    public int CapturedPiece;
    public bool EnPassantCapture;
    public int EnPassantCapturedX;
    public int EnPassantCapturedY;
    public bool WasPromotion;
    public int PromotionPiece;

    // Previous state for undo
    public Square? PreviousEnPassant;
    public bool PrevWhiteKingMoved;
    public bool PrevWhiteRookKingMoved;
    public bool PrevWhiteRookQueenMoved;
    public bool PrevBlackKingMoved;
    public bool PrevBlackRookKingMoved;
    public bool PrevBlackRookQueenMoved;
}
