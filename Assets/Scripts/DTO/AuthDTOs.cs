using System;

[System.Serializable]
public class LoginData
{
    public string email;
    public string password;
}

[System.Serializable]
public class RegisterData
{
    public string username;
    public string email;
    public string password;
}

[System.Serializable]
public class ApiError
{
    public string message;
    public bool success;
}

[System.Serializable]
public class AuthSuccessResponse
{
    public int userId;
    public string username;
    public string email;
    public string token;
    public string status;
    public string message;
    public bool success;
}