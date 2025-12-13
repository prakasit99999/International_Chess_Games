using System.Collections;
using Mirror; // ต้องใช้สำหรับเริ่มเกม Online
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; // ต้องใช้สำหรับเปลี่ยน Scene
using UnityEngine.UI;

public class MatchmakingManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject matchmakingPanel;
    public Button startMatchmakingButton;
    public Button cancelMatchmakingButton;
    public Text statusText;

    [Header("Game Settings")]
    public string gameSceneName = "GameCoreOnline";

    [Header("Matchmaking Api")]
    public string matchmakingApiUrl = "http://localhost:5000/api/Matchmaking";

    private MatchmakingApi matchmakingApi; // สคริปต์ API ที่เราจะเรียกใช้
    private string username;
    private bool isSearching = false;
    private Coroutine matchmakingCoroutine;



    void Start()
    {
        matchmakingApi = GetComponent<MatchmakingApi>();
        if (matchmakingApi == null)
        {
            Debug.LogError("ไม่พบ Script MatchmakingApi! กรุณาลากใส่ GameObject เดียวกัน");
            statusText.text = "Error: Missing API Script";
            return;
        }
        username = PlayerPrefs.GetString("username", "");
        if (string.IsNullOrEmpty(username))
        {
            statusText.text = "Error: Please Login First";
            startMatchmakingButton.interactable = false;
            return;
        }

        startMatchmakingButton.onClick.AddListener(StartMatchmaking);
        cancelMatchmakingButton.onClick.AddListener(CancelMatchmaking);
    }

    void Update()
    {

    }

    void StartMatchmaking()
    {
        isSearching = true;
        startMatchmakingButton.gameObject.SetActive(false); // ซ่อนปุ่มหา
        cancelMatchmakingButton.gameObject.SetActive(true); // โชว์ปุ่มยกเลิก
        statusText.text = "Joining Queue...";
    }

    public void CancelMatchmaking()
    {
        matchmakingApi.CancelQueue(username, (success, message) =>
        {
            if (success)
            {
                ResetUI();
            }
            else
            {
                Debug.LogError($"Error canceling queue: {message}");
                statusText.text = "Error canceling queue";
            }

        });

    }

    void OnMatchFound()
    {
        matchmakingApi.CheckForMatch(username, (success, matchResponse) =>
        {
            if (success)
            {
                if (matchResponse.matchDetails != null)
                {
                    ResetUI();
                    onCickHideMatchmakingPanel();
                    SceneManager.LoadScene(gameSceneName);
                }
            }
            else
            {
                Debug.LogError($"Error checking for match: {matchResponse.message}");
                statusText.text = "Error checking for match";
            }
        });

    }

    void OnMatchNotFound()
    {
        matchmakingApi.CheckQueue(username, (success, matchResponse) =>
        {
            if (success)
            {
                if (matchResponse.matchDetails != null)
                {
                    ResetUI();
                    onCickHideMatchmakingPanel();
                    SceneManager.LoadScene(gameSceneName);
                }
            }
            else
            {
                Debug.LogError($"Error checking for match: {matchResponse.message}");
                statusText.text = "Error checking for match";
            }
        });
    }

    public void onCickShowMatchmakingPanel()
    {
        matchmakingPanel.SetActive(true);
    }

    public void onCickHideMatchmakingPanel()
    {
        matchmakingPanel.SetActive(false);
    }

    void ResetUI()
    {
        startMatchmakingButton.gameObject.SetActive(true);
        cancelMatchmakingButton.gameObject.SetActive(false);
        statusText.text = "Ready to play";
        isSearching = false;
    }

}

