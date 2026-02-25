using UnityEngine;
using static ChessPiece;

public struct MoveResult
{
    public Vector2Int From;
    public Vector2Int To;
    public PieceType PieceType;
    public PieceType CapturedType;
    public Team CapturedTeam;
    public bool IsCastling;
    public bool IsEnPassant;
    public PieceType PromotedTo;

    // Helper Constructor
    public MoveResult(Vector2Int from, Vector2Int to, PieceType pieceType)
    {
        From = from;
        To = to;
        PieceType = pieceType;

        // Defaults
        CapturedType = PieceType.None;
        CapturedTeam = Team.None;
        IsCastling = false;
        IsEnPassant = false;
        PromotedTo = PieceType.None;
    }
}
