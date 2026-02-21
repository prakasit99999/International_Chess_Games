using UnityEngine;

[System.Serializable]
public class JoinQueueRequest
{
    public string username;
    public int minRating = 0;
    public int maxRating = 3000;
    public int matchMode = 1; // 0 = null, 1 = ranked, 2 = normal
}

[System.Serializable]
public class CancelQueueRequest
{
    public string username;
}

// Server: MatchFoundDTOs (JSON usually camelCase)
[System.Serializable]
public class MatchFoundResponse
{
    public int gameId;
    public string opponentUsername;
    public string roomCode;
    public string gameType;
    public int minRating;
    public bool isRated;
    public string color;
}

// Fallback for PascalCase JSON
[System.Serializable]
public class MatchFoundResponsePascal
{
    public int GameId;
    public string OpponentUsername;
    public string RoomCode;
    public string GameType;
    public int MinRating;
    public bool IsRated;
    public string Color;
}

[System.Serializable]
public class MatchmakingResponse
{
    public string status;
    public string message;
    public MatchFoundResponse matchDetails;
}

