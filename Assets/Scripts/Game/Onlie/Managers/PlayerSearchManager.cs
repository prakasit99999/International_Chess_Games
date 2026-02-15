using System.Collections.Generic;
using Project.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// จัดการ Panel ค้นหาผู้เล่น
public class PlayerSearchManager : MonoBehaviour
{
    [Header("References")]
    public UserAPI userApi;
    public TMP_InputField searchInput;
    public Button searchButton;
    public Transform contentParent; // Content ของ Scroll View
    public GameObject playerRowPrefab;

    private List<GameObject> spawnedRows = new List<GameObject>();
    private string currentUsername = "";

    private void Start()
    {
        if (userApi == null) userApi = UserAPI.Instance;

        // ผูก Event กับปุ่มค้นหา
        if (searchButton != null)
        {
            searchButton.onClick.AddListener(OnSearchClicked);
        }

        StartCoroutine(GetUserProfile());
        StartCoroutine(AutoRefreshRoutine()); // Start polling for updates
    }

    private System.Collections.IEnumerator GetUserProfile()
    {
        string url = "http://localhost:8080/api/User/profile";
        string token = SessionManager.Instance.Token;

        if (string.IsNullOrEmpty(token)) yield break;

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Authorization", "Bearer " + token);
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var profile = JsonUtility.FromJson<ProfileResponse>(www.downloadHandler.text);
                if (profile != null)
                {
                    currentUsername = profile.username;
                    Debug.Log($"👤 Current User: {currentUsername}");

                    OnGetAllUsersClicked();
                }
            }
        }
    }

    /// เมื่อกดปุ่มค้นหา
    public void OnSearchClicked()
    {
        string query = searchInput != null ? searchInput.text.Trim() : "";

        if (string.IsNullOrEmpty(query))
        {
            Debug.Log("⚠️ กรุณาใส่ชื่อผู้เล่นที่ต้องการค้นหา");
            return;
        }

        Debug.Log($"🔍 ค้นหา: {query}");
        StartCoroutine(userApi.SearchUsers(query, OnSearchSuccess, OnSearchError));
    }

    /// เมื่อกดปุ่มแสดงทั้งหมด
    public void OnGetAllUsersClicked()
    {
        if (userApi == null)
        {
            Debug.LogError("UserAPI not found!");
            return;
        }

        Debug.Log("📋 กำลังโหลดรายชื่อผู้เล่นทั้งหมด...");
        StartCoroutine(userApi.GetAllUsers(OnSearchSuccess, OnSearchError));
    }

    private int currentUserId = 0; // เก็บ ID ตัวเอง
    public MatchmakingApi matchmakingApi; // Reference

    private void OnSearchSuccess(UserListDto[] results)
    {
        ClearRows();

        int count = 0;
        foreach (var player in results)
        {
            if (!string.IsNullOrEmpty(currentUsername) && player.username == currentUsername)
            {
                currentUserId = player.UserId;
                Debug.Log($"🆔 Captured Current User ID: {currentUserId}");
                continue;
            }

            if (player.status != "online")
            {
                continue;
            }

            SpawnRow(player);
            count++;
        }

        // Debug.Log($"✅ พบผู้เล่น {results.Length} คน (แสดง {count} คน - ซ่อนตัวเอง)");
    }

    private void OnSearchError(string error)
    {
        Debug.LogError($"❌ Search Error: {error}");
    }

    private void SpawnRow(UserListDto data)
    {
        if (playerRowPrefab == null || contentParent == null)
        {
            Debug.LogError("❌ playerRowPrefab หรือ contentParent ไม่ได้กำหนด!");
            return;
        }

        GameObject row = Instantiate(playerRowPrefab, contentParent);
        PlayerRowUI rowUI = row.GetComponent<PlayerRowUI>();

        if (rowUI != null)
        {
            rowUI.SetData(data, this);
        }
        else
        {
            Debug.LogError("❌ Prefab ไม่มี PlayerRowUI component!");
        }

        spawnedRows.Add(row);
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null) Destroy(row);
        }
        spawnedRows.Clear();
    }

    private System.Collections.IEnumerator AutoRefreshRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Refresh every 5 seconds

            string query = searchInput != null ? searchInput.text.Trim() : "";
            if (string.IsNullOrEmpty(query))
            {
                OnGetAllUsersClicked();
            }
        }
    }

}
