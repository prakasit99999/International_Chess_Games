using System;

[Serializable]
public class InviteRequest
{
    public int senderId;
    public int receiverId;
    public string matchMode; // "normal" or "rank"
}

[Serializable]
public class InviteResponse
{
    public bool success;
    public string message;
}
