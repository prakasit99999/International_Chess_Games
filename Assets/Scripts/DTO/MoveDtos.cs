using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MoveCreateDto
{
    public int GameId;
    public int MoveNumber;

    public int StartX;
    public int StartY;
    public int EndX;
    public int EndY;
    public int CapturedX;
    public int CapturedY;

    public int PieceType;
    public int PlayerTurn;
    public int CapturedPieceType;
    public int CapturedPieceTeam;
    public int PromotedTo;
    public int PromotedFrom;
    public int AlgorithmType;

    public bool IsCastling;
    public bool IsEnPassant;
    public bool IsCapture;
    public bool IsCheck;
    public bool IsPawnTwoStep;
    public bool PieceHasMovedBefore;

    // AI stats (ทำ PascalCase ให้หมด)
    public int AiEvaluationScore;
    public int AiDepthSearched;
    public int AiNodesEvaluated;
    public int MoveTimeMilliseconds;
}

[Serializable]
public class MoveBatchRequest
{
    public List<MoveCreateDto> moves;
}

[Serializable]
public class RootWrapper
{
    public MoveBatchRequest wrapper;
}

[Serializable]
public class MoveDto
{
    // ✅ Fields ตรงกับ Backend Response (camelCase ตาม API)
    public int moveNumber;      // Backend ส่งมาเป็น camelCase
    public int startX;
    public int startY;
    public int endX;
    public int endY;
    public string fromPosition; // "h2"
    public string toPosition;   // "h4"
    public string playerTurn;   // "white" or "black"

    // ✅ Helper properties สำหรับแปลงเป็น Vector2Int
    public Vector2Int from_position => new Vector2Int(startX, startY);
    public Vector2Int to_position => new Vector2Int(endX, endY);
    public int move_number => moveNumber;

    // ✅ Fields สำหรับ Game Over (ถ้า backend ส่งมา)
    public string status;       // "playing", "finished", "abandoned", "resignation"
    public string winner;       // "white", "black", "draw"

    // ✅ New Fields for Synchronization
    public int promotedTo;      // PieceType integer (e.g. 5=Queen)
    public bool isCastling;
    public bool isEnPassant;
}