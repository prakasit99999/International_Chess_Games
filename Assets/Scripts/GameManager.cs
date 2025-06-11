using TMPro;
using UnityEngine;
using static ChessPiece;

public class GameManager : MonoBehaviour
{
    private bool gameIsOver = false;

    private ChessPiece.Team currentTurn = ChessPiece.Team.White;

    private ChessBoard chessBoard;
    private HistoryMoveUI historyMoveUI;
    private PromotionManager promotionManager;

    public static GameManager Instance;

    public GameObject drawGamePanel;
    public GameObject winGamePanel;
    public GameObject loseGamePanel;
    public GameObject drawInfoPanel;

    public TMP_Text winTxt;
    public TMP_Text loseTxt;
    public TMP_Text drawTxt;
    public TMP_Text fiftyMoveText;

    public bool isGameStarted;
    public bool isWhiteTurn;
    public bool isBlackTurn;

    public string whitePlayerName = "White";
    public string blackPlayerName = "Black";
    public bool isAIMode = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            chessBoard = FindObjectOfType<ChessBoard>();
            historyMoveUI = FindObjectOfType<HistoryMoveUI>();
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
        historyMoveUI = FindObjectOfType<HistoryMoveUI>(); 
        if (chessBoard == null)
        {
            Debug.LogError("❌ ChessBoard ไม่ถูกพบ! ตรวจสอบว่า ChessBoard อยู่ในฉาก");
        }
        else
        {
            chessBoard.SetGameManager(this); 
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

    /*Set*/
    public void SetCurrentTurn(Team team)
    {
        currentTurn = team;
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

    private ChessPiece.Team GetOpponentTeam(ChessPiece.Team team)
    {
        return (team == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
    }

    // ฟังก์ชันตรวจสอบ Checkmate หรือ Stalemate
    public void CheckGameState()
    {

        ChessPiece.Team opponentTeam = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
        if (ChessBoard.Instance.IsKingInCheckmate(currentTurn))
        {
            ChessPiece.Team winningTeam = GetOpponentTeam(currentTurn);
            Debug.Log($"♟️ Checkmate! {winningTeam} ชนะเกม!");
            GameOver(winningTeam);
            return;
        }
        else if (ChessBoard.Instance.IsStalemate(currentTurn))
        {
            Debug.Log("⚖️ Stalemate! เกมเสมอ!");
            GameOver(ChessPiece.Team.None);
            return;
        }
    }

    public void GameOver(ChessPiece.Team winningTeam)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        // ปิดทุก UI ก่อน
        winGamePanel.SetActive(false);
        loseGamePanel.SetActive(false);
        drawGamePanel.SetActive(false);
        drawInfoPanel.SetActive(false); // แสดง counter ก็ปิด

        Debug.Log($"🎉 เกมจบแล้ว! {(winningTeam == Team.None ? "เสมอ" : $"{winningTeam} ชนะ")}!");

        if (winningTeam == Team.None)
        {
            drawGamePanel.SetActive(true);
            drawTxt.text = "⚖️ เกมเสมอ!";
        }
        else
        {
            bool isLocalPlayerWinner = (winningTeam == GetCurrentTurn());
            if (isLocalPlayerWinner)
            {
                winGamePanel.SetActive(true);
                winTxt.text = $"{GetCurrentPlayerName()} ชนะ!";
            }
            else
            {
                loseGamePanel.SetActive(true);
                loseTxt.text = $"{GetCurrentPlayerName()} แพ้!";
            }
        }

        Time.timeScale = 0;

        // ปิดการทำงานของหมากทั้งหมด
        if (chessBoard != null)
        {
            foreach (var entry in chessBoard.GetPiecesOnBoard())
            {
                entry.Value.enabled = false;
            }
        }
    }

    public bool IsGameOver()
    {
        return gameIsOver;
    }

    public void UpdateFiftyMoveCounter(int count)
    {
        if (drawInfoPanel == null || fiftyMoveText == null) return;
        if (count >= 30)
        {
            drawInfoPanel.SetActive(true);
            fiftyMoveText.text = $"📏 กฎ 50 เดิน: {count}/50";

            if (count >= 48)
                fiftyMoveText.color = Color.red;
            else if (count >= 45)
                fiftyMoveText.color = new Color(1f, 0.5f, 0f);
            else
                fiftyMoveText.color = Color.white;
        }
        else
        {
            drawInfoPanel.SetActive(false);
        }
    }

    public void ResetFiftyMoveUI()
    {
        if (drawInfoPanel != null)
        {
            drawInfoPanel.SetActive(false);
        }
    }

}
