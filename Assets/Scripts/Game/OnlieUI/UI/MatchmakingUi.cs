using UnityEngine;
using UnityEngine.UI;

public class MatchmakingUi : MonoBehaviour
{
    [Header("Manager Reference")]
    public MatchmakingManager matchmakingManager;

    [Header("Object UI")]
    public GameObject panelMatchmaking;

    [Header("Button UI")]
    public Button btnMatchmakingShow;
    public Button btnMatchmakingStart;
    public Button btnMatchmakingCancel;
    public Button btnMatchmakingClose;

    [Header("Text UI")]
    public Text statusText;

    void Start()
    {
        if (matchmakingManager == null)
            matchmakingManager = FindObjectOfType<MatchmakingManager>();

        if (btnMatchmakingShow != null)
        {
            btnMatchmakingShow.onClick.RemoveAllListeners();
            btnMatchmakingShow.onClick.AddListener(ToggleMatchmakingPanel);
        }
        if (btnMatchmakingClose != null)
        {
            btnMatchmakingClose.onClick.RemoveAllListeners();
            btnMatchmakingClose.onClick.AddListener(HideMatchmaking);
        }

        ResetUI();
        panelMatchmaking.SetActive(false);
    }

    public void ToggleMatchmakingPanel()
    {
        bool isActive = panelMatchmaking.activeSelf;
        Debug.Log("ToggleMatchmakingPanel: " + isActive);
        if (isActive)
        {
            matchmakingManager.CancelMatchmaking();
            panelMatchmaking.SetActive(false);
        }
        else
        {
            ResetUI();
            panelMatchmaking.SetActive(true);
        }
    }

    public void ShowMatchmaking()
    {
        panelMatchmaking.SetActive(true);
        ResetUI();

    }

    public void HideMatchmaking()
    {
        if (matchmakingManager != null)
        {
            matchmakingManager.CancelMatchmaking();
        }
        panelMatchmaking.SetActive(false);
    }

    public void SetSearchingState(bool isSearching)
    {
        // ถ้ากำลังหา (True) -> ซ่อน Start, โชว์ Cancel, ปิด Close(optional)
        // ถ้าว่าง (False)   -> โชว์ Start, ซ่อน Cancel, เปิด Close

        if (btnMatchmakingStart != null) btnMatchmakingStart.gameObject.SetActive(!isSearching);
        if (btnMatchmakingCancel != null) btnMatchmakingCancel.gameObject.SetActive(isSearching);

        // (Optional) ถ้าอยากให้ล็อคห้ามกดปิดตอนหาห้อง ก็ใส่บรรทัดนี้ได้ครับ
        // if (btnMatchmakingClose != null) btnMatchmakingClose.interactable = !isSearching; 
    }

    public void SetStatusText(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    public void ResetUI()
    {
        SetSearchingState(false);
        SetStatusText("Ready to play");
    }
}