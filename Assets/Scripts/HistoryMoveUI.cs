using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HistoryMoveUI : MonoBehaviour
{
    private GameManager gameManager;
    private HistoryMove historyMove;

    public TMP_Text playerTurnText;       // Player_txt (แยกจาก Scroll View)
    public Transform contentParent; // ✅ drag Content ของ Scroll View
    public GameObject moveEntryPrefab;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }
        else if (historyMove != null)
        {
            Debug.LogError("❌ HistoryMove ไม่ถูกพบ! ตรวจสอบว่า HistoryMove อยู่ในฉาก");
        }
        else
        {

            Debug.LogError("❌ GameManager ไม่ถูกพบ! ตรวจสอบว่า GameManager อยู่ในฉาก");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // ค้นหา HistoryMove
        historyMove = FindObjectOfType<HistoryMove>();
        if (historyMove == null)
        {
            Debug.LogError("❌ HistoryMove ไม่ถูกพบใน Scene!");
            return;
        }

        // ตรวจสอบ GameManager
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("❌ GameManager.Instance เป็น Null!");
            return;
        }

        // ตรวจสอบ Content และ Prefab
        if (contentParent == null)
        {
            Debug.LogError("❌ Content Parent ไม่ถูกกำหนด!");
            return;
        }
        if (moveEntryPrefab == null)
        {
            Debug.LogError("❌ Move Entry Prefab ไม่ถูกกำหนด!");
            return;
        }

        // อัปเดต UI
        UpdatePlayerTurn();
        UpdateMoveHistoryList();
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

    // อัปเดตรายการเดินหมาก
    public void UpdateMoveHistoryList()
    {
        // ตรวจสอบ null
        if (contentParent == null || moveEntryPrefab == null)
        {
            Debug.LogError("❌ Content Parent หรือ Move Entry Prefab ไม่ถูกกำหนด!");
            return;
        }

        // ลบรายการเดิมทั้งหมด
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // ดึงประวัติการเดิน
        Stack<HistoryMove.HistoryMoveData> moves = historyMove.GetMoveHistory();
        List<HistoryMove.HistoryMoveData> moveList = new List<HistoryMove.HistoryMoveData>(moves);
        moveList.Reverse();

        // สร้างรายการใหม่
        for (int i = 0; i < moveList.Count; i++)
        {
            GameObject entry = Instantiate(moveEntryPrefab, contentParent);
            TMP_Text entryText = entry.GetComponent<TMP_Text>();

            string startPos = ConvertToChessNotation(moveList[i].startPosition);
            string endPos = ConvertToChessNotation(moveList[i].endPosition);

            entryText.text = $"{i + 1}. {startPos} → {endPos}";
        }
    }


    // แปลง Vector2Int เป็น Chess Notation
    private string ConvertToChessNotation(Vector2Int position)
    {
        char column = (char)('a' + position.x);
        int row = position.y + 1;
        return $"{column}{row}";
    }
}
