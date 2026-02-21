using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchmakingManager : MonoBehaviour
{
    [Header("UI")]
    public MatchmakingUi matchmakingUi;

    [Header("Scene")]
    public string gameSceneName = "GameCoreOnline";

    private MatchmakingApi matchmakingApi;
    private bool isSearching = false;
    private bool isLoadingGame = false;
    private Coroutine pollingCoroutine;

    private const float POLL_INTERVAL = 2f;
    private const float SEARCH_TIMEOUT = 45f;

    public enum MatchMode
    {
        Ranked = 1,
        Normal = 2
    }

    [Header("Mode")]
    public MatchMode matchMode = MatchMode.Ranked;
    private string GetUsername() => SessionManager.Instance != null
        ? SessionManager.Instance.Username
        : PlayerPrefs.GetString("username", "Guest");

    private void Start()
    {
        matchmakingApi = GetComponent<MatchmakingApi>();
        if (matchmakingUi == null)
            matchmakingUi = FindObjectOfType<MatchmakingUi>();

        if (SessionManager.Instance.UserId <= 0)
        {
            matchmakingUi.SetStatusText("Please Login First");
            matchmakingUi.btnMatchmakingStart.interactable = false;
            return;
        }

        matchmakingUi.btnMatchmakingStart.onClick.RemoveAllListeners();
        matchmakingUi.btnMatchmakingStart.onClick.AddListener(StartMatchmaking);

        matchmakingUi.btnMatchmakingCancel.onClick.RemoveAllListeners();
        matchmakingUi.btnMatchmakingCancel.onClick.AddListener(CancelMatchmaking);
    }

    // START MATCHMAKING
    public void StartMatchmaking()
    {
        if (isSearching) return;

        if (matchMode == MatchMode.Normal)
        {
            Debug.Log("Normal Mode uses Invite system.");
            return;
        }

        isSearching = true;
        matchmakingUi.SetSearchingState(true);
        matchmakingUi.SetStatusText("Joining Queue...");

        string username = GetUsername();

        StartCoroutine(matchmakingApi.JoinQueue(
            username,
            0,
            3000,
            (int)matchMode,
            (success, response) =>
            {
                if (!success || response == null)
                {
                    ResetSearch("Connection Error");
                    return;
                }

                if (response.gameId != 0)
                {
                    StartGame(response);
                }
                else
                {
                    matchmakingUi.SetStatusText("Searching...");
                    StartPolling();
                }
            }));
    }

    // POLLING
    private void StartPolling()
    {
        if (pollingCoroutine != null)
            StopCoroutine(pollingCoroutine);

        pollingCoroutine = StartCoroutine(PollForMatch());
    }

    IEnumerator PollForMatch()
    {
        float elapsed = 0f;

        while (isSearching)
        {
            yield return new WaitForSeconds(POLL_INTERVAL);
            elapsed += POLL_INTERVAL;

            if (elapsed >= SEARCH_TIMEOUT)
            {
                ResetSearch("No match found.");
                yield break;
            }

            string username = GetUsername();
            matchmakingApi.StartCoroutine(
                matchmakingApi.CheckQueue(username,
                (success, response) =>
                {
                    if (!success || response == null)
                        return;

                    if (response.gameId != 0)
                    {
                        isSearching = false;
                        StartGame(response);
                    }
                }));
        }
    }

    // CANCEL
    public void CancelMatchmaking()
    {
        if (!isSearching) return;

        if (pollingCoroutine != null)
            StopCoroutine(pollingCoroutine);

        isSearching = false;

        matchmakingUi.ResetUI();

        string username = GetUsername();
        matchmakingApi.StartCoroutine(
            matchmakingApi.CancelQueue(username,
            (s, m) => { }));
    }

    private void ResetSearch(string message)
    {
        isSearching = false;
        matchmakingUi.ResetUI();
        matchmakingUi.SetStatusText(message);
    }

    // START GAME
    void StartGame(MatchFoundResponse response)
    {
        if (isLoadingGame) return;
        isLoadingGame = true;

        PlayerPrefs.SetInt("CurrentGameId", response.gameId);
        PlayerPrefs.SetString("CurrentRoomCode", response.roomCode ?? string.Empty);
        PlayerPrefs.SetString("MyColor", response.color ?? "random");
        PlayerPrefs.SetString("OpponentName", response.opponentUsername ?? string.Empty);

        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");
        PlayerPrefs.Save();

        matchmakingUi.SetStatusText($"VS {response.opponentUsername}");

        StartCoroutine(LoadGameSceneDelay());
    }

    IEnumerator LoadGameSceneDelay()
    {
        yield return new WaitForSeconds(1f);
        matchmakingUi.HideMatchmaking();
        SceneManager.LoadScene(gameSceneName);
    }
}
