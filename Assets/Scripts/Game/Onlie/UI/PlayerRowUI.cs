using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Script สำหรับ Player Row ในผลการค้นหา
public class PlayerRowUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text usernameText;
    public TMP_Text statusText;
    public Button inviteButton;

    private PlayerSearchDto playerData;
    private PlayerSearchManager searchManager;

    /// ตั้งค่าข้อมูลสำหรับแถวนี้
    public void SetData(PlayerSearchDto data, PlayerSearchManager manager)
    {
        playerData = data;
        searchManager = manager;

        if (usernameText != null) usernameText.text = data.username;
        if (statusText != null)
        {
            statusText.text = data.status;
            // เปลี่ยนสีตามสถานะ
            if (data.status == "online")
            {
                statusText.color = Color.green;
            }
            else if (data.status == "playing")
            {
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.color = Color.gray;
            }
        }
        Debug.Log($"usernameText: {(usernameText != null ? usernameText.text : "null")} | statusText: {(statusText != null ? statusText.text : "null")}");

        // ซ่อนปุ่มเชิญถ้าผู้เล่นไม่ online
        if (inviteButton != null)
        {
            inviteButton.interactable = (data.status == "online");
            inviteButton.onClick.RemoveAllListeners();
            inviteButton.onClick.AddListener(OnInviteClicked);
        }
    }

    private void OnInviteClicked()
    {
        if (InviteManager.Instance != null && playerData != null)
        {
            InviteManager.Instance.SendInvite(playerData.UserId, playerData.username);
        }
        else
        {
            Debug.LogError("InviteManager Instance not found or playerData is null");
        }
    }
}
