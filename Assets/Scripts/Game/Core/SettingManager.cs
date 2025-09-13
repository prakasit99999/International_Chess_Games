using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;
    public bool isSoundEnabled = true; // เปิด/ปิดเสียง
    public Toggle toggleLabelShow;
    public GameObject boardLabels;
    public GameObject settingPanel;

    public Text toggleLabelText;
    public float soundVolume = 1.0f; // ระดับเสียง

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // ถ้ามี Instance อยู่แล้ว ให้ทำลายตัวเอง
        }
    }
    private void Start()
    {
        // ตั้งค่าเริ่มต้นสำหรับ Toggle
        if (toggleLabelShow != null)
        {
            toggleLabelShow.isOn = boardLabels.activeSelf;
            toggleLabelShow.onValueChanged.AddListener(OnToggleLabelChanged);
        }
    }

    public void OpenSettings()
    {
        settingPanel.SetActive(true);
        if (!PauseManager.isPaused)
            PauseManager.Pase();
    }

    public void CloseSettings()
    {
        settingPanel.SetActive(false);
        if (PauseManager.isPaused)  
            PauseManager.Resume();
    }
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PauseManager.Resume();
        SceneManager.LoadScene("MainMenu");
    }
 
    public void SetSoundVolume(float volume)
    {
        soundVolume = volume;
        Debug.Log("ระดับเสียง: " + soundVolume);
    }
    public void OnToggleLabelChanged(bool isOn)
    {
        if (boardLabels != null)
        {
            boardLabels.SetActive(isOn);
            toggleLabelText.text = isOn ? "ON" : "OFF";
        }
    }

}
