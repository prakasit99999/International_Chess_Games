using System.Collections;
using Project.Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InviteManager : MonoBehaviour
{
    public static InviteManager Instance;

    [Header("Services")]
    public InviteApi inviteApi;

    [Header("UI")]
    public InvitePopupUi invitePopupUi;
    public GameObject waitingPanel;
    public Button waitingCancelButton;

    private string currentInviteId;
    private string sentInviteId;

    private Coroutine pollingCoroutine;
    private bool isLoadingGame = false;

    private const float POLL_INTERVAL = 2f;
    private const float POLL_TIMEOUT = 30f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (inviteApi == null)
            inviteApi = FindFirstObjectByType<InviteApi>();

        invitePopupUi?.Setup(OnAcceptClicked, OnDeclineClicked);
        invitePopupUi?.HideInvitePopup();

        waitingPanel?.SetActive(false);

        if (waitingCancelButton != null)
        {
            waitingCancelButton.onClick.RemoveAllListeners();
            waitingCancelButton.onClick.AddListener(OnCancelInviteClicked);
        }
    }

    // SEND INVITE (รองรับ matchMode)

    public void SendInvite(int toUserId, int matchMode, string username = "Unknown")
    {
        if (SessionManager.Instance.UserId <= 0)
        {
            Debug.LogError("Must login first.");
            return;
        }

        waitingPanel?.SetActive(true);

        var txt = waitingPanel?.GetComponentInChildren<TMP_Text>();
        if (txt != null)
            txt.text = $"Inviting {username}...";

        InviteRequest req = new InviteRequest
        {
            fromUserId = SessionManager.Instance.UserId,
            toUserId = toUserId,
            gameType = "online_multiplayer",
            matchMode = matchMode,
            expiresInSeconds = 300
        };

        StartCoroutine(inviteApi.SendInvite(req, (success, msg, inviteId) =>
        {
            if (!success)
            {
                Debug.LogError("Send invite failed: " + msg);
                waitingPanel?.SetActive(false);
                return;
            }

            sentInviteId = inviteId;
            StartPollingSentInvite(inviteId);
        }));
    }

    // POLLING SENT INVITE

    private void StartPollingSentInvite(string inviteId)
    {
        if (pollingCoroutine != null)
            StopCoroutine(pollingCoroutine);

        pollingCoroutine = StartCoroutine(PollSentInviteStatus(inviteId));
    }

    private IEnumerator PollSentInviteStatus(string inviteId)
    {
        float elapsed = 0f;

        while (!string.IsNullOrEmpty(sentInviteId))
        {
            yield return new WaitForSeconds(POLL_INTERVAL);
            elapsed += POLL_INTERVAL;

            if (elapsed >= POLL_TIMEOUT)
            {
                Debug.LogWarning("Invite polling timeout.");
                waitingPanel?.SetActive(false);
                sentInviteId = null;
                yield break;
            }

            bool done = false;

            inviteApi.StartCoroutine(
                inviteApi.GetSentInvites(SessionManager.Instance.UserId,
                (success, invites) =>
                {
                    done = true;

                    if (!success || invites == null)
                        return;

                    foreach (var inv in invites)
                    {
                        if (inv.inviteId != inviteId)
                            continue;

                        if (inv.status == "accepted" && inv.gameId.HasValue)
                        {
                            sentInviteId = null;
                            LoadOnlineGame(inv.gameId.Value, inv.fromUserId, inv.toUserId);
                        }
                        else if (inv.status == "declined" ||
                                 inv.status == "canceled" ||
                                 inv.status == "expired")
                        {
                            sentInviteId = null;
                            waitingPanel?.SetActive(false);
                        }
                    }
                }));

            yield return new WaitUntil(() => done);
        }
    }

    // RECEIVE INVITE

    public void HandleInviteReceivedFromPolling(InviteReceivedEvent evt)
    {
        currentInviteId = evt.inviteId;

        invitePopupUi?.SetStatusText(
            $"Player {evt.fromUserId} invited you to play!"
        );

        invitePopupUi?.ShowInvitePopup();
    }

    // ACCEPT

    private void OnAcceptClicked()
    {
        if (string.IsNullOrEmpty(currentInviteId))
            return;

        StartCoroutine(inviteApi.AcceptInvite(currentInviteId, (success, msg) =>
        {
            if (!success)
            {
                Debug.LogError("Accept failed: " + msg);
                return;
            }

            invitePopupUi?.HideInvitePopup();
            StartCoroutine(PollAcceptedInviteGame());
        }));
    }

    private IEnumerator PollAcceptedInviteGame()
    {
        float elapsed = 0f;

        while (elapsed < POLL_TIMEOUT)
        {
            yield return new WaitForSeconds(POLL_INTERVAL);
            elapsed += POLL_INTERVAL;

            bool done = false;

            inviteApi.StartCoroutine(
                inviteApi.GetInboxInvites(SessionManager.Instance.UserId,
                (success, invites) =>
                {
                    done = true;

                    if (!success || invites == null)
                        return;

                    foreach (var inv in invites)
                    {
                        if (inv.status == "accepted" && inv.gameId.HasValue)
                        {
                            LoadOnlineGame(inv.gameId.Value, inv.fromUserId, inv.toUserId);
                            return;
                        }
                    }
                }));

            yield return new WaitUntil(() => done);
        }

        Debug.LogWarning("Accept invite polling timeout.");
    }

    // DECLINE

    private void OnDeclineClicked()
    {
        if (string.IsNullOrEmpty(currentInviteId))
            return;

        StartCoroutine(inviteApi.DeclineInvite(currentInviteId, (success, msg) =>
        {
            invitePopupUi?.HideInvitePopup();
            currentInviteId = null;
        }));
    }

    // CANCEL

    private void OnCancelInviteClicked()
    {
        if (string.IsNullOrEmpty(sentInviteId))
            return;

        StartCoroutine(inviteApi.CancelInvite(sentInviteId, (success, msg) =>
        {
            waitingPanel?.SetActive(false);
            sentInviteId = null;
        }));
    }

    // LOAD GAME

    private void LoadOnlineGame(int gameId, int fromUserId, int toUserId)
    {
        if (isLoadingGame) return;
        isLoadingGame = true;

        waitingPanel?.SetActive(false);

        PlayerPrefs.SetInt("CurrentGameId", gameId);
        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");

        int myId = SessionManager.Instance.UserId;

        PlayerPrefs.SetString("MyColor",
            myId == fromUserId ? "white" : "black");

        SceneManager.LoadScene("GameCoreOnline");
    }
}
