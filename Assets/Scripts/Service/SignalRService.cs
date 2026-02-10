
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SignalRService : MonoBehaviour
{
    public static SignalRService Instance { get; private set; }

    public event Action<InviteReceivedEvent> OnInviteReceived;
    public event Action<InviteAcceptedEvent> OnInviteAccepted;
    public event Action<InviteDeclinedEvent> OnInviteDeclined;
    public event Action<InviteCanceledEvent> OnInviteCanceled;
    public event Action<InviteExpiredEvent> OnInviteExpired; // Added missing event

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

    public void Enqueue(Action action) { } // Stub method
}


