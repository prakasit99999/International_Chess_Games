using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenu;
    public GameObject aiMenu;
    public GameObject aivsaiMenu;

    [Header("AI vs AI Dropdowns")]
    public TMP_Dropdown dpdWhite;
    public TMP_Dropdown dpdBlack;

    private string GetDifficultyFromDropdown(TMP_Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0: return "Easy";
            case 1: return "Normal";
            case 2: return "Hard";
            default: return "Easy";
        }
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
        PlayerPrefs.SetString("Mode", "SinglePlayer");
        PlayerPrefs.SetString("AI_Difficulty", difficulty);
        PlayerPrefs.SetString("AI_Color", "Black");

        //Debug.Log($"Selected AI Difficulty: {difficulty}");


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

        //Debug.Log($"Starting AI vs AI Match: White - {whiteDiff}, Black - {blackDiff}");

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

        if (Application.isPlaying)
        {
            SceneManager.LoadScene("GameCore");
        }
        else
        {
            Debug.LogWarning("❗ LoadScene ได้เฉพาะขณะอยู่ใน Play Mode เท่านั้น");
        }
    }
}
