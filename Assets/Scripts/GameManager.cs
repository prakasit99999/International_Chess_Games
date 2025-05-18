using UnityEngine;
using UnityEngine.UI;
using static ChessPiece;

public class GameManager : MonoBehaviour
{
    private bool gameIsOver = false;
    private ChessBoard chessBoard;
    private HistoryMoveUI historyMoveUI; // เปลี่ยนจาก MoveHistoryUI เป็น HistoryMoveUI
    private PromotionManager promotionManager; // เชื่อมกับ PromotionManager ใน Inspector
    private ChessPiece.Team currentTurn = ChessPiece.Team.White;

    public static GameManager Instance;
    public GameObject gameOverPanel;
    public Text gameOverText;
    public bool isGameStarted;
    public bool isWhiteTurn;
    public bool isBlackTurn;

    public string whitePlayerName = "White";
    public string blackPlayerName = "Black";
    public bool isAIMode = false; // โหมดเล่นกับ AI


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            chessBoard = FindObjectOfType<ChessBoard>();
            historyMoveUI = FindObjectOfType<HistoryMoveUI>(); // ค้นหา HistoryMoveUI ใน Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        chessBoard = FindObjectOfType<ChessBoard>();
        historyMoveUI = FindObjectOfType<HistoryMoveUI>(); // ค้นหา HistoryMoveUI ใน Scene

        if (chessBoard == null)
        {
            Debug.LogError("❌ ChessBoard ไม่ถูกพบ! ตรวจสอบว่า ChessBoard อยู่ในฉาก");
        }
        else
        {
            chessBoard.SetGameManager(this); // Set the gameManager instance in ChessBoard
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    //set method
    public void SetGameStarted(bool value)
    {
        isGameStarted = value;
    }

    public void SetPlayerNames(string whiteName, string blackName)
    {
        whitePlayerName = whiteName;
        blackPlayerName = blackName;
        UpdatePlayerTurnUI(); // อัปเดต UI ทันที
    }

    //Get method
    // ตรวจสอบว่าตอนนี้เป็นตาของทีมไหน
    public ChessPiece.Team GetCurrentTurn()
    {
        return currentTurn;
    }

    public string GetCurrentPlayerName()
    {
        return (currentTurn == ChessPiece.Team.White) ? whitePlayerName : blackPlayerName;
    }

    // อัปเดต UI เมื่อเปลี่ยนตาเดิน
    public void UpdatePlayerTurnUI()
    {
        if (HistoryMoveUI.Instance != null)
        {
            HistoryMoveUI.Instance.UpdatePlayerTurn();
            HistoryMoveUI.Instance.UpdateMoveHistoryList();
        }
        else
        {
            Debug.LogWarning("HistoryMoveUI not found!");
        }
    }

    // ฟังก์ชันสลับเทิร์น
    public void SwitchTurn(bool forceSwitch = false)
    {
        if (IsGameOver())
        {
            Debug.Log("เกมจบแล้ว ไม่สามารถสลับเทิร์นได้");
            return;
        }

        // Remove the promotion check to allow forced turn switch
        if (!forceSwitch && ChessBoard.Instance.IsPromoting())
        {
            Debug.Log("กำลังเลื่อนขั้น ไม่สามารถสลับเทิร์นได้");
            return;
        }

        currentTurn = (currentTurn == Team.White) ? Team.Black : Team.White;
        UpdatePlayerTurnUI();
        CheckGameState();
    }


    // ฟังก์ชันตรวจสอบ Checkmate หรือ Stalemate
    public void CheckGameState()
    {
        //Debug.Log("🔍 ตรวจสอบสถานะเกม...");

        ChessPiece.Team opponentTeam = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;

        if (ChessBoard.Instance.IsKingInCheckmate(opponentTeam))
        {
            Debug.Log($"♟️ Checkmate! {currentTurn} ชนะเกม!");
            GameOver(currentTurn);
            return;
        }
        else if (ChessBoard.Instance.IsStalemate(opponentTeam))
        {
            Debug.Log("⚖️ Stalemate! เกมเสมอ!");
            GameOver(ChessPiece.Team.None); // ใช้ทีม "None" เพื่อบอกว่าเกมเสมอ
            return;
        }
    }


    public void GameOver(ChessPiece.Team winningTeam)
    {
        if (gameIsOver) return;

        gameIsOver = true;
        Debug.Log($"🎉 เกมจบแล้ว! {winningTeam} เป็นฝ่ายชนะ!");

        // แสดง UI Game Over
        gameOverPanel.SetActive(true);
        gameOverText.text = (winningTeam == Team.None)
            ? "⚖️ เกมเสมอ!"
            : $"🎉 {winningTeam} ชนะ!";

        // หยุดเวลาในเกม (หยุดการเคลื่อนไหว)
        Time.timeScale = 0;

        // ปิดการทำงานของตัวหมากทั้งหมด
        if (chessBoard != null)
        {
            foreach (var entry in chessBoard.GetPiecesOnBoard())
            {
                if (entry.Value != null)
                {
                    entry.Value.enabled = false;
                }
            }
        }
        else
        {
            Debug.LogError("❌ ChessBoard เป็น null! ตรวจสอบว่า ChessBoard อยู่ในฉาก");
        }
    }

    public bool IsGameOver()
    {
        return gameIsOver;
    }


}
