using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class UserAPI : MonoBehaviour
{
    public static UserAPI Instance;
    private string baseUrl = "http://localhost:8080/api/User";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator UpdateStatus(string status, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/status"; // Corresponds to [HttpPut("status")]

        UpdateStatusRequest requestData = new UpdateStatusRequest
        {
            status = status
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            // Add Authorization header if needed, similar to profileApi
            string token = SessionManager.Instance.Token;
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                Debug.LogWarning("Unauthorized (401). Redirecting to login...");
                if (authApi.Instance != null)
                {
                    authApi.Instance.ForceLogout();
                }
                yield break;
            }


            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                // Assuming the server returns a standard response wrapper or just success
                // Based on UserDTO.cs: UpdateStatusResponse
                UpdateStatusResponse response = JsonUtility.FromJson<UpdateStatusResponse>(responseText);

                if (response != null && response.Success)
                {
                    callback(true, response.Message);
                }
                else
                {
                    // Fallback if response is not standard wrapper but request succeeded
                    callback(true, "Status updated");
                }
            }
            else
            {
                Debug.LogError($"UpdateStatus Error: {request.error}");
                callback(false, request.error);
            }
        }
    }

    /// ดึงรายชื่อผู้เล่นทั้งหมด
    public IEnumerator GetAllUsers(Action<UserListDto[]> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}/all";
        string token = SessionManager.Instance.Token;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                Debug.LogWarning("Unauthorized (401). Redirecting to login...");
                if (authApi.Instance != null)
                {
                    authApi.Instance.ForceLogout();
                }
                yield break;
            }


            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"📥 GetAllUsers Response: {responseText}");

                string wrappedJson = "{\"items\":" + responseText + "}";
                PlayerSearchWrapper wrapper = JsonUtility.FromJson<PlayerSearchWrapper>(wrappedJson);

                if (wrapper != null && wrapper.items != null)
                {
                    onSuccess?.Invoke(wrapper.items);
                }
                else
                {
                    onError?.Invoke("Failed to parse users data.");
                }
            }
            else
            {
                Debug.LogError($"GetAllUsers Error: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    /// ค้นหาผู้เล่นจาก Username
    public IEnumerator SearchUsers(string username, Action<UserListDto[]> onSuccess, Action<string> onError = null)
    {
        string url = $"{baseUrl}/search?query={UnityWebRequest.EscapeURL(username)}";
        string token = SessionManager.Instance.Token;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return request.SendWebRequest();

            if (request.responseCode == 401)
            {
                Debug.LogWarning("Unauthorized (401). Redirecting to login...");
                if (authApi.Instance != null)
                {
                    authApi.Instance.ForceLogout();
                }
                yield break;
            }


            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"📥 SearchUsers Response: {responseText}");

                string wrappedJson = "{\"items\":" + responseText + "}";
                PlayerSearchWrapper wrapper = JsonUtility.FromJson<PlayerSearchWrapper>(wrappedJson);

                if (wrapper != null && wrapper.items != null)
                {
                    onSuccess?.Invoke(wrapper.items);
                }
                else
                {
                    onError?.Invoke("Failed to parse search results.");
                }
            }
            else
            {
                Debug.LogError($"SearchUsers Error: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }
}
