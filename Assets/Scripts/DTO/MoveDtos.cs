using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MoveCreateDto
{
    // PascalCase ตาม Backend
    public int GameId;
    public int MoveNumber;

    // camelCase ตาม Backend
    public int startX;
    public int startY;
    public int endX;
    public int endY;
    public int capturedX;
    public int capturedY;

    // PascalCase (Enum -> int)
    public int PieceType;
    public int PlayerTurn;
    public int CapturedPieceType;
    public int CapturedPieceTeam;
    public int PromotedTo;
    public int PromotedFrom;
    public int AlgorithmType; // 0=None, 1=Minimax, 2=AlphaBeta

    // PascalCase (Boolean)
    public bool IsCasting;
    public bool IsEnPassant;
    public bool IsCapture;
    public bool IsCheck;
    public bool IsPawnTwoStep;
    public bool PieceHasMovedBefore;

    // camelCase (AI Stats)
    public double aiEvaluationScore;
    public int aiDepthSearched;
    public int aiNodesEvaluated;
    public int moveTimeMilliseconds;
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