using System;
using System.Collections;
using UnityEngine;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MatchmakingApi : MonoBehaviour
{
    private string baseUrl = "http://localhost:8080/api/Matchmaking";


    // --- ฟังก์ชัน 1: Join Queue ---
    public virtual IEnumerator JoinQueue(string username, int minRate, int maxRate, int matchMode, Action<bool, MatchFoundResponse> callback)
    {
        // Server expects POST with JSON body
        string url = $"{baseUrl}/join";

        var reqBody = new JoinQueueRequest
        {
            username = username,
            minRating = minRate,
            maxRating = maxRate,
            matchMode = matchMode
        };

        string json = JsonUtility.ToJson(reqBody);
        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        string token = SessionManager.Instance.Token;
        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            Debug.LogWarning("Unauthorized (401). Redirecting to login...");
            if (authApi.Instance != null) authApi.Instance.ForceLogout();
            yield break;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            string raw = request.downloadHandler.text;
            var response = ParseMatchFound(raw);
            Debug.LogWarning($"Join Response: {raw}");
            callback(true, response);
        }
        else
        {
            Debug.LogError($"Join Error: {request.error}");
            callback(false, null);
        }
    }

    // --- ฟังก์ชัน 2: Check Match ---
    public virtual IEnumerator CheckQueue(string username, Action<bool, MatchFoundResponse> callback)
    {
        string url = $"{baseUrl}/check?username={UnityWebRequest.EscapeURL(username)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            string token = SessionManager.Instance.Token;
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                UnityEngine.Debug.LogWarning("Unauthorized (401). Redirecting to login...");
                if (authApi.Instance != null) authApi.Instance.ForceLogout();
                yield break;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = ParseMatchFound(request.downloadHandler.text);
                if (response != null && response.gameId != 0)
                    callback(true, response);
                else
                    callback(false, null);
            }
            else
            {
                callback(false, null);
            }
        }
    }

    // --- ฟังก์ชัน 3: Cancel ---
    public virtual IEnumerator CancelQueue(string username, Action<bool, string> callback)
    {
        // Server expects PUT with JSON body
        string url = $"{baseUrl}/cancel";

        var reqBody = new CancelQueueRequest
        {
            username = username
        };

        string json = JsonUtility.ToJson(reqBody);
        var request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
      
        string token = SessionManager.Instance.Token;
        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            Debug.LogWarning("Unauthorized (401). Redirecting to login...");
            if (authApi.Instance != null) authApi.Instance.ForceLogout();
            yield break;
        }
        if (request.result == UnityWebRequest.Result.Success) callback(true, "Cancelled");
        else callback(false, request.error);
    }

    // --- ฟังก์ชัน 4: Update Player Status ---
    public virtual IEnumerator UpdatePlayerStatus(int userId, string status, Action<bool> callback)
    {
        string url = "http://localhost:8080/api/Player/status";
        string json = $"{{\"userId\":{userId}, \"status\":\"{status}\"}}";

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            Debug.LogWarning("Unauthorized (401). Redirecting to login...");
            if (authApi.Instance != null) authApi.Instance.ForceLogout();
            yield break;
        }
        callback(request.result == UnityWebRequest.Result.Success);
    }

    // --- ฟังก์ชัน 5: Get Leaderboard ---
    public virtual IEnumerator GetLeaderboard(Action<string> callback)
    {
        string url = "http://localhost:8080/api/Leaderboard";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                Debug.LogWarning("Unauthorized (401). Redirecting to login...");
                if (authApi.Instance != null) authApi.Instance.ForceLogout();
                yield break;
            }
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(request.downloadHandler.text); // ส่ง raw json ไปให้ UI จัดการ
            }
            else
            {
                callback(null);
            }
        }
    }

    private MatchFoundResponse ParseMatchFound(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        // Wrapper response: { status, message, matchDetails }
        if (json.Contains("\"matchDetails\""))
        {
            var wrapped = JsonUtility.FromJson<MatchmakingResponse>(json);
            if (wrapped != null && wrapped.matchDetails != null && wrapped.matchDetails.gameId != 0)
                return wrapped.matchDetails;
        }

        var response = JsonUtility.FromJson<MatchFoundResponse>(json);
        if (response != null && response.gameId != 0)
            return response;

        // Fallback for PascalCase JSON
        if (json.Contains("\"GameId\""))
        {
            var pascal = JsonUtility.FromJson<MatchFoundResponsePascal>(json);
            if (pascal != null)
            {
                return new MatchFoundResponse
                {
                    gameId = pascal.GameId,
                    opponentUsername = pascal.OpponentUsername,
                    roomCode = pascal.RoomCode,
                    gameType = pascal.GameType,
                    minRating = pascal.MinRating,
                    isRated = pascal.IsRated,
                    color = pascal.Color
                };
            }
        }

        return response;
    }

}
