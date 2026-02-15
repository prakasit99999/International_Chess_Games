using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GameAPI : MonoBehaviour
{
    [Header("Server Config")]
    [SerializeField] private string baseUrl = "http://localhost:8080/api/Game"; // เปลี่ยน Port ตามจริง

    // ========== START GAME ==========

    /// 1 Start Online Game (requires authentication)
    public IEnumerator CreateOnlineGame(GameCreateDto dto, Action<GameStartResponse> onSuccess = null, Action<string> onError = null)
    {
        string json = JsonUtility.ToJson(dto);
        Debug.Log("📤 Starting Online Game - JSON: " + json);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/start/online", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GameStartResponse>(req.downloadHandler.text);

                if (response != null && response.gameId > 0)
                {
                    Debug.Log($"✅ Online Game Started: ID {response.gameId}, Mode: {response.mode}, MatchMode: {response.matchMode}");
                    onSuccess?.Invoke(response);
                }
                else
                {
                    Debug.LogWarning("⚠️ Parse Error or Invalid ID: " + req.downloadHandler.text);
                    onError?.Invoke("Parse Error");
                }
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    /// 2 Start Offline Game (single player, AI, local multiplayer)
    public IEnumerator CreateOfflineGame(GameCreateDto dto, Action<GameStartResponse> onSuccess = null, Action<string> onError = null)
    {
        string json = JsonUtility.ToJson(dto);
        Debug.Log("📤 Starting Offline Game - JSON: " + json);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/start/offline", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GameStartResponse>(req.downloadHandler.text);

                if (response != null && response.gameId > 0)
                {
                    Debug.Log($"✅ Offline Game Started: ID {response.gameId}, Mode: {response.mode}");
                    onSuccess?.Invoke(response);
                }
                else
                {
                    Debug.LogWarning("⚠️ Parse Error or Invalid ID: " + req.downloadHandler.text);
                    onError?.Invoke("Parse Error");
                }
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    // ========== END GAME ==========

    /// 3 End Online Game (requires authentication)
    public IEnumerator FinalizeOnlineGame(GameResultDto dto, Action<GameEndResponse> onSuccess = null, Action<string> onError = null)
    {
        string json = JsonUtility.ToJson(dto);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/end/online", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GameEndResponse>(req.downloadHandler.text);
                Debug.Log($"✅ Online Game Finalized Successfully. White Rating: {response.whiteRating}, Black Rating: {response.blackRating}");
                onSuccess?.Invoke(response);
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    /// 4 End Offline Game
    public IEnumerator FinalizeOfflineGame(GameResultDto dto, Action<GameEndResponse> onSuccess = null, Action<string> onError = null)
    {
        string json = JsonUtility.ToJson(dto);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/end/offline", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GameEndResponse>(req.downloadHandler.text);
                Debug.Log($"✅ Offline Game Finalized Successfully.");
                onSuccess?.Invoke(response);
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    // ========== RESIGN GAME ==========

    /// 5 Resign Online Game (requires authentication)
    public IEnumerator ResignOnlineGame(int gameId, string reason, Action<GameEndResponse> onSuccess = null, Action<string> onError = null)
    {
        var dto = new GameResignDto
        {
            GameId = gameId,
            Reason = reason
            // PlayerId จะมาจาก Token ใน API
        };

        string json = JsonUtility.ToJson(dto);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/resign/online", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GameEndResponse>(req.downloadHandler.text);
                Debug.Log($"🏳️ Online Game {gameId} Resigned. White Rating: {response.whiteRating}, Black Rating: {response.blackRating}");
                onSuccess?.Invoke(response);
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    /// 6 Resign Offline Game
    public IEnumerator ResignOfflineGame(int gameId, int playerId, string reason, Action<GameEndResponse> onSuccess = null, Action<string> onError = null)
    {
        var dto = new GameResignDto
        {
            GameId = gameId,
            PlayerId = playerId,
            Reason = reason
        };

        string json = JsonUtility.ToJson(dto);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/resign/offline", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<GameEndResponse>(req.downloadHandler.text);
                Debug.Log($"🏳️ Offline Game {gameId} Resigned by Player {playerId}");
                onSuccess?.Invoke(response);
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    // ========== GET GAME INFO ==========

    /// 7 Get Game Result
    public IEnumerator GetGameResult(int gameId, Action<GameResultResponseDto> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{baseUrl}/result/{gameId}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var result = JsonUtility.FromJson<GameResultResponseDto>(req.downloadHandler.text);

                if (result != null)
                {
                    onSuccess?.Invoke(result);
                }
                else
                {
                    Debug.LogWarning("⚠️ Failed to parse GameResultResponseDto");
                    onError?.Invoke("Parse Error");
                }
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    /// 8 Get Game Status (เช็คสถานะเกม - จบหรือยัง, ใครชนะ)
    public IEnumerator GetGameStatus(int gameId, Action<GameStatusDto> onSuccess = null, Action<string> onError = null)
    {
        string url = $"{baseUrl}/status/{gameId}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var statusDto = JsonUtility.FromJson<GameStatusDto>(req.downloadHandler.text);

                if (statusDto != null)
                {
                    onSuccess?.Invoke(statusDto);
                }
                else
                {
                    Debug.LogWarning("⚠️ Failed to parse GameStatusDto");
                    onError?.Invoke("Parse Error");
                }
            }
            else
            {
                HandleError(req, onError);
            }
        }
    }

    // ========== HELPER METHODS ==========

    /// สร้าง Request แบบมาตรฐาน (ลดโค้ดซ้ำ)
    private UnityWebRequest CreateRequest(string url, string jsonBody)
    {
        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        // เพิ่ม Authorization Token ถ้ามี
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.Token))
        {
            req.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.Token);
        }

        return req;
    }

    /// จัดการ Error และ Log
    private void HandleError(UnityWebRequest req, Action<string> onError)
    {
        if (req.responseCode == 401)
        {
            Debug.LogWarning("Unauthorized (401). Redirecting to login...");
            if (authApi.Instance != null)
            {
                authApi.Instance.ForceLogout();
            }
            return;
        }

        string errorMsg = $"❌ Request Failed: {req.error} | Response: {req.downloadHandler.text}";
        Debug.LogError(errorMsg);
        onError?.Invoke(errorMsg);
    }
}