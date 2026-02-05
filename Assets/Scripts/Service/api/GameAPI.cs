using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GameAPI : MonoBehaviour
{
    [Header("Server Config")]
    [SerializeField] private string baseUrl = "http://localhost:8080/api/Game"; // เปลี่ยน Port ตามจริง

    //  1. Start Game
    public IEnumerator CreateGame(GameCreateDto dto, Action<int> onSuccess = null, Action<string> onError = null)
    {
        string json = JsonUtility.ToJson(dto);
        Debug.Log("JSON: " + json);

        // ถ้ามีปัญหาเรื่อง null ให้พิจารณาใช้ Newtonsoft.Json แทน
        using (UnityWebRequest req = CreateRequest(baseUrl + "/start", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // แปลง JSON Response: { "message": "...", "gameId": 123 }
                var response = JsonUtility.FromJson<GameStartResponse>(req.downloadHandler.text);


                if (response != null && response.gameId > 0)
                {
                    Debug.Log($"✅ Game Started: ID {response.gameId}");
                    onSuccess?.Invoke(response.gameId);
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

    //  2. End Game (Normal Finish)
    public IEnumerator FinalizeGame(GameResultDto dto, Action<bool> onComplete = null)
    {
        string json = JsonUtility.ToJson(dto);

        using UnityWebRequest req = CreateRequest(baseUrl + "/end", json);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Game Finalized Successfully.");
            onComplete?.Invoke(true);
        }
        else
        {
            HandleError(req, (err) => onComplete?.Invoke(false));
        }
    }

    //  3. Resign / Abort (ยอมแพ้ / กดออก) **(NEW)**
    public IEnumerator ResignGame(int gameId, int playerId, string reason, Action<bool> onComplete = null)
    {
        var dto = new GameResignDto
        {
            GameId = gameId,
            PlayerId = playerId,
            Reason = reason
        };

        string json = JsonUtility.ToJson(dto);

        using (UnityWebRequest req = CreateRequest(baseUrl + "/resign", json))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"🏳️ Game {gameId} Resigned by Player {playerId}");
                onComplete?.Invoke(true);
            }
            else
            {
                HandleError(req, (err) => onComplete?.Invoke(false));
            }
        }
    }

    // 4. Get Game Status (เช็คสถานะเกม - จบหรือยัง, ใครชนะ)
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

    // สร้าง Request แบบมาตรฐาน (ลดโค้ดซ้ำ)
    private UnityWebRequest CreateRequest(string url, string jsonBody)
    {
        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    // จัดการ Error และ Log
    private void HandleError(UnityWebRequest req, Action<string> onError)
    {
        string errorMsg = $"❌ Request Failed: {req.error} | Response: {req.downloadHandler.text}";
        Debug.LogError(errorMsg);
        onError?.Invoke(errorMsg);
    }
}