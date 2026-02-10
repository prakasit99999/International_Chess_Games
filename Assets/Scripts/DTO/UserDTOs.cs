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
    public string status;
}

[System.Serializable]
public class UpdateStatusResponse
{
    public bool Success;
    public string Message;
}

[System.Serializable]
public class PlayerSearchDto
{
    public int UserId;
    public string username;
    public string status;
}

[System.Serializable]
public class PlayerSearchWrapper
{
    public PlayerSearchDto[] items;
}
