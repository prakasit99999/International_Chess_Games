using UnityEngine;
using Utils;
using static ChessPiece;

public class GameTimerManager : MonoBehaviour
{
    public float whiteTime;
    public float blackTime;
    public bool useTimer;
    public bool useLocalCountdown;

    private bool isRunning;
    private GameManager gameManager;
    private Team activeTimerTeam = Team.White;

    private void Start()
    {
        gameManager = GameManager.Instance;
        if (gameManager != null)
            activeTimerTeam = gameManager.CurrentTurn;

        InitializeTimer();
    }

    private void OnEnable()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager != null)
            gameManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void Update()
    {
        if (!useTimer || !useLocalCountdown || !isRunning || gameManager == null || gameManager.IsGameOver())
            return;

        if (activeTimerTeam == Team.White)
            whiteTime -= Time.deltaTime;
        else
            blackTime -= Time.deltaTime;

        CheckTimeout();
    }

    public void InitializeTimer()
    {
        var modeManager = GameModeManager.Instance;
        if (modeManager != null)
            modeManager.LoadFromPlayerPrefs();

        var mode = modeManager != null
            ? modeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;

        bool allowTimerDisplay =
            mode == GameModeManager.GameModes.SinglePlayer ||
            mode == GameModeManager.GameModes.LocalMultiplayer ||
            mode == GameModeManager.GameModes.Online;

        useLocalCountdown =
            mode == GameModeManager.GameModes.SinglePlayer ||
            mode == GameModeManager.GameModes.LocalMultiplayer;

        if (!allowTimerDisplay)
        {
            useTimer = false;
            useLocalCountdown = false;
            whiteTime = 0f;
            blackTime = 0f;
            StopTimer();
            return;
        }

        string selectedTime = PlayerPrefs.GetString("Game_Time", "No Timer");
        float startSeconds = ParseTimeToSeconds(selectedTime);

        whiteTime = startSeconds;
        blackTime = startSeconds;
        useTimer = startSeconds > 0f;
        activeTimerTeam = gameManager != null ? gameManager.CurrentTurn : Team.White;

        if (useTimer && useLocalCountdown)
            StartTimer();
        else
            StopTimer();
    }


    private float ParseTimeToSeconds(string timeText)
    {
        switch (timeText)
        {
            case "3 Min":
                return 180f;
            case "5 Min":
                return 300f;
            case "10 Min":
                return 600f;
            default:
                return 0f;  
        }
    }

    private void CheckTimeout()
    {
        if (whiteTime <= 0f)
        {
            whiteTime = 0f;
            StopTimer();
            gameManager.EndGameByTimeout(Team.White);
            return;
        }

        if (blackTime <= 0f)
        {
            blackTime = 0f;
            StopTimer();
            gameManager.EndGameByTimeout(Team.Black);
        }
    }



    public void StartTimer() => isRunning = true;
    public void StopTimer() => isRunning = false;

    public float GetWhiteTime() => whiteTime;
    public float GetBlackTime() => blackTime;

    private void HandleTurnChanged(Team currentTurn)
    {
        activeTimerTeam = currentTurn;
    }
}
