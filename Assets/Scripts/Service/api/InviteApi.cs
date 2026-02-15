using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.Services
{
    public class InviteApi : MonoBehaviour
    {
        private string baseUrl = "http://localhost:8080/api/invites";

        // --- 1. Send Invite ---
        public IEnumerator SendInvite(InviteRequest requestData, Action<bool, string, string> callback)
        {
            string url = baseUrl;
            string json = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Add Authorization header
                string token = SessionManager.Instance.Token;
                Debug.Log($"🚀 Sending Invite API Request... URL: {url}");
                Debug.Log($"📦 Request Body: {json}");

                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                    Debug.Log("🔑 Token attached.");
                }
                else
                {
                    Debug.LogError("❌ No Token found in SessionManager!");
                }

                yield return request.SendWebRequest();

                Debug.Log($"📥 API Response Code: {request.responseCode}");

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
                    string rawResponse = request.downloadHandler.text;
                    Debug.Log($"✅ API Success Response: {rawResponse}");

                    var response = JsonUtility.FromJson<InviteResponse>(rawResponse);
                    if (response != null && !string.IsNullOrEmpty(response.inviteId))
                    {
                        callback(true, $"Invite sent to user {response.toUserId}", response.inviteId);
                    }
                    else
                    {
                        Debug.LogError($"❌ Invalid Response Structure: {rawResponse}");
                        callback(false, $"Invalid response. Raw: {rawResponse}", null);
                    }
                }
                else
                {
                    Debug.LogError($"❌ Invite API Error: {request.error} | Response: {request.downloadHandler.text}");
                    callback(false, request.error, null);
                }
            }
        }

        // --- 2. Accept Invite ---
        public IEnumerator AcceptInvite(string inviteId, Action<bool, InviteResponse> callback)
        {
            string url = $"{baseUrl}/{inviteId}/accept";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                string token = SessionManager.Instance.Token;
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("Authorization", "Bearer " + token);

                yield return request.SendWebRequest();

                if (request.responseCode == 401)
                {
                    if (authApi.Instance != null)
                        authApi.Instance.ForceLogout();
                    callback(false, null);
                    yield break;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<InviteResponse>(request.downloadHandler.text);
                    callback(true, response);
                }
                else
                {
                    Debug.LogError("Accept Invite Error: " + request.error);
                    callback(false, null);
                }
            }
        }


        // --- 3. Decline Invite ---
        public IEnumerator DeclineInvite(string inviteId, Action<bool, string> callback)
        {
            yield return SendAction(inviteId, "decline", callback);
        }

        // --- 4. Cancel Invite ---
        public IEnumerator CancelInvite(string inviteId, Action<bool, string> callback)
        {
            yield return SendAction(inviteId, "cancel", callback);
        }

        // --- 5. Get Sent Invites (Sender Polling) ---
        public IEnumerator GetSentInvites(int userId, Action<bool, InviteResponse[]> callback)
        {
            string url = $"{baseUrl}/sent/{userId}";
            yield return GetInvitesRequest(url, callback);
        }

        // --- 6. Get Inbox Invites (Receiver Polling) ---
        public IEnumerator GetInboxInvites(int userId, Action<bool, InviteResponse[]> callback)
        {
            string url = $"{baseUrl}/inbox/{userId}";
            // Or just /api/invites/inbox/{userId} if baseUrl is /api/invites
            yield return GetInvitesRequest(url, callback);
        }

        // Helper for GET list
        private IEnumerator GetInvitesRequest(string url, Action<bool, InviteResponse[]> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                string token = SessionManager.Instance.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + token);
                }

                yield return request.SendWebRequest();

                if (request.responseCode == 401)
                {
                    if (authApi.Instance != null) authApi.Instance.ForceLogout();
                    callback(false, null);
                    yield break;
                }

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = "{\"invites\":" + request.downloadHandler.text + "}";
                    try
                    {
                        var list = JsonUtility.FromJson<InviteListResponse>(json);
                        callback(true, list != null ? list.invites : new InviteResponse[0]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error parsing invites: {ex.Message}");
                        callback(false, null);
                    }
                }
                else
                {
                    callback(false, null);
                }
            }
        }

        // Helper for actions
        private IEnumerator SendAction(string inviteId, string action, Action<bool, string> callback)
        {
            string url = $"{baseUrl}/{inviteId}/{action}";
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}"); // Empty JSON object
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Auth
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
                    // For Accept/Decline/Cancel, just assume success if HTTP 200
                    callback(true, "Success");
                }
                else
                {
                    Debug.LogError($"{action} Invite Error: {request.error}");
                    callback(false, request.error);
                }
            }
        }
    }
}
