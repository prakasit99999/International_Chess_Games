using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;
    public bool isSoundEnabled = true; // เปิด/ปิดเสียง
    public Toggle toggleLabelShow;
    public GameObject boardLabels; // แสดง Label
    public GameObject settingPanel;
    public Text toggleLabelText;

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
        if (IsOnlineMode())
        {
            return;
        }

        settingPanel.SetActive(false);
        if (PauseManager.isPaused)
            PauseManager.Resume();
    }
    public void ReturnToMainMenu()
    {
        if (IsOnlineMode())
        {
            return;
        }

        Time.timeScale = 1f;
        PauseManager.Resume();

        // ถ้ามี GameSyncService ให้ส่งผ่าน flow ปกติ (finalize + load)
        var sync = FindFirstObjectByType<GameSyncService>();
        if (GameManager.Instance != null && sync != null)
        {
            GameManager.Instance.OnExitGameClicked();
            return;
        }

        // fallback: ไม่มี sync handler ให้กลับเมนูทันที
        SceneManager.LoadScene("MainMenu");
    }

    public void Replay()
    {
        if (IsOnlineMode())
        {
            return;
        }
        PauseManager.Resume();

        var sync = FindFirstObjectByType<GameSyncService>();
        if (GameManager.Instance != null && sync != null)
        {
            GameManager.Instance.OnRestartClick();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
            GameManager.Instance.ApplyOfflineModeSettings();
        }

        settingPanel.SetActive(false);
    }

    public void OnToggleLabelChanged(bool isOn)
    {
        if (boardLabels != null)
        {
            boardLabels.SetActive(isOn);
            toggleLabelText.text = isOn ? "ON" : "OFF";
        }
    }

    private bool IsOnlineMode()
    {
        if (GameModeManager.Instance != null)
            return GameModeManager.Instance.CurrentMode == GameModeManager.GameModes.Online;

        if (GameManager.Instance != null && GameManager.Instance.gameModeManager != null)
            return GameManager.Instance.gameModeManager.CurrentMode == GameModeManager.GameModes.Online;

        return false;
    }

}
