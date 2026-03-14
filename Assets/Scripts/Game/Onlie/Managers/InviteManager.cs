using System.Collections;
using Project.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InviteManager : MonoBehaviour
{
    public static InviteManager Instance;

    [Header("Services")]
    public InviteApi inviteApi;
    public InvitePollingService invitePollingServiceObject;

    [Header("UI")]
    public InvitePopupUi invitePopupUi;
    public InviteWaitingUi inviteWaitingUi;

    [Header("Quick Test (Inspector)")]
    [Tooltip("Used by SendInviteFromInspector()")]
    public int testToUserId;
    [Tooltip("1 = Ranked, 2 = Normal")]
    public int testMatchMode = 2;
    [Tooltip("Display name for waiting UI")]
    public string testUsername = "Unknown";

    private string currentInviteId;
    private string sentInviteId;
    private bool isSendingInvite = false;
    private bool cancelPendingSend = false;

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

        inviteWaitingUi?.Setup(OnCancelInviteClicked);
        inviteWaitingUi?.HideWaiting();

        // Simple, centralized control of polling
        if (InvitePollingService.Instance != null)
            InvitePollingService.Instance.StartPolling();
    }

    private void OnDestroy()
    {
        if (InvitePollingService.Instance != null)
            InvitePollingService.Instance.StopPolling();
    }

    // SEND INVITE (รองรับ matchMode)

    public void SendInvite(int toUserId, int matchMode, string username = "Unknown")
    {
        if (SessionManager.Instance.UserId <= 0)
        {
            Debug.LogError("Must login first.");
            return;
        }

        inviteWaitingUi?.ShowWaiting($"Inviting {username}...");
        isSendingInvite = true;
        cancelPendingSend = false;

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
            isSendingInvite = false;

            if (!success)
            {
                Debug.LogError("Send invite failed: " + msg);
                inviteWaitingUi?.HideWaiting();
                return;
            }

            if (cancelPendingSend)
            {
                if (!string.IsNullOrEmpty(inviteId))
                {
                    StartCoroutine(inviteApi.CancelInvite(inviteId, (s, m) => { }));
                }

                inviteWaitingUi?.HideWaiting();
                sentInviteId = null;
                return;
            }

            sentInviteId = inviteId;
            StartPollingSentInvite(inviteId);
        }));
    }

    // SIMPLE SEND (Inspector-friendly)
    public void SendInviteFromInspector()
    {
        if (testToUserId <= 0)
        {
            Debug.LogWarning("Set testToUserId first.");
            return;
        }

        SendInvite(testToUserId, testMatchMode, testUsername);
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
                inviteWaitingUi?.HideWaiting();
                sentInviteId = null;
                yield break;
            }

            bool done = false;

            inviteApi.StartCoroutine(
                inviteApi.GetSentInvites((success, invites) =>
                {
                    done = true;

                    if (!success || invites == null)
                    {
                        Debug.LogWarning("Invite polling (sent) failed or null.");
                        return;
                    }

                    bool found = false;
                    foreach (var inv in invites)
                    {
                        if (inv.inviteId != inviteId)
                            continue;

                        found = true;
                        if (inv.status == "accepted" && inv.gameId > 0)
                        {
                            Debug.Log($"InviteManager: Sender accepted. gameId={inv.gameId} -> LoadOnlineGame");
                            sentInviteId = null;
                            SetOpponentNameFromInvite(inv);
                            LoadOnlineGame(inv.gameId, inv.fromUserId, inv.toUserId);
                        }
                        else if (inv.status == "declined" ||
                                 inv.status == "canceled" ||
                                 inv.status == "expired")
                        {
                            sentInviteId = null;
                            inviteWaitingUi?.HideWaiting();
                        }
                    }

                    if (!found)
                    {
                        Debug.LogWarning("Invite polling (sent): invite not found.");
                    }
                }));

            yield return new WaitUntil(() => done);
        }
    }

    // RECEIVE INVITE

    public void HandleInviteReceivedFromPolling(InviteReceivedEvent evt)
    {
        Debug.Log($"InviteManager: Received invite {evt.inviteId} from {evt.fromUserId} to {evt.toUserId}");
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

        StartCoroutine(inviteApi.AcceptInvite(currentInviteId, (success, response) =>
        {
            if (!success)
            {
                Debug.LogError("Accept failed.");
                return;
            }

            invitePopupUi?.HideInvitePopup();
            currentInviteId = null;

            if (response != null && response.gameId > 0)
            {
                Debug.Log($"InviteManager: Receiver accepted. gameId={response.gameId} -> LoadOnlineGame");
                SetOpponentNameFromInvite(response);
                LoadOnlineGame(response.gameId, response.fromUserId, response.toUserId);
                return;
            }

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
                inviteApi.GetInboxInvites((success, invites) =>
                {
                    done = true;

                    if (!success || invites == null)
                        return;

                    foreach (var inv in invites)
                    {
                        if (inv.status == "accepted" && inv.gameId > 0)
                        {
                            Debug.Log($"InviteManager: Sender accepted. gameId={inv.gameId} -> LoadOnlineGame");
                            SetOpponentNameFromInvite(inv);
                            LoadOnlineGame(inv.gameId, inv.fromUserId, inv.toUserId);
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
        if (isSendingInvite && string.IsNullOrEmpty(sentInviteId))
        {
            cancelPendingSend = true;
            inviteWaitingUi?.HideWaiting();
            return;
        }

        if (string.IsNullOrEmpty(sentInviteId))
            return;

        StartCoroutine(inviteApi.CancelInvite(sentInviteId, (success, msg) =>
        {
            inviteWaitingUi?.HideWaiting();
            sentInviteId = null;
        }));
    }

    private void SetOpponentNameFromInvite(InviteResponse invite)
    {
        if (invite == null || SessionManager.Instance == null)
            return;

        int myId = SessionManager.Instance.UserId;
        string opponentName = myId == invite.fromUserId
            ? invite.toUsername
            : invite.fromUsername;

        if (string.IsNullOrEmpty(opponentName))
            opponentName = "Opponent";

        PlayerPrefs.SetString("OpponentName", opponentName);
    }

    // LOAD GAME

    private void LoadOnlineGame(int gameId, int fromUserId, int toUserId)
    {
        if (isLoadingGame) return;
        isLoadingGame = true;

        inviteWaitingUi?.HideWaiting();

        PlayerPrefs.SetInt("CurrentGameId", gameId);
        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");

        int myId = SessionManager.Instance.UserId;

        PlayerPrefs.SetString("MyColor",
            myId == fromUserId ? "white" : "black");

        SceneManager.LoadScene("GameCoreOnline");
    }
}
