using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MatchmakingApi : MonoBehaviour
{
    private string baseUrl = "http://localhost:8080/api/Matchmaking";

    // --- ฟังก์ชัน 1: Join Queue (แก้ให้ส่ง MatchResponse) ---
    public virtual IEnumerator JoinQueue(string username, int minRate, int maxRate, Action<bool, MatchResponse> callback)
    {
        string url = $"{baseUrl}/join?username={username}&minRating={minRate}&maxRating={maxRate}";
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
                // แปลง JSON เป็น Object ทันที
                string json = request.downloadHandler.text;
                var response = JsonUtility.FromJson<MatchResponse>(json);
                callback(true, response);
            }
            else
            {
                Debug.LogError($"Join Error: {request.error}");
                callback(false, null);
            }
        }
    }

    // --- ฟังก์ชัน 2: Check Match ---
    public virtual IEnumerator CheckQueue(string username, Action<bool, MatchResponse> callback)
    {
        string url = $"{baseUrl}/check?username={username}";

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
                var response = JsonUtility.FromJson<MatchResponse>(request.downloadHandler.text);

                // เช็คว่ามี GameId จริงไหม
                if (response != null && response.matchDetails != null && response.matchDetails.gameId != 0)
                {
                    callback(true, response);
                }
                else
                {
                    callback(false, null);
                }
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
        string url = $"{baseUrl}/cancel?username={username}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
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

}