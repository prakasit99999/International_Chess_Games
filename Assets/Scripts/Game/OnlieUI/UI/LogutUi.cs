using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogutUi : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {
        authApi.Instance.OnLogoutSuccess = HandleLogoutSuccess;
        authApi.Instance.OnLogoutFailed = HandleLogoutFailed;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   
    private void HandleLogoutSuccess()
    {
        Debug.Log("Logout success UI side!");
        // หรือไป Scene Login โดยตรง
        SceneManager.LoadScene("Onlinelogin");
    }

    private void HandleLogoutFailed(string error)
    {
        Debug.LogError("Logout failed: " + error);
    }

    public void LogoutUser()
    {
        int userId = PlayerPrefs.GetInt("user_id", -1);
        if (userId != -1)
        {
            Debug.Log(userId);
            StartCoroutine(authApi.Instance.LogoutRequest(userId));
        }
        else
        {
            Debug.LogWarning("No user logged in.");
        }
    }

}
