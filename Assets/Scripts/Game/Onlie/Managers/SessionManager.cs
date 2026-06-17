using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    // Properties wrapping PlayerPrefs for type safety and easy access
    public string Token
    {
        get => PlayerPrefs.GetString("auth_token", "");
        private set => PlayerPrefs.SetString("auth_token", value);
    }

    public int UserId
    {
        get => PlayerPrefs.GetInt("user_id", -1);
        private set => PlayerPrefs.SetInt("user_id", value);
    }

    public string Username
    {
        get => PlayerPrefs.GetString("username", "Guest");
        private set => PlayerPrefs.SetString("username", value);
    }

    public string Email
    {
        get => PlayerPrefs.GetString("email", "");
        private set => PlayerPrefs.SetString("email", value);
    }

    public string Status
    {
        get => PlayerPrefs.GetString("status", "offline");
        private set => PlayerPrefs.SetString("status", value);
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// Save login data to PlayerPrefs via properties
    public void SetLoginData(string token, int userId, string username, string email, string status)
    {
        Token = token;
        UserId = userId;
        Username = username;
        Email = email;
        Status = status;
        PlayerPrefs.Save();

        Debug.Log($"[SessionManager] Login Data Set: User {Username} (ID: {UserId})");
    }

    /// Clear all session data and optionally redirect to login scene
    public void Logout(bool redirect = true)
    {
        Debug.Log("[SessionManager] Logging out...");

        // Clear PlayerPrefs
        PlayerPrefs.DeleteKey("auth_token");
        PlayerPrefs.DeleteKey("user_id");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.DeleteKey("email");
        PlayerPrefs.DeleteKey("status");
        PlayerPrefs.Save();

        // Clear PerformanceTracker if it exists
        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.UserId = -1;
        }

        if (redirect)
        {
            SceneManager.LoadScene("Onlinelogin");
        }
    }
}
