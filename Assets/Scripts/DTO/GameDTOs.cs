using System;
using UnityEngine;

[Serializable]
public class GameCreateDto
{
    // ข้อมูลสำหรับ "เริ่มเกม" (ส่งไป /api/game/start)
    public string GameType;            // "single_player", "ai_vs_ai", "online_multiplayer"
    public int MatchMode;              // 0 = null, 1 = ranked , 2 = normal
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
    public string message;
    public int gameId;
    public string mode;        // game type (e.g., "online_multiplayer", "ai_game")
    public string matchMode;   // "ranked", "normal", or null
}

[Serializable]
public class GameEndResponse
{
    public string message;
    public int? whiteRating;
    public int? blackRating;
}

[Serializable]
public class GameResultResponseDto
{
    public int GameId;
    public string GameType;
    public int? MatchMode;
    public string Result;
    public int MoveCount;
    public string CreatedAt;
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
    public int GameId;
    public string GameType;
    public int? MatchMode;
    public string Status;
    public int MoveCount;
    public string CreatedAt;
}

[Serializable]
public class GameStatusDtoCamel
{
    public int gameId;
    public string gameType;
    public int? matchMode;
    public string status;
    public int moveCount;
    public string createdAt;
}
