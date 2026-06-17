using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class authApi : MonoBehaviour
{
    public static authApi Instance;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    //api url
    private string apiUrl = "http://localhost:8080/api/Auth"; // เปลี่ยนเป็น URL ของ API ที่คุณใช้

    public Action<AuthSuccessResponse> OnLoginSuccess;
    public Action<string> OnLoginFailed;
    public Action OnRegisterSuccess;
    public Action<string> OnRegisterFailed;
    public Action OnLogoutSuccess;
    public Action<string> OnLogoutFailed;

    //Interface for LoginRequest
    public IEnumerator LoginRequest(string email, string password)
    {
        var jsonData = JsonUtility.ToJson(new LoginData { email = email, password = password });

        using (UnityWebRequest www = new UnityWebRequest(apiUrl + "/login", UnityWebRequest.kHttpVerbPOST))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            var responseText = www.downloadHandler.text;

            if (www.result == UnityWebRequest.Result.Success)
            {
                var authResp = JsonUtility.FromJson<AuthSuccessResponse>(responseText);
                if (authResp != null && authResp.success)
                {
                    // Use SessionManager to store login data
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.SetLoginData(
                            authResp.token,
                            authResp.userId,
                            authResp.username,
                            authResp.email,
                            authResp.status
                        );
                    }

                    OnLoginSuccess?.Invoke(authResp);
                }
                else
                {
                    OnLoginFailed?.Invoke("Invalid response.");
                }
            }
            else
            {
                try
                {
                    var errorObj = JsonUtility.FromJson<ApiError>(responseText);
                    OnLoginFailed?.Invoke(errorObj?.message ?? "Unknown error.");
                }
                catch
                {
                    OnLoginFailed?.Invoke("Error parsing response.");
                }
            }
        }
    }

    //Interface for LogoutRequest
    [Obsolete]
    public IEnumerator LogoutRequest(int userId)
    {
        UnityWebRequest unityWebRequest = UnityWebRequest.Post("http://localhost:8080/api/Auth/logout", "");
        using (UnityWebRequest www = unityWebRequest)
        {
            string token = SessionManager.Instance.Token;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("No authentication token found.");
                yield break;
            }
            www.SetRequestHeader("Authorization", "Bearer " + token);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Logout successful");

                // Use SessionManager to handle cleanup
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.Logout(redirect: false); // Redirect handled by OnLogoutSuccess if needed, or force it here
                }

                // เปลี่ยนหน้าไปที่หน้า Login
                OnLogoutSuccess?.Invoke();
                SceneManager.LoadScene("Onlinelogin");
            }
            else
            {
                OnLogoutFailed?.Invoke($"Logout failed ({www.responseCode}): {www.downloadHandler.text}");
            }
        }
    }

    //Interface for RegisterRequest
    public IEnumerator RegisterRequest(string username, string email, string password)
    {
        var jsonData = JsonUtility.ToJson(new RegisterData { username = username, email = email, password = password });

        using (UnityWebRequest www = new UnityWebRequest(apiUrl + "/register", UnityWebRequest.kHttpVerbPOST))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            var responseText = www.downloadHandler.text;

            if (www.result == UnityWebRequest.Result.Success)
            {
                OnRegisterSuccess?.Invoke();
            }
            else
            {
                try
                {
                    var errorObj = JsonUtility.FromJson<ApiError>(responseText);
                    OnRegisterFailed?.Invoke(errorObj?.message ?? "Unknown error.");
                }
                catch
                {
                    OnRegisterFailed?.Invoke("Error parsing response.");
                }
            }

        }
    }



    public void ForceLogout()
    {
        Debug.LogWarning("Token expired or unauthorized. Forcing logout.");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.Logout(redirect: true);
        }
        else
        {
            // Fallback if SessionManager missing (should not happen if set up correctly)
            PlayerPrefs.DeleteKey("auth_token");
            PlayerPrefs.Save();
            SceneManager.LoadScene("Onlinelogin");
        }
    }
}

