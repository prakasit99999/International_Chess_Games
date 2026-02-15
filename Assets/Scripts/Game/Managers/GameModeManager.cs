using System.Collections;
using System.Collections.Generic;
using AIEngine.Adapters;
using AIEngine.Utilities;
using Game.Interfaces;
using TMPro;
using UnityEngine;

public class GameModeManager : MonoBehaviour
{

    public enum GameModes
    {
        LocalMultiplayer,
        SinglePlayer,
        AIVsAI,
        Online
    }

    public static GameModeManager Instance { get; private set; }
    public GameModes CurrentMode { get; private set; } = GameModes.SinglePlayer;
    public string SelectedDifficulty { get; private set; } = "medium";
    public string SelectedWhiteDifficulty { get; private set; } = "medium";
    public string SelectedBlackDifficulty { get; private set; } = "medium";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadFromPlayerPrefs();
    }

    public void LoadFromPlayerPrefs()
    {
        SetModeFromPlayerPrefs();
        LoadDifficultyFromPlayerPrefs();
    }

    public void SetMode(GameModes mode)
    {
        CurrentMode = mode;
        Debug.Log($"🎮 Mode set to: {mode}");
    }

    public void SetModeFromPlayerPrefs()
    {
        string modeStr = PlayerPrefs.GetString("Mode", "SinglePlayer");

        CurrentMode = modeStr switch
        {
            "AIVsAI" => GameModes.AIVsAI,
            "LocalMultiplayer" => GameModes.LocalMultiplayer,
            "OnlineMultiplayer" => GameModes.Online,
            _ => GameModes.SinglePlayer
        };

        Debug.Log($"🎮 Mode loaded from PlayerPrefs: {CurrentMode}");
    }

    public void SetDifficulty(string difficulty)
    {
        SelectedDifficulty = NormalizeDifficulty(difficulty);
    }

    private void LoadDifficultyFromPlayerPrefs()
    {
        string single = PlayerPrefs.GetString("AI_Difficulty", "Medium");
        SelectedDifficulty = NormalizeDifficulty(single);

        string white = PlayerPrefs.GetString("AI_White_Difficulty", single);
        string black = PlayerPrefs.GetString("AI_Black_Difficulty", single);

        SelectedWhiteDifficulty = NormalizeDifficulty(white);
        SelectedBlackDifficulty = NormalizeDifficulty(black);
    }

    private string NormalizeDifficulty(string difficulty)
    {
        if (string.IsNullOrEmpty(difficulty))
            return "medium";

        string value = difficulty.Trim().ToLowerInvariant();
        if (value == "normal")
            value = "medium";

        return value;
    }
}
