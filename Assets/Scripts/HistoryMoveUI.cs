using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HistoryMoveUI : MonoBehaviour
{
    private GameManager gameManager;
    public TMP_Text playerTurnText;       // Player_txt (แยกจาก Scroll View)

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.Instance;
        UpdatePlayerTurn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ฟังก์ชันแยก: อัปเดตชื่อผู้เล่นปัจจุบัน
    public void UpdatePlayerTurn()
    {
        // ตรวจสอบ null เพื่อป้องกัน error
        if (gameManager == null || playerTurnText == null) return;
        string currentName = gameManager.GetCurrentPlayerName();
        Debug.Log($"🎯 อัปเดตชื่อผู้เล่น: {currentName}");
        playerTurnText.text = $"{gameManager.GetCurrentPlayerName()}'s Turn";
    }
}
