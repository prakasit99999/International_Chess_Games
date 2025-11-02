
using AIEngine.Adapters;
using Game.Interfaces;
using System.Collections;
using TMPro;
using UnityEngine;
using static ChessPiece;

public class GameManager : MonoBehaviour
{
    private IChessAI chessAI;
    private Coroutine aiLoop;

    private bool gameIsOver = false;
    private bool isAITurnActive = false;

    private string difficultyWhite;
    private string difficultyBlack;
    private string aiColor;

    private GameModes currentMode = GameModes.SinglePlayer;

    private ChessPiece.Team currentTurn = ChessPiece.Team.White;

    private ChessBoard chessBoard;
    private HistoryMoveUI historyMoveUI;

    public static GameManager Instance;
    public  Team CurrentTurn => currentTurn;

    public enum PlayerType { Human, AI }
    public enum GameModes { LocalMultiplayer, SinglePlayer, AIVsAI, Online }
    public AIDifficulty aiDifficultyWhite;
    public AIDifficulty aiDifficultyBlack;

    public GameObject drawGamePanel;
    public GameObject winGamePanel;
    public GameObject loseGamePanel;
    public GameObject drawInfoPanel;

    public TMP_Text winTxt;
    public TMP_Text loseTxt;
    public TMP_Text drawTxt;
    public TMP_Text fiftyMoveText;
    public TMP_Text txtNameWhite;
    public TMP_Text txtNameBlack;

    public bool isGameStarted;
    public bool isWhiteTurn;
    public bool isBlackTurn;

    public AIDifficulty aiDifficulty;
    public PlayerType WhitePlayer = PlayerType.Human;
    public PlayerType BlackPlayer = PlayerType.AI;


    public string whitePlayerName = "White";
    public string blackPlayerName = "Black";

    public bool isAIMode = false;

    public GameManager(GameModes currentMode)
    {
        this.currentMode = currentMode;
    }

    [System.Obsolete]
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
        if (chessBoard == null)
        {
            Debug.LogError("❌ ChessBoard ไม่ถูกพบ! ตรวจสอบว่า ChessBoard อยู่ในฉาก");
        }
        else
        {
            chessBoard.SetGameManager(this);
            chessAI = GetComponent<UnityAIBoardAdapter>();
            if (chessAI == null)
            {
                chessAI = gameObject.AddComponent<UnityAIBoardAdapter>();
            }
            ModeSelect();

            // ถ้ามี AI ฝั่งใดฝั่งหนึ่ง → เริ่ม Coroutine
            if (WhitePlayer == PlayerType.AI || BlackPlayer == PlayerType.AI)
            {
                aiLoop = StartCoroutine(AIPlayLoop());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.isPaused || gameIsOver) return;
    }


    private IEnumerator AIPlayLoop()
    {
        while (!gameIsOver)
        {
            // รอ pause / promotion
            while (PauseManager.isPaused || gameIsOver || ChessBoard.Instance.IsPromoting())
                yield return null;

            if (IsCurrentPlayerAI())
            {
                var difficulty = (currentTurn == Team.White) ? aiDifficultyWhite : aiDifficultyBlack;

                // 🟢 ส่ง currentTurn ที่ถูกต้อง
                chessAI.StartCalculateMove(chessBoard, currentTurn, CurrentTurn, difficulty);

                yield return new WaitForSeconds(0.3f);

                var move = chessAI.GetCalculatedMove();
                if (move != null)
                {
                    ExecuteAIMove(move[0], move[1]);
                    chessAI.ClearCalculatedMove();

                    yield return new WaitForSeconds(0.5f);

                }
                else
                {
                    Debug.LogWarning("❌ AI ไม่สามารถหา move ได้");
                }
            }

            yield return null;
        }
    }

    private AIDifficulty ParseDifficulty(string diff)
    {
        switch (diff)
        {
            case "Easy": return AIDifficulty.Easy;
            case "Normal": return AIDifficulty.Normal;
            case "Hard": return AIDifficulty.Hard;
            default: return AIDifficulty.Easy;
        }
    }

    private bool IsCurrentPlayerAI()
    {
        return (currentTurn == Team.White && WhitePlayer == PlayerType.AI) ||
               (currentTurn == Team.Black && BlackPlayer == PlayerType.AI);
    }

    private void ExecuteAIMove(Vector2Int from, Vector2Int to)
    {
        if (!chessBoard.PiecesOnBoard.TryGetValue(from, out ChessPiece piece))
        {
            Debug.LogWarning($"❌ AI tried to move from {from}, but no piece found!");
            return;
        }

        if (piece.team != currentTurn)
        {
            Debug.LogWarning($"🚨 AI tried to move wrong team! Piece={piece.team}, CurrentTurn={currentTurn}");
            return;
        }

        chessBoard.SelectPiece(piece);
        chessBoard.MoveSelectedPiece(to);

        // ✅ log ก่อนสลับตา
        Debug.Log($"[CHECK] Piece={piece.team}, CurrentTurn={currentTurn}, From={from}, To={to}");

    }

    private void SetupAIVsAIMode()
    {
        currentMode = GameModes.AIVsAI;
        difficultyWhite = PlayerPrefs.GetString("AI_White_Difficulty", "Easy");
        difficultyBlack = PlayerPrefs.GetString("AI_Black_Difficulty", "Easy");
        SetPlayerTypes(PlayerType.AI, PlayerType.AI);
        SetAIDifficulty(difficultyWhite, difficultyBlack);
        Debug.Log($"[MODE] AI vs AI - White: {difficultyWhite}, Black: {difficultyBlack}");
    }

    private void SetupLocalMultiplayerMode()
    {
        currentMode = GameModes.LocalMultiplayer;
        SetPlayerTypes(PlayerType.Human, PlayerType.Human);
        Debug.Log("[MODE] Local Multiplayer - Human vs Human");
    }

    private void SetupSinglePlayerMode()
    {
        currentMode = GameModes.SinglePlayer;

        // ดึงระดับความยากจาก PlayerPrefs (ถ้าไม่มี default = Easy)
        string difficulty = PlayerPrefs.GetString("AI_Difficulty", "Easy");

        // กำหนดตายตัว: ขาว = Human, ดำ = AI
        SetPlayerTypes(PlayerType.Human, PlayerType.AI);

        // ตั้ง difficulty: ฝั่งขาว (Human) = None, ฝั่งดำ (AI) = ตามค่าที่เลือก
        aiDifficultyWhite = AIDifficulty.None; 
        aiDifficultyBlack = ParseDifficulty(difficulty);

        // ส่งค่าความยากไปให้ AI system
        SetAIDifficulty(aiDifficultyWhite.ToString(), aiDifficultyBlack.ToString());

        // ตั้งชื่อผู้เล่น
        SetPlayerNamesAndTypes();

        Debug.Log($"[MODE] Single Player - White: Human, Black: AI ({difficulty})");
    }

    private ChessPiece.Team GetOpponentTeam(ChessPiece.Team team)
    {
        return (team == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
    }

    //set method
    public void SetPlayerNamesAndTypes()
    {
        string whiteDisplayName = WhitePlayer == PlayerType.Human
         ? "Human (White)"
         : $"AI ({aiDifficultyWhite})";

        string blackDisplayName = BlackPlayer == PlayerType.Human
            ? "Human (Black)"
            : $"AI ({aiDifficultyBlack})";

        //Debug.Log($"whiteDisplayName:{whiteDisplayName} blackDisplayName: {blackDisplayName}");

        if (txtNameWhite != null)
            txtNameWhite.text = whiteDisplayName;

        if (txtNameBlack != null)
            txtNameBlack.text = blackDisplayName;
    }

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

    public void SetAIDifficulty(string whiteDiff, string blackDiff)
    {
        aiDifficultyWhite = ParseDifficulty(whiteDiff);
        aiDifficultyBlack = ParseDifficulty(blackDiff);
    }

    // ตั้งค่าประเภทผู้เล่น
    public void SetPlayerTypes(PlayerType whiteType, PlayerType blackType)
    {
        WhitePlayer = whiteType;
        BlackPlayer = blackType;
        SetPlayerNamesAndTypes();
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

    public GameModes GetCurrentMode()
    {
        return currentMode;
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

    // ฟังก์ชันตรวจสอบ Checkmate หรือ Stalemate
    public void CheckGameState()
    {

        ChessPiece.Team opponentTeam = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
        if (ChessBoard.Instance.IsKingInCheckmate(currentTurn))
        {
            ChessPiece.Team winningTeam = GetOpponentTeam(currentTurn);
            Debug.Log($"♟️ Checkmate! {winningTeam} win!");
            GameOver(winningTeam);
            return;
        }
        else if (ChessBoard.Instance.IsStalemate(currentTurn))
        {
            Debug.Log("⚖️ Stalemate! draw!");
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
            drawTxt.text = "game draw!";
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

    public void ResetGame()
    {
        gameIsOver = false;
        isGameStarted = false;
        isAITurnActive = false;
        currentTurn = Team.White;

        Debug.Log(historyMoveUI);

        winGamePanel.SetActive(false);
        loseGamePanel.SetActive(false);
        drawGamePanel.SetActive(false);
        drawInfoPanel.SetActive(false);

        if (chessBoard != null)
        {
            chessBoard.ResetBoard();
        }
        Debug.Log("♟️ เกมถูกรีเซ็ต!");
        if (historyMoveUI != null)
        {
            historyMoveUI.ClearHistory();
        }

        UpdatePlayerTurnUI();
        ResetFiftyMoveUI();
        UpdatePlayerTurnUI();
    }

    public void ReturnMain()
    {
        if (gameIsOver)
        {
            ResetGame();
        }
        // ปิด UI ทั้งหมด
        winGamePanel.SetActive(false);
        loseGamePanel.SetActive(false);
        drawGamePanel.SetActive(false);
        drawInfoPanel.SetActive(false);
        // รีเซ็ตสถานะเกม
        gameIsOver = false;
        isGameStarted = false;
        isAITurnActive = false;
        currentTurn = Team.White;
        // กลับไปยังเมนูหลัก
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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

    public void ModeSelect()
    {
        string modeStr = PlayerPrefs.GetString("Mode", "SinglePlayer");
        switch (modeStr)
        {
            case "AIVsAI":
                SetupAIVsAIMode();
                break;

            case "LocalMultiplayer":
                SetupLocalMultiplayerMode();
                break;

            case "SinglePlayer":
            default:
                SetupSinglePlayerMode();
                break;
        }

        isGameStarted = true;
        currentTurn = Team.White;
        UpdatePlayerTurnUI();
    }

    public bool IsGameOver() => gameIsOver;

}
