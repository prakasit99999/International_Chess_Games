using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// Polling service to check for pending invites (alternative to SignalR)
/// Checks every 3 seconds for new invites
public class InvitePollingService : MonoBehaviour
{
    public static InvitePollingService Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float pollingInterval = 3f; // Check every 3 seconds

    private string baseUrl = "http://localhost:8080/api/invites";
    private bool isPolling = false;
    private HashSet<string> processedInviteIds = new HashSet<string>();

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

    private void Start()
    {
        StartPolling();
    }

    public void StartPolling()
    {
        if (!isPolling)
        {
            isPolling = true;
            StartCoroutine(PollForInvites());
            Debug.Log("🔄 Invite Polling Started");
        }
    }

    public void StopPolling()
    {
        isPolling = false;
        Debug.Log("⏸️ Invite Polling Stopped");
    }

    private IEnumerator PollForInvites()
    {
        while (isPolling)
        {
            yield return new WaitForSeconds(pollingInterval);
            yield return CheckPendingInvites();
        }
    }

    private IEnumerator CheckPendingInvites()
    {
        string token = SessionManager.Instance.Token;
        if (string.IsNullOrEmpty(token))
        {
            yield break; // No token, skip
        }

        int myUserId = SessionManager.Instance.UserId;
        if (myUserId <= 0)
        {
            // Try fallback
            myUserId = (PerformanceTracker.Instance != null) ? PerformanceTracker.Instance.UserId : 0;
            if (myUserId <= 0) yield break;
        }

        string url = $"{baseUrl}/inbox/{myUserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;

                // Parse array of invites
                // Assuming API returns [{},{}]
                InviteListResponse listResponse = null;
                try
                {
                    listResponse = JsonUtility.FromJson<InviteListResponse>("{\"invites\":" + rawResponse + "}");
                }
                catch
                {
                    // Ignore parse errors
                    Debug.LogError("❌ Error parsing invites: " + rawResponse);
                }

                if (listResponse != null && listResponse.invites != null)
                {
                    foreach (var invite in listResponse.invites)
                    {
                        // Only process invites where I am the receiver and haven't processed yet
                        if (invite.toUserId == myUserId && !processedInviteIds.Contains(invite.inviteId) && invite.status == "pending")
                        {
                            processedInviteIds.Add(invite.inviteId);
                            NotifyInviteReceived(invite);
                        }
                    }
                }
            }
        }
    }

    private void NotifyInviteReceived(InviteResponse invite)
    {
        Debug.Log($"📩 New Invite Detected: {invite.inviteId} from User {invite.fromUserId}");

        // Convert to event format
        InviteReceivedEvent evt = new InviteReceivedEvent
        {
            inviteId = invite.inviteId,
            fromUserId = invite.fromUserId,
            toUserId = invite.toUserId,
            createdAt = invite.createdAt,
            expiresAt = invite.expiresAt,
            status = invite.status
        };

        // Notify InviteManager
        if (InviteManager.Instance != null)
        {
            InviteManager.Instance.HandleInviteReceivedFromPolling(evt);
        }
    }

    private void OnDestroy()
    {
        StopPolling();
    }
}

[System.Serializable]
public class InviteListResponse
{
    public InviteResponse[] invites;
}
