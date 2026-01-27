using UnityEngine;

[System.Serializable]
public class MatchResponse
{
    public string message;
    public MatchDetails matchDetails;
}

[System.Serializable]
public class MatchRequest
{
    public string username;
    public int minRating = 0;    // รับทุกคนที่ Rank มากกว่า 0
    public int maxRating = 3000; // รับทุกคนที่ Rank น้อยกว่า 3000
}

[System.Serializable]
public class MatchDetails
{
    public int gameId;
    public int roomCode;
    public string opponentUsername;
    public string color;
    public string gameType;
}