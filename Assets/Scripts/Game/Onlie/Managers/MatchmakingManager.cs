using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchmakingManager : MonoBehaviour
{
    [Header("UI References")]
    public MatchmakingUi matchmakingUi;

    [Header("Game Settings")]
    public string gameSceneName = "GameCoreOnline";

    private MatchmakingApi matchmakingApi;
    private string username;
    private bool isSearching = false;
    private Coroutine pollingCoroutine;

    public enum MatchMode { Ranked, Normal }
    [Header("Mode Settings")]
    public MatchMode matchMode = MatchMode.Ranked;

    public void Start()
    {
        matchmakingApi = GetComponent<MatchmakingApi>();
        if (matchmakingUi == null) matchmakingUi = FindObjectOfType<MatchmakingUi>();

        // เช็ค Login
        username = PlayerPrefs.GetString("username", "");
        if (string.IsNullOrEmpty(username))
        {
            matchmakingUi.SetStatusText("Error: Please Login First");
            if (matchmakingUi.btnMatchmakingStart != null)
                matchmakingUi.btnMatchmakingStart.interactable = false;
            return;
        }

        // --- เชื่อมปุ่ม (Wiring Buttons) ---
        // ปุ่ม Start -> เรียกฟังก์ชัน StartMatchmaking
        if (matchmakingUi.btnMatchmakingStart != null)
        {
            matchmakingUi.btnMatchmakingStart.onClick.RemoveAllListeners();
            matchmakingUi.btnMatchmakingStart.onClick.AddListener(StartMatchmaking);
        }

        // ปุ่ม Cancel -> เรียกฟังก์ชัน CancelMatchmaking
        if (matchmakingUi.btnMatchmakingCancel != null)
        {
            matchmakingUi.btnMatchmakingCancel.onClick.RemoveAllListeners();
            matchmakingUi.btnMatchmakingCancel.onClick.AddListener(CancelMatchmaking);
        }
    }

    // --- ฟังก์ชันเริ่มหาห้อง ---
    public void StartMatchmaking()
    {
        if (isSearching) return;

        // ถ้าเป็น Normal Mode (เชิญผู้เล่น) จะไม่เข้าสู่ระบบ Matchmaking แบบ Ranked
        if (matchMode == MatchMode.Normal)
        {
            Debug.Log("Mode is Normal: Waiting for Invite interaction.");
            return;
        }

        isSearching = true;
        matchmakingUi.SetSearchingState(true); // เปลี่ยนปุ่มเป็น Cancel
        matchmakingUi.SetStatusText("Joining Queue...");

        StartCoroutine(matchmakingApi.JoinQueue(username, 0, 3000, (success, response) =>
        {
            if (success && response != null)
            {
                if (response.matchDetails != null && response.matchDetails.gameId != 0)
                {
                    StartGame(response); // เจอทันที
                }
                else
                {
                    matchmakingUi.SetStatusText("Searching...");
                    Debug.Log($"Searching for match...{response.matchDetails.gameId}");
                    if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);
                    pollingCoroutine = StartCoroutine(PollForMatch()); // รอคิว
                }
            }
            else
            {
                isSearching = false;
                matchmakingUi.ResetUI();
                matchmakingUi.SetStatusText("Connection Error");
            }
        }));
    }

    // --- ฟังก์ชันยกเลิก ---
    public void CancelMatchmaking()
    {
        if (!isSearching) return; // ถ้าไม่ได้หาอยู่ ก็ไม่ต้องทำอะไร

        if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);
        isSearching = false;

        // Reset UI ทันทีเพื่อให้ผู้เล่นรู้สึกว่าระบบตอบสนองเร็ว
        matchmakingUi.ResetUI();

        // ส่งเรื่องไปบอก Server (ทำเบื้องหลัง)
        matchmakingApi.StartCoroutine(matchmakingApi.CancelQueue(username, (s, m) => { }));
    }

    // ... (ส่วน PollForMatch และ StartGame เหมือนเดิม) ...
    IEnumerator PollForMatch()
    {
        while (isSearching)
        {
            yield return new WaitForSeconds(2f);
            matchmakingApi.StartCoroutine(matchmakingApi.CheckQueue(username, (success, response) =>
            {
                if (success && response != null && response.matchDetails != null && response.matchDetails.gameId != 0)
                {
                    isSearching = false;
                    StartGame(response);
                }
            }));
        }
    }

    void StartGame(MatchResponse response)
    {
        var details = response.matchDetails;
        PlayerPrefs.SetInt("CurrentGameId", details.gameId);
        PlayerPrefs.SetString("CurrentRoomCode", details.roomCode.ToString());
        PlayerPrefs.SetString("MyColor", details.color);
        PlayerPrefs.SetString("OpponentName", details.opponentUsername);

        // 🔹 ระบุโหมดให้ GameManager รู้ว่าเป็น Online
        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");

        PlayerPrefs.Save();

        matchmakingUi.SetStatusText($"VS {details.opponentUsername}");
        Debug.Log("Starting game with opponent: " + details.opponentUsername);
        StartCoroutine(LoadGameSceneDelay());
    }

    IEnumerator LoadGameSceneDelay()
    {
        yield return new WaitForSeconds(1.0f);
        matchmakingUi.HideMatchmaking();
        SceneManager.LoadScene(gameSceneName);
    }
}