using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;


public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenu;
    public GameObject aiMenu;
    public GameObject aivsaiMenu;

    [Header("Single Player Dropdowns")]
    public TMP_Dropdown dpdSinglePlayerDifficulty;
    public TMP_Dropdown dpdSinglePlayerColor;
    public TMP_Dropdown dpdSinglePlayerTime;

    [Header("Single Player buttons")]
    public Button btnStartSinglePlayer;

    [Header("AI vs AI Dropdowns")]
    public TMP_Dropdown dpdWhite;
    public TMP_Dropdown dpdBlack;

    private string selectedSinglePlayerDifficulty = "Easy";
    private string selectedSinglePlayerColor = PlayersUtils.White;
    private string selectedSinglePlayerTime = "No Timer";

    private void Start()
    {
        InitializeSinglePlayerDropdowns();
        UpdateSinglePlayerStartButton();
    }

    private string GetDifficultyFromDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
            return "Easy";

        switch (dropdown.value)
        {
            case 0: return "Easy";
            case 1: return "Normal";
            case 2: return "Hard";
            default: return "Easy";
        }
    }

    private void InitializeSinglePlayerDropdowns()
    {
        if (dpdSinglePlayerDifficulty != null && dpdSinglePlayerDifficulty.options.Count > 0)
            selectedSinglePlayerDifficulty = dpdSinglePlayerDifficulty.options[dpdSinglePlayerDifficulty.value].text;

        if (dpdSinglePlayerColor != null && dpdSinglePlayerColor.options.Count > 0)
            selectedSinglePlayerColor = dpdSinglePlayerColor.options[dpdSinglePlayerColor.value].text;

        if (dpdSinglePlayerTime != null && dpdSinglePlayerTime.options.Count > 0)
            selectedSinglePlayerTime = dpdSinglePlayerTime.options[dpdSinglePlayerTime.value].text;
    }

    private void UpdateSinglePlayerStartButton()
    {
        if (btnStartSinglePlayer == null)
            return;

        bool hasDifficulty = !string.IsNullOrEmpty(selectedSinglePlayerDifficulty);
        bool hasColor = !string.IsNullOrEmpty(selectedSinglePlayerColor);
        bool hasTime = !string.IsNullOrEmpty(selectedSinglePlayerTime);

        btnStartSinglePlayer.interactable = hasDifficulty && hasColor && hasTime;
    }

    public void OnSinglePlayerColorChanged(int index)
    {
        if (dpdSinglePlayerColor == null || index < 0 || index >= dpdSinglePlayerColor.options.Count)
            return;

        selectedSinglePlayerColor = dpdSinglePlayerColor.options[index].text;
        UpdateSinglePlayerStartButton();
    }

    public void OnSinglePlayerDifficultyChanged(int index)
    {
        if (dpdSinglePlayerDifficulty == null || index < 0 || index >= dpdSinglePlayerDifficulty.options.Count)
            return;

        selectedSinglePlayerDifficulty = dpdSinglePlayerDifficulty.options[index].text;
        UpdateSinglePlayerStartButton();
    }

    public void OnSinglePlayerTimeChanged(int index)
    {
        if (dpdSinglePlayerTime == null || index < 0 || index >= dpdSinglePlayerTime.options.Count)
            return;

        selectedSinglePlayerTime = dpdSinglePlayerTime.options[index].text;
        UpdateSinglePlayerStartButton();
    }

    public void ShowAIMenu()
    {
        mainMenu.SetActive(false);
        aiMenu.SetActive(true);
    }

    public void ShowAIVsAIMenu()
    {
        mainMenu.SetActive(false);
        aivsaiMenu.SetActive(true);
    }

    public void OnBackFromAIMenu()
    {
        aiMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void OnBackFromAIVSAIMenu()
    {
        aivsaiMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void OnSelectSingleAIDifficulty(string difficulty)
    {
        OnSelectSingleAIGame(difficulty, PlayersUtils.White);
    }

    public void OnSelectSingleAIGame(string difficulty, string playerColorChoice)
    {
        PlayerColorSetup colorSetup = PlayersUtils.ResolveSinglePlayerColors(playerColorChoice);

        PlayerPrefs.SetString("Mode", "SinglePlayer");
        PlayerPrefs.SetString("AI_Difficulty", difficulty);
        PlayerPrefs.SetString("Player_Color", colorSetup.playerColor);
        PlayerPrefs.SetString("Game_Time", selectedSinglePlayerTime);
        PlayerPrefs.SetString("AI_Color", colorSetup.aiColor);

        Debug.Log(
            $"Selected AI Difficulty: {difficulty} | Player: {colorSetup.playerColor} | AI: {colorSetup.aiColor}  | Time: {selectedSinglePlayerTime}");

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.LoadFromPlayerPrefs();

        if (Application.isPlaying)
        {
            SceneManager.LoadScene("GameCore");
        }
        else
        {
            Debug.LogWarning("❗ LoadScene ได้เฉพาะขณะอยู่ใน Play Mode เท่านั้น");
        }
    }

    public void StartSinglePlayerGameFromDropdown()
    {
        PlayerColorSetup colorSetup = PlayersUtils.ResolveSinglePlayerColors(selectedSinglePlayerColor);

        PlayerPrefs.SetString("Mode", "SinglePlayer");
        PlayerPrefs.SetString("AI_Difficulty", selectedSinglePlayerDifficulty);
        PlayerPrefs.SetString("Player_Color", colorSetup.playerColor);
        PlayerPrefs.SetString("AI_Color", colorSetup.aiColor);
        PlayerPrefs.SetString("Game_Time", selectedSinglePlayerTime);

        Debug.Log(
            $"Single Player Start | Difficulty: {selectedSinglePlayerDifficulty} | Player: {colorSetup.playerColor} | AI: {colorSetup.aiColor} | Time: {selectedSinglePlayerTime}");

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.LoadFromPlayerPrefs();

        if (Application.isPlaying)
        {
            SceneManager.LoadScene("GameCore");
        }
        else
        {
            Debug.LogWarning("❗ LoadScene ได้เฉพาะขณะอยู่ใน Play Mode เท่านั้น");
        }
    }

    public void OnStartAIVsAIMatch()
    {
        string whiteDiff = GetDifficultyFromDropdown(dpdWhite);
        string blackDiff = GetDifficultyFromDropdown(dpdBlack);

        PlayerPrefs.SetString("Mode", "AIVsAI");
        PlayerPrefs.SetString("AI_White_Difficulty", whiteDiff);
        PlayerPrefs.SetString("AI_Black_Difficulty", blackDiff);
        PlayerPrefs.SetString("Game_Time", "No Timer");


        Debug.Log($"Starting AI vs AI Match: White - {whiteDiff}, Black - {blackDiff}");

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.LoadFromPlayerPrefs();
        if (Application.isPlaying)
        {
            SceneManager.LoadScene("GameCore");
        }
        else
        {
            Debug.LogWarning("❗ LoadScene ได้เฉพาะขณะอยู่ใน Play Mode เท่านั้น");
        }
    }
    public void onLocalMultiplayer()
    {
        PlayerPrefs.SetString("Mode", "LocalMultiplayer");
        PlayerPrefs.SetString("Game_Time", "No Timer");


        if (GameModeManager.Instance != null)
            GameModeManager.Instance.LoadFromPlayerPrefs();

        if (Application.isPlaying)
        {
            SceneManager.LoadScene("GameCore");
        }
        else
        {
            Debug.LogWarning("❗ LoadScene ได้เฉพาะขณะอยู่ใน Play Mode เท่านั้น");
        }
    }
    public void onlineMultiplayer()
    {
        PlayerPrefs.SetString("Mode", "OnlineMultiplayer");
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.LoadFromPlayerPrefs();

        if (Application.isPlaying)
        {
            SceneManager.LoadScene("Onlinelogin");
        }
        else
        {
            Debug.LogWarning("❗ LoadScene ได้เฉพาะขณะอยู่ใน Play Mode เท่านั้น");
        }
    }
}
