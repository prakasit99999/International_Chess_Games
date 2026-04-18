using System.Diagnostics;
using TMPro;
using UnityEngine;
using Utils;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using static ChessPiece;

public class TimerUI : MonoBehaviour
{
    [Header("Timer Text")]
    public TMP_Text txtTimer;
    public TMP_Text txtWhiteTimer;
    public TMP_Text txtBlackTimer;

    [Header("Settings")]
    public GameObject showIndividualTimers;
    public string noTimerValue = "No Timer";

    [Header("References")]
    public GameTimerManager timerManager;
    public GameManager gameManager;

    private void Start()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (timerManager == null)
            timerManager = FindFirstObjectByType<GameTimerManager>();

        SetupTimerVisibility();
    }

    private void Update()
    {
        if (timerManager == null || gameManager == null)
            return;

        UpdateCurrentTurnTimer();
        UpdateIndividualTimers();
    }


    private void UpdateCurrentTurnTimer()
    {
        if (txtTimer == null)
            return;

        float currentTime = gameManager.CurrentTurn == Team.White
            ? timerManager.GetWhiteTime()
            : timerManager.GetBlackTime();

        txtTimer.text = GameUtils.FormatTime(currentTime);
    }

    private void UpdateIndividualTimers()
    {
        if (txtWhiteTimer != null)
            txtWhiteTimer.text = GameUtils.FormatTime(timerManager.GetWhiteTime());

        if (txtBlackTimer != null)
            txtBlackTimer.text = GameUtils.FormatTime(timerManager.GetBlackTime());
    }

    public void SetupTimerVisibility()
    {
        string selectedTime = PlayerPrefs.GetString("Game_Time", noTimerValue);
        Debug.Log($"Selected Time from PlayerPrefs: {selectedTime}");
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

        ShowIndividualTimers(allowTimerDisplay, selectedTime);
    }


    public void ShowIndividualTimers(bool show, string selectedTime)
    {
        bool shouldShow = show && selectedTime != noTimerValue && !string.IsNullOrEmpty(selectedTime);

        if (showIndividualTimers != null)
            showIndividualTimers.SetActive(shouldShow);

        if (txtTimer != null)
            txtTimer.gameObject.SetActive(shouldShow);

        // if (txtWhiteTimer != null)
        //     txtWhiteTimer.gameObject.SetActive(shouldShow);

        // if (txtBlackTimer != null)
        //     txtBlackTimer.gameObject.SetActive(shouldShow);
    }

    public void HideAllTimerUI()
    {
        ShowIndividualTimers(false, noTimerValue);
    }
}
