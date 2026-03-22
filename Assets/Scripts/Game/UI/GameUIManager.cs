using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ChessPiece;

public class GameUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject drawGamePanel;
    public GameObject winGamePanel;
    public GameObject loseGamePanel;
    public GameObject drawInfoPanel;

    [Header("Texts")]
    public TMP_Text winTxt;
    public TMP_Text loseTxt;
    public TMP_Text drawTxt;
    public TMP_Text fiftyMoveText;
    public TMP_Text txtNameWhite;
    public TMP_Text txtNameBlack;

    private GameManager gameManager;
    private Coroutine fiftyMoveCoroutine;
    private bool isProcessingExit = false;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            Debug.LogError("❌ GameManager not found in scene.");
    }

    private void OnEnable()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        // Ensure input isn't locked if Pause state leaked across scenes
        if (PauseManager.isPaused)
            PauseManager.Resume();

        if (gameManager == null)
            return;
        Debug.Log($"Pause: {PauseManager.isPaused}");

        gameManager.OnGameOver += HandleGameOver;
        gameManager.OnFiftyMoveCounterChanged += UpdateFiftyMoveCounter;
        gameManager.OnPlayerNamesChanged += HandlePlayerNamesChanged;
    }

    private void OnDisable()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameOver -= HandleGameOver;
        gameManager.OnFiftyMoveCounterChanged -= UpdateFiftyMoveCounter;
        gameManager.OnPlayerNamesChanged -= HandlePlayerNamesChanged;
    }

    private void Start()
    {
        SetPlayerNames();
    }

    public void SetPlayerNames()
    {
        if (gameManager == null) return;

        string whiteDisplayName = "White";
        string blackDisplayName = "Black";

        var modeManager = GameModeManager.Instance ?? gameManager.gameModeManager;
        if (modeManager != null)
            modeManager.LoadFromPlayerPrefs();

        var mode = modeManager != null
            ? modeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;

        Debug.Log($"🎮 Game mode: {mode}");
        string localName = PlayerPrefs.GetString("PlayerName", "Human");
        string whiteDiff = string.Empty;
        string blackDiff = string.Empty;

        if (modeManager != null)
        {
            if (mode == GameModeManager.GameModes.AIVsAI)
            {
                whiteDiff = FormatDifficulty(modeManager.SelectedWhiteDifficulty);
                blackDiff = FormatDifficulty(modeManager.SelectedBlackDifficulty);
            }
            else if (mode == GameModeManager.GameModes.SinglePlayer)
            {
                string singleDiff = FormatDifficulty(modeManager.SelectedDifficulty);
                if (gameManager.WhitePlayer == GameManager.PlayerType.AI)
                    whiteDiff = singleDiff;
                if (gameManager.BlackPlayer == GameManager.PlayerType.AI)
                    blackDiff = singleDiff;
            }
        }

        switch (mode)
        {
            case GameModeManager.GameModes.AIVsAI:
                whiteDisplayName = string.IsNullOrEmpty(whiteDiff) ? "AI (White)" : $"AI ({whiteDiff}) (White)";
                blackDisplayName = string.IsNullOrEmpty(blackDiff) ? "AI (Black)" : $"AI ({blackDiff}) (Black)";
                break;

            case GameModeManager.GameModes.SinglePlayer:
                whiteDisplayName = "Human (White)";
                blackDisplayName = string.IsNullOrEmpty(blackDiff) ? "AI (Black)" : $"AI ({blackDiff}) (Black)";
                break;

            case GameModeManager.GameModes.LocalMultiplayer:
                whiteDisplayName = "Human (White)";
                blackDisplayName = "Human (Black)";
                break;

            case GameModeManager.GameModes.Online:
                whiteDisplayName = gameManager.GetWhitePlayerName();
                blackDisplayName = gameManager.GetBlackPlayerName();
                break;
        }

        if (txtNameWhite) txtNameWhite.text = whiteDisplayName;
        if (txtNameBlack) txtNameBlack.text = blackDisplayName;
    }

    private string FormatDifficulty(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        string v = value.Trim().ToLowerInvariant();
        if (v == "normal") v = "medium";

        return v switch
        {
            "hard" => "Hard",
            "medium" => "Normal",
            "easy" => "Easy",
            _ => value
        };
    }


    public void ShowGameOver(Team winningTeam, string endReason)
    {
        if (gameManager == null)
            return;

        HideAllPanels();

        if (winningTeam == Team.None)
            ShowDraw();
        else
            ShowWinLose(winningTeam);
    }

    private void ShowDraw()
    {
        if (!drawGamePanel) return;

        drawGamePanel.SetActive(true);
        if (drawTxt)
            drawTxt.text = "Game Draw!";
    }

    private void ShowWinLose(Team winningTeam)
    {
        bool localWon = IsLocalWinner(winningTeam);
        string winnerName = gameManager.GetPlayerName(winningTeam);
        Team losingTeam = winningTeam == Team.White ? Team.Black : Team.White;
        string loserName = gameManager.GetPlayerName(losingTeam);
        var mode = gameManager.gameModeManager != null
            ? gameManager.gameModeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;
        bool isOnline = mode == GameModeManager.GameModes.Online;

        if (localWon)
        {
            if (winGamePanel)
            {
                winGamePanel.SetActive(true);
                if (winTxt)
                    winTxt.text = $"{winnerName} Win!";
            }
        }
        else
        {
            if (loseGamePanel)
            {
                loseGamePanel.SetActive(true);
                if (loseTxt)
                    loseTxt.text = isOnline ? $"{loserName} Lose!" : $"{winnerName} Win!";
            }
        }
    }

    private bool IsLocalWinner(Team winningTeam)
    {
        if (gameManager == null || gameManager.gameModeManager == null)
            return true;

        var mode = gameManager.gameModeManager != null
            ? gameManager.gameModeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;

        if (mode == GameModeManager.GameModes.Online)
            return winningTeam == gameManager.myLocalTeam;

        if (mode == GameModeManager.GameModes.SinglePlayer)
        {
            if (gameManager.WhitePlayer == GameManager.PlayerType.Human &&
                gameManager.BlackPlayer == GameManager.PlayerType.AI)
                return winningTeam == Team.White;

            if (gameManager.BlackPlayer == GameManager.PlayerType.Human &&
                gameManager.WhitePlayer == GameManager.PlayerType.AI)
                return winningTeam == Team.Black;
        }

        // LocalMultiplayer / AIVsAI -> show generic win panel
        return true;
    }

    private void HideAllPanels()
    {
        if (winGamePanel) winGamePanel.SetActive(false);
        if (loseGamePanel) loseGamePanel.SetActive(false);
        if (drawGamePanel) drawGamePanel.SetActive(false);
        if (drawInfoPanel) drawInfoPanel.SetActive(false);
    }

    public void UpdateFiftyMoveCounter(int count)
    {
        if (!drawInfoPanel || !fiftyMoveText)
            return;

        if (count < 30)
        {
            drawInfoPanel.SetActive(false);
            return;
        }

        drawInfoPanel.SetActive(true);
        fiftyMoveText.text = $"50-Move Rule: {count}/50";

        if (count >= 48)
            fiftyMoveText.color = Color.red;
        else if (count >= 45)
            fiftyMoveText.color = new Color(1f, 0.5f, 0f);
        else
            fiftyMoveText.color = Color.white;

        if (fiftyMoveCoroutine != null)
            StopCoroutine(fiftyMoveCoroutine);

        fiftyMoveCoroutine = StartCoroutine(AutoHideFiftyMovePanel(0.3f));
    }

    private IEnumerator AutoHideFiftyMovePanel(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (drawInfoPanel)
            drawInfoPanel.SetActive(false);

        fiftyMoveCoroutine = null;
    }

    // Online → resign (อาจหักคะแนน)
    public void OnGiveUpClicked()
    {
        if (isProcessingExit || gameManager == null)
            return;

        if (gameManager.IsGameOver())
        {
            Debug.LogWarning("Resign skipped: game already over.");
            return;
        }

        isProcessingExit = true;

        gameManager.RequestResign();
    }

    // Local Exit Only
    public void OnExitGameClicked()
    {
        if (isProcessingExit || gameManager == null)
            return;

        var mode = gameManager.gameModeManager.CurrentMode;

        if (mode == GameModeManager.GameModes.Online)
        {
            Debug.LogWarning("❌ Online mode must use resign instead.");
            return;
        }

        if (PauseManager.isPaused)
            PauseManager.Resume();

        isProcessingExit = true;

        gameManager.OnExitGameClicked();
    }

    // Local Reset Only
    public void OnResetGameClicked()
    {
        if (gameManager == null)
            return;

        var mode = gameManager.gameModeManager != null
            ? gameManager.gameModeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;

        if (mode == GameModeManager.GameModes.Online)
        {
            Debug.LogWarning("❌ Reset is offline-only.");
            return;
        }

        if (PauseManager.isPaused)
            PauseManager.Resume();

        gameManager.RequestReplay();
    }

    public void OnExitOnlineClicked()
    {
        if (gameManager == null)
            return;

        var mode = gameManager.gameModeManager != null
            ? gameManager.gameModeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;

        if (mode != GameModeManager.GameModes.Online)
            return;

        // ถ้าต้องการแจ้ง resign/exit ให้เซิร์ฟเวอร์ก่อน
        var onlineSession = FindFirstObjectByType<OnlineSessionManager>();
        if (onlineSession != null)
            onlineSession.ExitOnline();

        UnityEngine.SceneManagement.SceneManager.LoadScene("OnlineLobby");
    }


    // Force leave (after sync done)
    public void DoResetAndLeave()
    {
        if (PauseManager.isPaused)
            PauseManager.Resume();

        if (gameManager != null)
            gameManager.HandleExitGame();

        SceneManager.LoadScene("MainMenu");
    }

    private void HandleGameOver(Team winningTeam, string endReason)
    {
        ShowGameOver(winningTeam, endReason);
    }

    private void HandlePlayerNamesChanged(string whiteName, string blackName)
    {
        SetPlayerNames();
    }

}
