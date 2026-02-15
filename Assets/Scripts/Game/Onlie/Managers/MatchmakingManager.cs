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

        int userId = SessionManager.Instance.UserId;

        StartCoroutine(matchmakingApi.JoinQueue(
            userId,
            0,
            3000,
            (success, response) =>
            {
                if (!success || response == null)
                {
                    ResetSearch("Connection Error");
                    return;
                }

                if (response.matchDetails != null &&
                    response.matchDetails.gameId != 0)
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

            int userId = SessionManager.Instance.UserId;

            matchmakingApi.StartCoroutine(
                matchmakingApi.CheckQueue(userId,
                (success, response) =>
                {
                    if (!success || response == null)
                        return;

                    if (response.matchDetails != null &&
                        response.matchDetails.gameId != 0)
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

        int userId = SessionManager.Instance.UserId;

        matchmakingApi.StartCoroutine(
            matchmakingApi.CancelQueue(userId,
            (s, m) => { }));
    }

    private void ResetSearch(string message)
    {
        isSearching = false;
        matchmakingUi.ResetUI();
        matchmakingUi.SetStatusText(message);
    }

    // START GAME
    void StartGame(MatchResponse response)
    {
        if (isLoadingGame) return;
        isLoadingGame = true;

        var details = response.matchDetails;

        PlayerPrefs.SetInt("CurrentGameId", details.gameId);
        PlayerPrefs.SetString("CurrentRoomCode", details.roomCode.ToString());
        PlayerPrefs.SetString("MyColor", details.color);
        PlayerPrefs.SetString("OpponentName", details.opponentUsername);

        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");
        PlayerPrefs.Save();

        matchmakingUi.SetStatusText($"VS {details.opponentUsername}");

        StartCoroutine(LoadGameSceneDelay());
    }

    IEnumerator LoadGameSceneDelay()
    {
        yield return new WaitForSeconds(1f);
        matchmakingUi.HideMatchmaking();
        SceneManager.LoadScene(gameSceneName);
    }
}
