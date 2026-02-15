using System.Collections;
using System.Collections.Generic;
using System;
using AIEngine.Adapters;
using AIEngine.Utilities;
using Game.Interfaces;
using TMPro;
using UnityEngine;
using static ChessPiece;

public class GameManager : MonoBehaviour
{
    [Header("private Game")]
    private ChessPiece.Team currentTurn = ChessPiece.Team.White;
    private ChessBoard chessBoard;
    private HistoryMove historyMove;
    private int lastAppliedMoveNumber = 0;

    [Header("Modules")]
    public AiController aiController;
    public GameModeManager gameModeManager;

    [Header("API Services")]
    public GameAPI gameAPI;
    public AiPerformanceAPI aiPerformanceApi;
    public MovesAPI movesApi;
    public MatchmakingApi matchmakingApi;
    public UserAPI userApi;

    [Header("Game Data")]
    public int currentGameId = -1;
    public int moveCount = 0;
    public int matchMode = 1; // 1 = ranked, 2 = normal

    [Header("Game State")]
    public static GameManager Instance;
    public Team CurrentTurn => currentTurn;
    public enum PlayerType { Human, AI }
    public PlayerType WhitePlayer = PlayerType.Human;
    public PlayerType BlackPlayer = PlayerType.AI;
    public string whitePlayerName = "White";
    public string blackPlayerName = "Black";
    public bool gameIsOver = false;
    public bool isGameStarted;
    public ChessPiece.Team myLocalTeam = ChessPiece.Team.None;

    public event Action<Team> OnTurnChanged;
    public event Action<Team, string> OnGameOver;
    public event Action<int> OnFiftyMoveCounterChanged;
    public event Action OnExitRequested;
    public event Action OnResignRequested;
    public event Action OnReplayRequested;
    public event Action<string, string> OnPlayerNamesChanged;
    public event Action OnMoveHistoryUpdated;
    public event Action<MoveResult> OnMoveCompleted;

    [System.Obsolete]
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            chessBoard = FindObjectOfType<ChessBoard>();
            historyMove = FindObjectOfType<HistoryMove>();
            if (gameModeManager == null)
                gameModeManager = GameModeManager.Instance ?? FindFirstObjectByType<GameModeManager>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (chessBoard == null) chessBoard = FindObjectOfType<ChessBoard>();
        if (chessBoard != null)
        {
            chessBoard.SetGameManager(this);
            chessBoard.OnMoveCompleted += HandleMoveCompleted;
            if (aiPerformanceApi == null) aiPerformanceApi = GetComponent<AiPerformanceAPI>() ?? gameObject.AddComponent<AiPerformanceAPI>();

        }
        else
        {
            Debug.LogError("❌ ChessBoard ไม่ถูกพบ!");
        }

        StartGameSession();
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseManager.isPaused || gameIsOver) return;
    }

    private void SetupOfflineModes()
    {
        currentTurn = Team.White;
        isGameStarted = true;
        UpdatePlayerTurnUI();
    }

    private void SetupLocalMultiplayerMode()
    {
        gameModeManager.SetMode(GameModeManager.GameModes.LocalMultiplayer);
        SetPlayerTypes(PlayerType.Human, PlayerType.Human);
        Debug.Log("[MODE] Local Multiplayer - Human vs Human");
    }

    private void ResetGameData()
    {
        currentGameId = -1;
        moveCount = 0;
        gameIsOver = false;

    }
    private void ResetInternalState()
    {
        gameIsOver = false;
        isGameStarted = false;
        currentGameId = -1;
        currentTurn = Team.White;
    }

    public ChessPiece.Team GetOpponentTeam(ChessPiece.Team team)
    {
        return (team == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
    }
    //set method
    public void SetPlayerNames(string whiteName, string blackName)
    {
        whitePlayerName = whiteName;
        blackPlayerName = blackName;
        OnPlayerNamesChanged?.Invoke(whitePlayerName, blackPlayerName);
        UpdatePlayerTurnUI(); // อัปเดต UI ทันที
    }
    public void SetGameId(int id) => currentGameId = id;


    public void SetPlayerTypes(PlayerType whiteType, PlayerType blackType)
    {
        WhitePlayer = whiteType;
        BlackPlayer = blackType;
    }

    private void SetupAI()
    {
        if (aiController == null) return;

        string level = gameModeManager.SelectedDifficulty.ToLower();
        if (level == "normal") level = "medium";

        string algorithmType = (level == "easy") ? "minimax" : "alpha_beta";

        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.AiLevel = level;
            PerformanceTracker.Instance.AlgorithmType = algorithmType;
            PerformanceTracker.Instance.GameId = currentGameId;
        }
    }

    private void StartGameSession()
    {
        Debug.Log("🚀 Starting New Game Session...");

        if (gameModeManager == null)
            gameModeManager = GameModeManager.Instance ?? FindFirstObjectByType<GameModeManager>();

        if (gameModeManager != null)
            gameModeManager.LoadFromPlayerPrefs();

        var mode = GetCurrentMode();

        if (mode == GameModeManager.GameModes.Online)
            return;

        PerformanceTracker.Instance?.ResetData();

        if (mode == GameModeManager.GameModes.SinglePlayer)
        {
            string aiColor = PlayerPrefs.GetString("AI_Color", "Black");
            bool aiIsWhite = aiColor.Equals("White", StringComparison.OrdinalIgnoreCase);
            SetPlayerTypes(aiIsWhite ? PlayerType.AI : PlayerType.Human,
                aiIsWhite ? PlayerType.Human : PlayerType.AI);
        }
        else if (mode == GameModeManager.GameModes.AIVsAI)
        {
            SetPlayerTypes(PlayerType.AI, PlayerType.AI);
        }
        else if (mode == GameModeManager.GameModes.LocalMultiplayer)
        {
            SetPlayerTypes(PlayerType.Human, PlayerType.Human);
        }

        currentTurn = Team.White;
        isGameStarted = true;
        UpdatePlayerTurnUI();

        if (WhitePlayer == PlayerType.AI || BlackPlayer == PlayerType.AI)
        {
            SetupAI();
            if (aiController != null)
                aiController.StartAI();
        }
    }

    public void SetGameStarted(bool value) => isGameStarted = value;
    public void SetTurn(Team team)
    {
        currentTurn = team;
        UpdatePlayerTurnUI();
    }
    public void SetLocalTeam(Team team) => myLocalTeam = team;
    public void SetMoveCount(int count) => moveCount = count;
    public void SetCurrentTurn(Team team)
    {
        currentTurn = team;
        UpdatePlayerTurnUI();
    }
    public void OnRestartClick()
    {
        RequestReplay();
    }
    //Get method
    public ChessPiece.Team GetCurrentTurn() => currentTurn;
    public string GetWhitePlayerName() => whitePlayerName;
    public string GetBlackPlayerName() => blackPlayerName;

    public string GetPlayerName(ChessPiece.Team team)
    {
        return (team == ChessPiece.Team.White) ? whitePlayerName : blackPlayerName;
    }

    public string GetCurrentPlayerName()
    {
        return (currentTurn == ChessPiece.Team.White) ? whitePlayerName : blackPlayerName;
    }

    public GameModeManager.GameModes GetCurrentMode()
    {
        return gameModeManager != null ? gameModeManager.CurrentMode : GameModeManager.GameModes.SinglePlayer;
    }

    public bool IsMyTurn()
    {
        if (gameModeManager == null || gameModeManager.CurrentMode != GameModeManager.GameModes.Online) return true;
        return currentTurn == myLocalTeam;
    }

    public void SwitchTurn()
    {
        if (gameIsOver) return;

        currentTurn = (currentTurn == Team.White) ? Team.Black : Team.White;
        UpdatePlayerTurnUI();
    }

    public void UpdatePlayerTurnUI()
    {
        OnTurnChanged?.Invoke(currentTurn);
        OnMoveHistoryUpdated?.Invoke();
    }

    public void UpdateFiftyMoveCounter(int count)
    {
        OnFiftyMoveCounterChanged?.Invoke(count);
    }

    public void ResetFiftyMoveUI()
    {
        OnFiftyMoveCounterChanged?.Invoke(0);
    }
    // ฟังก์ชันตรวจสอบ Checkmate หรือ Stalemate
    public void CheckGameState()
    {

        ChessPiece.Team opponentTeam = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
        if (ChessBoard.Instance.IsKingInCheckmate(currentTurn))
        {
            ChessPiece.Team winningTeam = GetOpponentTeam(currentTurn);
            Debug.Log($"♟️ Checkmate! {winningTeam} win!");

            return;
        }
        else if (ChessBoard.Instance.IsStalemate(currentTurn))
        {
            Debug.Log("⚖️ Stalemate! draw!");
            return;
        }
    }

    public void ResetGame()
    {
        gameIsOver = false;
        isGameStarted = false;
        currentTurn = Team.White;
        moveCount = 0;              // ต้องรีเซ็ตเพื่อให้ MoveNumber เริ่มที่ 1 ใหม่
        currentGameId = -1;         // ตัด Game ID เก่าทิ้ง

        // --- 5. รีเซ็ตกระดานและตัวหมาก ---
        if (chessBoard != null)
        {
            chessBoard.ResetBoard();
        }

        if (historyMove != null)
            historyMove.ClearHistory();

        Debug.Log("♟️ เกมถูกรีเซ็ตสมบูรณ์!");

        // --- 6. อัปเดต UI ให้กลับเป็นค่าเริ่มต้น ---
        ResetFiftyMoveUI();
        UpdatePlayerTurnUI();
    }

    public void ReturnMain()
    {
        OnExitRequested?.Invoke();
    }


    public void HandleGameOver(Team winningTeam, string endReason)
    {
        GameOver(winningTeam, endReason);
    }

    public void GameOver(Team winningTeam, string endReason)
    {
        if (gameIsOver) return;
        gameIsOver = true;
        Debug.Log($"🏁 Game Over → Winner: {winningTeam} | Reason: {endReason}");
        OnGameOver?.Invoke(winningTeam, endReason);
    }

    public void RequestResign()
    {
        OnResignRequested?.Invoke();
    }

    public void RequestReplay()
    {
        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameModes.Online)
        {
            Debug.LogWarning("❌ Replay is offline-only.");
            return;
        }

        if (OnReplayRequested != null)
        {
            OnReplayRequested.Invoke();
            return;
        }

        ResetGame();
    }

    public void OnExitGameClicked()
    {
        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameModes.Online)
        {
            Debug.LogWarning("❌ Online mode must use resign instead.");
            return;
        }

        OnExitRequested?.Invoke();
    }
    public void TrySelectPiece(ChessPiece piece)
    {
        if (piece == null || chessBoard == null)
            return;

        if (gameIsOver)
            return;

        ChessPiece selectedPiece = chessBoard.SelectedPiece;

        if (selectedPiece != null && piece.team != selectedPiece.team)
        {
            chessBoard.SelectPiece(piece);
            return;
        }

        if (gameModeManager != null && gameModeManager.CurrentMode == GameModeManager.GameModes.Online)
        {
            if (piece.team != myLocalTeam)
            {
                Debug.Log($"❌ คุณควบคุม {myLocalTeam} เท่านั้น");
                return;
            }
        }

        if (piece.team != currentTurn)
            return;

        chessBoard.SelectPiece(piece);
    }


    public bool IsLocalPlayer(Team team)
    {
        if (gameModeManager.CurrentMode != GameModeManager.GameModes.Online)
            return true;

        return team == myLocalTeam;
    }


    public void HandleExitGame()
    {
        if (!gameIsOver)
        {
            Debug.Log("⚠️ Exiting before game ended");
        }

        ResetInternalState();
    }

    private void HandleMoveCompleted(MoveResult result)
    {
        OnMoveCompleted?.Invoke(result);
    }

    public bool IsGameOver() => gameIsOver;

}
