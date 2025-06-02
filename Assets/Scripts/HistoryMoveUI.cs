using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HistoryMoveUI : MonoBehaviour
{
    private GameManager gameManager;
    private HistoryMove historyMove;
    private List<HistoryMove> historyMoves;
    public GameObject moveEntryPrefab;

    public static HistoryMoveUI Instance;
    public TMP_Text playerTurnText;      
    public Transform contentParent; 
    public ScrollRect scrollRect; // กำหนดใน Inspector



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // ✅ กำหนด Instance ให้อ้างอิงตัวเอง
            historyMove = FindObjectOfType<HistoryMove>();
            gameManager = FindObjectOfType<GameManager>();

        }
        else
        {
            Destroy(gameObject);
        }

        if ((gameManager = GameManager.Instance) == null)
            Debug.LogError("❌ GameManager ไม่ถูกพบ! ตรวจสอบว่า GameManager อยู่ในฉาก");

        if ((historyMove = FindObjectOfType<HistoryMove>()) == null)
            Debug.LogError("❌ HistoryMove ไม่ถูกพบ! ตรวจสอบว่า HistoryMove อยู่ในฉาก");
    }

    // Start is called before the first frame update
    void Start()
    {
        // ค้นหา HistoryMove

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
        //Debug.Log($"🎯 อัปเดตชื่อผู้เล่น: {currentName}");
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
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = Vector2.zero;
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

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = Vector2.zero;
        }
        // สร้างรายการใหม่
        for (int i = 0; i < moveList.Count; i++)
        {
            GameObject entry = Instantiate(moveEntryPrefab, contentParent);
            TMP_Text entryText = entry.GetComponent<TMP_Text>();

            string startPos = ConvertToChessNotation(moveList[i].startPosition);
            string endPos = ConvertToChessNotation(moveList[i].endPosition);

            entryText.text = $"{i + 1}. {startPos} → {endPos}";

            // เพิ่ม Button และตั้งค่า Event
            Button button = entry.GetComponent<Button>();
            if (button == null)
            {
                button = entry.AddComponent<Button>();
            }
            int currentIndex = i; // เก็บ index ปัจจุบัน
            button.onClick.AddListener(() => OnMoveEntryClicked(currentIndex));

        }
    }

    private void OnMoveEntryClicked(int clickedIndex)
    {
        // ตรวจสอบการอ้างอิงทั้งหมด
        if (historyMove == null)
        {
            Debug.LogError("❌ historyMove ไม่ถูกกำหนด!");
            return;
        }
        if (UndoMove.Instance == null)
        {
            Debug.LogError("❌ UndoMove.Instance ไม่ถูกกำหนด!");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance ไม่ถูกกำหนด!");
            return;
        }

        int totalMoves = historyMove.GetMoveHistory().Count;
        if (clickedIndex < 0 || clickedIndex >= totalMoves)
        {
            Debug.LogError($"❌ Index {clickedIndex} ไม่ถูกต้อง (ทั้งหมด {totalMoves} การเดิน)");
            return;
        }

        int movesToUndo = totalMoves - clickedIndex - 1;
        for (int i = 0; i < movesToUndo; i++)
        {
            UndoMove.Instance.UndoLastMove();
        }

        // อัปเดต UI
        UpdateMoveHistoryList();
        GameManager.Instance.UpdatePlayerTurnUI();
    }

    // แปลง Vector2Int เป็น Chess Notation
    private string ConvertToChessNotation(Vector2Int position)
    {
        char column = (char)('a' + position.x);
        int row = position.y + 1;
        return $"{column}{row}";
    }
}
