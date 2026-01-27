using System;
using UnityEngine;

[Serializable]
public class GameCreateDto
{
    // ข้อมูลสำหรับ "เริ่มเกม" (ส่งไป /api/game/start)
    public string GameType;            // "single_player", "ai_vs_ai", "online_multiplayer"
    public string WhitePlayerType;     // "human", "ai_easy"
    public string BlackPlayerType;
    public int? WhitePlayerId;         // ใส่ null ได้
    public int? BlackPlayerId;
}

[Serializable]
public class GameResultDto
{
    public int GameId;
    public string Result;           // white, black, draw, abandoned
    public string ResultReason;     // checkmate, resignation, timeout
    public int? MoveCount;
    public string WhitePlayerType;
    public string BlackPlayerType;
    public int? WhitePlayerId;
    public int? BlackPlayerId;

}

[Serializable]
public class GameResignDto
{
    public int GameId;
    public int PlayerId;
    public string Reason;
}

[Serializable]
public class GameStartResponse
{
    public string Message;
    public int gameId;
}

[Serializable]
public class GeneralResponse
{
    public string Message;
    public string Error;
}

[Serializable]
public class GameStatusDto
{
    public int gameId;
    public string status;      // "playing", "finished", "abandoned"
    public string winner;      // "white", "black", "draw", null
    public string reason;      // "checkmate", "resignation", "timeout", "abandonment"
    public int moveCount;
}
