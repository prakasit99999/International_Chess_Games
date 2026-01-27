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

    public IEnumerator UpdateStatus(int userId, string status, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/status"; // Corresponds to [HttpPut("status")]

        UpdateStatusRequest requestData = new UpdateStatusRequest
        {
            userId = userId,
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
            string token = PlayerPrefs.GetString("auth_token", "");
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }

            yield return request.SendWebRequest();

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
}
