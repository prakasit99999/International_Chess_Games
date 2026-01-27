[System.Serializable]
public class ProfileResponse
{
    public string username;
    public string email;
    public int rating;
    public int win;
    public int lose;
    public int drawn;
    public bool Success;
    public string Message;
}

[System.Serializable]
public class UpdateStatusRequest
{
    public int userId;
    public string status;
}

public class UpdateStatusResponse
{
    public bool Success;
    public string Message;
}

