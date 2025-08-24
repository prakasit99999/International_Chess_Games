using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MatchmakingApi : MonoBehaviour
{
    public static MatchmakingApi Instance;

    private string apiUrl = "http://localhost:8080/api/Matchmaking"; // URL ของ API

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    [System.Serializable]
    public class MatchFoundResponse
    {
        public string message;
        public MatchFoundDTOs MatchDetails;
    }

    [System.Serializable]
    public class MatchFoundDTOs
    {
        public int GameId;
        public string OpponentUsername;
        public string RoomCode;
        public int TimeControlMinutes;
        public string GameType;
        public bool IsRated;
        public string Color;
    }

    public IEnumerator JoinQueue(string username, int minRating, int maxRating, int preferredTimeControl, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/join?username={username}&minRating={minRating}&maxRating={maxRating}&preferredTimeControl={preferredTimeControl}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(www.downloadHandler.text);
            else
                onError?.Invoke(www.error);
        }
    }

    public IEnumerator CancelQueue(string username, System.Action<string> onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/cancel?username={username}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onSuccess?.Invoke(www.downloadHandler.text);
            else
                onError?.Invoke(www.error);
        }
    }

    public IEnumerator CheckForMatch(string username, System.Action<MatchFoundDTOs> onMatchFound, System.Action onNotFound, System.Action<string> onError)
    {
        string url = $"{apiUrl}/check?username={username}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                MatchFoundResponse resp = JsonUtility.FromJson<MatchFoundResponse>(www.downloadHandler.text);
                if (resp != null && resp.MatchDetails != null)
                    onMatchFound?.Invoke(resp.MatchDetails);
                else
                    onNotFound?.Invoke();
            }
            else if (www.responseCode == 404)
                onNotFound?.Invoke();
            else
                onError?.Invoke(www.error);
        }
    }
}
