using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class InviteWaitingUi : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panelWaiting;
    public Button btnCancel;
    public Text txtStatus;

    private System.Action onCancelCallback;

    private void Start()
    {
        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(OnCancelClicked);
        }

        HideWaiting();
    }

    public void Setup(System.Action onCancel)
    {
        onCancelCallback = onCancel;
    }

    public void ShowWaiting(string message)
    {
        if (txtStatus != null) txtStatus.text = message;
        if (panelWaiting != null) panelWaiting.SetActive(true);
    }

    public void HideWaiting()
    {
        if (panelWaiting != null) panelWaiting.SetActive(false);
    }

    private void OnCancelClicked()
    {
        onCancelCallback?.Invoke();
    }
}
