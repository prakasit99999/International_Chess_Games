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
        int myUserId = SessionManager.Instance.UserId;
        if (myUserId <= 0) yield break;

        bool done = false;

        yield return InviteManager.Instance.inviteApi.GetInboxInvites(myUserId,
            (success, invites) =>
            {
                done = true;
                if (!success || invites == null) return;

                foreach (var invite in invites)
                {
                    if (invite.status == "pending" &&
                        !processedInviteIds.Contains(invite.inviteId))
                    {
                        processedInviteIds.Add(invite.inviteId);

                        InviteManager.Instance.HandleInviteReceivedFromPolling(
                            new InviteReceivedEvent
                            {
                                inviteId = invite.inviteId,
                                fromUserId = invite.fromUserId,
                                toUserId = invite.toUserId,
                                createdAt = invite.createdAt,
                                expiresAt = invite.expiresAt,
                                status = invite.status
                            });
                    }
                }
            });

        yield return new WaitUntil(() => done);
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
