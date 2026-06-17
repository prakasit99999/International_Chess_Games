using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LeaderboardApi : MonoBehaviour
{
    public static LeaderboardApi Instance;
    private string apiUrl = "http://localhost:8080/api/leaderboard";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// ดึงข้อมูล Leaderboard จาก Server
    public IEnumerator GetLeaderboard(Action<LeaderboardResponse[]> onSuccess, Action<string> onError = null)
    {
        string token = PlayerPrefs.GetString("auth_token", "");
        if (string.IsNullOrEmpty(token))
        {
            onError?.Invoke("No authentication token found.");
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl))
        {
            www.SetRequestHeader("Authorization", "Bearer " + token);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;

                // ✅ Log: แสดง JSON ดิบจาก Server
                Debug.Log($"📥 Raw JSON from Server: {responseText}");

                // Unity JsonUtility ไม่รองรับ array โดยตรง ต้อง wrap ด้วย object
                string wrappedJson = "{\"items\":" + responseText + "}";
                LeaderboardWrapper wrapper = JsonUtility.FromJson<LeaderboardWrapper>(wrappedJson);

                if (wrapper != null && wrapper.items != null)
                {
                    // ✅ Log: แสดงจำนวนข้อมูลที่ได้
                    Debug.Log($"✅ Parsed {wrapper.items.Length} leaderboard entries");

                    // ✅ Log: แสดงข้อมูลแต่ละแถว
                    foreach (var item in wrapper.items)
                    {
                        Debug.Log($"   Rank:{item.rank} | {item.username} | Rating:{item.rating} | W:{item.w} L:{item.l} D:{item.d}");
                    }

                    onSuccess?.Invoke(wrapper.items);
                }
                else
                {
                    Debug.LogError("❌ Failed to parse: wrapper or items is null");
                    onError?.Invoke("Failed to parse leaderboard data.");
                }
            }
            else
            {
                Debug.LogError($"❌ API Error: {www.error}");
                onError?.Invoke($"Failed to load leaderboard: {www.error}");
            }
        }
    }
}

// Helper class สำหรับ parse JSON array
[System.Serializable]
public class LeaderboardWrapper
{
    public LeaderboardResponse[] items;
}
