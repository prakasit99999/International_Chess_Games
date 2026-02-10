using System.Collections;
using Project.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InviteManager : MonoBehaviour
{
    public static InviteManager Instance;

    [Header("Services")]
    public InviteApi inviteApi;
    public SignalRService signalRService;

    [Header("UI References")]
    public InvitePopupUi invitePopupUi;
    public GameObject waitingPanel;
    public Button waitingCancelButton; // ปุ่ม Cancel ใน Waiting Panel

    private string currentInviteId; // Invite ID ที่ได้รับ (สำหรับ Accept/Decline)
    private string sentInviteId;    // Invite ID ที่ส่งไป (สำหรับ Cancel)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (inviteApi == null) inviteApi = FindFirstObjectByType<InviteApi>();
        if (signalRService == null) signalRService = SignalRService.Instance;

        if (signalRService != null)
        {
            signalRService.OnInviteReceived += HandleInviteReceived;
            signalRService.OnInviteAccepted += HandleInviteAccepted;
            signalRService.OnInviteDeclined += HandleInviteDeclined;
            signalRService.OnInviteCanceled += HandleInviteCanceled;
        }

        // Setup UI
        if (invitePopupUi != null)
        {
            invitePopupUi.Setup(OnAcceptClicked, OnDeclineClicked);
            invitePopupUi.HideInvitePopup();
        }

        if (waitingPanel != null) waitingPanel.SetActive(false);

        // Setup Cancel button in waiting panel
        if (waitingCancelButton != null)
        {
            waitingCancelButton.onClick.RemoveAllListeners();
            waitingCancelButton.onClick.AddListener(OnCancelInviteClicked);
        }
    }

    private void HideInvitePopup()
    {
        if (invitePopupUi != null) invitePopupUi.HideInvitePopup();
    }

    // --- Public Methods ---

    public void SendInvite(int toUserId, string username = "Unknown")
    {
        Debug.Log($"📨 Sending Invite to {username} (ID: {toUserId})");

        if (waitingPanel != null)
        {
            waitingPanel.SetActive(true);
            // Try to find a Text component to show who we are inviting
            TMP_Text waitText = waitingPanel.GetComponentInChildren<TMP_Text>();
            if (waitText != null)
            {
                Debug.Log($"📝 Setting waiting text to: Inviting {username}...");
                waitText.text = $"Inviting {username}...";
            }
            else
            {
                Text legacyText = waitingPanel.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    Debug.Log($"📝 Setting waiting (legacy) text to: Inviting {username}...");
                    legacyText.text = $"Inviting {username}...";
                }
            }
        }

        int myUserId = SessionManager.Instance.UserId;
        Debug.Log($"👤 My User ID: {myUserId}");

        if (myUserId <= 0)
        {
            Debug.LogError($"❌ Cannot send invite: You must be logged in first! {myUserId}");
            if (waitingPanel != null) waitingPanel.SetActive(false);
            return;
        }

        InviteRequest req = new InviteRequest
        {
            fromUserId = myUserId,
            toUserId = toUserId,
            gameType = "online_multiplayer",
            matchMode = 2, // 0=null, 1=Ranked, 2=Normal
            expiresInSeconds = 300
        };

        if (inviteApi != null)
        {
            Debug.Log($"📤 Calling InviteApi.SendInvite... From: {myUserId} To: {toUserId}");
            StartCoroutine(inviteApi.SendInvite(req, (success, msg, inviteId) =>
            {
                Debug.Log($"📩 InviteCallback: Success={success}, Msg={msg}, ID={inviteId}");
                if (success)
                {
                    Debug.Log($"✅ Invite Sent Successfully! ID: {inviteId}");
                    sentInviteId = inviteId; // เก็บ ID ไว้สำหรับ Cancel
                    if (waitingPanel != null)
                    {
                        waitingPanel.SetActive(true);
                        Debug.Log("👁️ WaitingPanel set to ACTIVE (Callback)");
                    }
                    // Start Polling
                    StartPollingSentInvite(inviteId);
                }
                else
                {
                    Debug.LogError($"❌ Send Invite Failed: {msg}");
                    if (waitingPanel != null)
                    {
                        waitingPanel.SetActive(false);
                        Debug.Log("🔒 WaitingPanel set to INACTIVE (Failed)");
                    }
                }
            }));
        }
        else
        {
            Debug.LogError("❌ InviteApi is null");
        }
    }

    private void OnCancelInviteClicked()
    {
        if (string.IsNullOrEmpty(sentInviteId))
        {
            Debug.LogWarning("⚠️ No invite to cancel (or invite ID not yet received)");
            if (waitingPanel != null) waitingPanel.SetActive(false); // Close panel anyway
            return;
        }

        Debug.Log($" Canceling Invite: {sentInviteId}");

        if (inviteApi != null)
        {
            StartCoroutine(inviteApi.CancelInvite(sentInviteId, (success, msg) =>
            {
                if (success)
                {
                    Debug.Log("✅ Invite Canceled");
                    if (waitingPanel != null) waitingPanel.SetActive(false);
                    sentInviteId = null;
                }
                else
                {
                    Debug.LogError($"❌ Cancel Failed: {msg}");
                }
            }));
        }
    }

    // --- Sender Polling ---
    private Coroutine pollingCoroutine;

    private void StartPollingSentInvite(string inviteId)
    {
        if (pollingCoroutine != null) StopCoroutine(pollingCoroutine);
        pollingCoroutine = StartCoroutine(PollSentInviteStatus(inviteId));
    }

    private void StopPollingSentInvite()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
    }

    private IEnumerator PollSentInviteStatus(string inviteId)
    {
        Debug.Log($"🔄 Start Polling Status for Invite ID: {inviteId}");
        while (!string.IsNullOrEmpty(sentInviteId) && sentInviteId == inviteId)
        {
            yield return new WaitForSeconds(2f); // Check every 2 seconds

            if (inviteApi != null && SessionManager.Instance.UserId > 0)
            {
                // Call GetSentInvites
                bool isDone = false;
                inviteApi.StartCoroutine(inviteApi.GetSentInvites(SessionManager.Instance.UserId, (success, invites) =>
                {
                    isDone = true;
                    if (success && invites != null)
                    {
                        foreach (var inv in invites)
                        {
                            if (inv.inviteId == inviteId)
                            {
                                // Debug.Log($"🔎 Polling Invite {inviteId}: Status = {inv.status}");
                                if (inv.status == "accepted")
                                {
                                    // Handle Accepted: Trigger valid event
                                    // Assuming gameId is 0 if missing, but flow requires it.
                                    // We'll use 0 and hope backend puts us in correct game or we query later.
                                    InviteAcceptedEvent evt = new InviteAcceptedEvent
                                    {
                                        gameId = 0, // Placeholder
                                        fromUserId = SessionManager.Instance.UserId, // It's us
                                        toUserId = inv.toUserId,
                                        inviteId = inviteId
                                    };
                                    HandleInviteAccepted(evt);
                                    StopPollingSentInvite();
                                }
                                else if (inv.status == "declined" || inv.status == "canceled" || inv.status == "expired")
                                {
                                    HandleInviteDeclined(new InviteDeclinedEvent { inviteId = inviteId });
                                    StopPollingSentInvite();
                                }
                            }
                        }
                    }
                }));
                yield return new WaitUntil(() => isDone);
            }
        }
    }

    private void OnDestroy()
    {
        StopPollingSentInvite();
        if (signalRService != null)
        {
            signalRService.OnInviteReceived -= HandleInviteReceived;
            signalRService.OnInviteAccepted -= HandleInviteAccepted;
            signalRService.OnInviteDeclined -= HandleInviteDeclined;
            signalRService.OnInviteCanceled -= HandleInviteCanceled;
        }
    }

    // --- SignalR Handlers ---
    private void HandleInviteReceived(InviteReceivedEvent evt)
    {
        Debug.Log($"📩 Invite Received from {evt.fromUserId}");
        currentInviteId = evt.inviteId;
        ShowInvitePopup($"Player {evt.fromUserId} invited you to play!");
    }

    // Public method for polling service to call
    public void HandleInviteReceivedFromPolling(InviteReceivedEvent evt)
    {
        HandleInviteReceived(evt);
    }

    private void HandleInviteAccepted(InviteAcceptedEvent evt)
    {
        Debug.Log($"✅ Invite Accepted! Game ID: {evt.gameId}");

        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (invitePopupUi != null) invitePopupUi.HideInvitePopup();

        sentInviteId = null;
        currentInviteId = null;

        // Save Game ID and Load Scene
        PlayerPrefs.SetInt("CurrentGameId", evt.gameId);
        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");

        // Check my ID
        int myId = SessionManager.Instance.UserId;
        if (evt.fromUserId == myId) PlayerPrefs.SetString("MyColor", "white");
        else PlayerPrefs.SetString("MyColor", "black");

        // Load Game Scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
    }

    private void HandleInviteDeclined(InviteDeclinedEvent evt)
    {
        Debug.Log("❌ Invite Declined.");
        if (waitingPanel != null) waitingPanel.SetActive(false);
        sentInviteId = null; // Clear the sent invite ID
    }

    private void HandleInviteCanceled(InviteCanceledEvent evt)
    {
        Debug.Log("⚠️ Invite Canceled.");
        if (invitePopupUi != null) invitePopupUi.HideInvitePopup();
        currentInviteId = null; // Clear the received invite ID
    }

    // --- UI Actions ---

    private void OnAcceptClicked()
    {
        if (!string.IsNullOrEmpty(currentInviteId))
        {
            StartCoroutine(inviteApi.AcceptInvite(currentInviteId, (success, msg) =>
            {
                if (!success) Debug.LogError($"Accept Failed: {msg}");
                if (invitePopupUi != null) invitePopupUi.HideInvitePopup();
                currentInviteId = null; // Clear after accepting
            }));
        }
    }

    private void OnDeclineClicked()
    {
        if (invitePopupUi != null) invitePopupUi.HideInvitePopup(); // Close immediately for better UX

        if (!string.IsNullOrEmpty(currentInviteId))
        {
            StartCoroutine(inviteApi.DeclineInvite(currentInviteId, (success, msg) =>
            {
                if (!success) Debug.LogError($"Decline Failed: {msg}");
                currentInviteId = null; // Clear after declining
            }));
        }
    }

    private void ShowInvitePopup(string msg)
    {
        if (invitePopupUi != null)
        {
            invitePopupUi.SetStatusText(msg);
            invitePopupUi.ShowInvitePopup();
        }
    }
}
