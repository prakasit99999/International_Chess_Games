using System;

[Serializable]
public class InviteRequest
{
    public int fromUserId;
    public int toUserId;
    public string gameType; // e.g., "online_multiplayer"
    public int matchMode;   // 0, 1, 2
    public int expiresInSeconds; // e.g., 300
}

[Serializable]
public class InviteResponse
{
    public string inviteId;
    public int fromUserId;
    public int toUserId;
    public string status; // "pending", "accepted", "declined", etc.
    public string createdAt;
    public string expiresAt;
    public int? gameId; // nullable
}

// --- SignalR Event DTOs ---

[Serializable]
public class InviteReceivedEvent
{
    public string inviteId;
    public int fromUserId;
    public int toUserId;
    public string createdAt;
    public string expiresAt;
    public string status;
}

[Serializable]
public class InviteAcceptedEvent
{
    public string inviteId;
    public int gameId;
    public int fromUserId;
    public int toUserId;
}

[Serializable]
public class InviteDeclinedEvent
{
    public string inviteId;
    public int fromUserId;
    public int toUserId;
}

[Serializable]
public class InviteCanceledEvent
{
    public string inviteId;
    public int fromUserId;
    public int toUserId;
}

[Serializable]
public class InviteExpiredEvent
{
    public string inviteId;
    public int fromUserId;
    public int toUserId;
}
