using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// จัดการแสดงผล Leaderboard ทั้งหมด
/// แนบกับ RankPanel หรือ parent ของ Scroll View
public class LeaderboardManager : MonoBehaviour
{
    [Header("References")]
    public LeaderboardApi leaderboardApi;
    public ScrollRect scrollRect;
    public Transform contentParent; // Content ของ Scroll View
    public GameObject rowPrefab;    // Prefab ของแต่ละแถว (มี LeaderboardRowUI)

    private List<GameObject> spawnedRows = new List<GameObject>();

    private void Start()
    {
        LoadLeaderboard();
        StartCoroutine(AutoRefreshLeaderboard());
    }

    /// โหลดข้อมูล Leaderboard จาก API
    public void LoadLeaderboard()
    {
        if (leaderboardApi == null)
        {
            leaderboardApi = LeaderboardApi.Instance;
        }

        if (leaderboardApi != null)
        {
            StartCoroutine(leaderboardApi.GetLeaderboard(OnLeaderboardLoaded, OnLeaderboardError));
        }
        else
        {
            Debug.LogError("LeaderboardApi not found!");
        }
    }

    /// Callback เมื่อโหลดข้อมูลสำเร็จ
    private void OnLeaderboardLoaded(LeaderboardResponse[] data)
    {
        Debug.Log($"🔄 OnLeaderboardLoaded called with {data.Length} items");
        Debug.Log($"   rowPrefab: {(rowPrefab != null ? "OK" : "NULL")}");
        Debug.Log($"   contentParent: {(contentParent != null ? contentParent.name : "NULL")}");

        ClearRows();

        foreach (var item in data)
        {
            SpawnRow(item);
        }

        Debug.Log($"✅ Loaded {data.Length} leaderboard entries, spawnedRows count: {spawnedRows.Count}");
    }

    /// Callback เมื่อโหลดข้อมูลผิดพลาด
    private void OnLeaderboardError(string error)
    {
        Debug.LogError($"❌ Leaderboard Error: {error}");
    }

    /// สร้างแถวใหม่และเพิ่มลง Scroll View
    private void SpawnRow(LeaderboardResponse data)
    {
        if (rowPrefab == null)
        {
            Debug.LogError("❌ rowPrefab is NULL! ลาก Prefab ลง Inspector");
            return;
        }
        if (contentParent == null)
        {
            Debug.LogError("❌ contentParent is NULL! ลาก Content ลง Inspector");
            return;
        }

        GameObject row = Instantiate(rowPrefab, contentParent);
        Debug.Log($"   🆕 Spawned row: {row.name} under {contentParent.name}");

        LeaderboardRowUI rowUI = row.GetComponent<LeaderboardRowUI>();

        if (rowUI != null)
        {
            rowUI.SetData(data);
            Debug.Log($"   ✅ SetData for: {data.username}");
        }
        else
        {
            Debug.LogError("❌ rowPrefab ไม่มี LeaderboardRowUI component!");
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

    /// Refresh ข้อมูล Leaderboard
    public void RefreshLeaderboard()
    {
        LoadLeaderboard();
    }

    private System.Collections.IEnumerator AutoRefreshLeaderboard()
    {
        while (true)
        {
            yield return new WaitForSeconds(20f); // Refresh every 20 seconds
            LoadLeaderboard();
        }
    }
}
