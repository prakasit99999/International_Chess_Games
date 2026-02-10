using UnityEngine;
using UnityEngine.UI;

public class InvitePopupUi : MonoBehaviour
{

    [Header("UI Elements")]
    public GameObject panelInvite;
    public Button btnAccept;
    public Button btnDecline;
    public Text txtStatus;

    private void Start()
    {
        if (btnAccept != null)
        {
            btnAccept.onClick.RemoveAllListeners();
            btnAccept.onClick.AddListener(OnAccept);
        }
        if (btnDecline != null)
        {
            btnDecline.onClick.RemoveAllListeners();
            btnDecline.onClick.AddListener(OnDecline);
        }
    }

    private System.Action onAcceptCallback;
    private System.Action onDeclineCallback;

    public void Setup(System.Action onAccept, System.Action onDecline)
    {
        this.onAcceptCallback = onAccept;
        this.onDeclineCallback = onDecline;
    }

    public void OnAccept()
    {
        Debug.Log("Accept Invite");
        onAcceptCallback?.Invoke();
    }

    public void OnDecline()
    {
        Debug.Log("Decline Invite");
        onDeclineCallback?.Invoke();
    }

    public void ShowInvitePopup()
    {
        panelInvite.SetActive(true);
    }

    public void HideInvitePopup()
    {
        panelInvite.SetActive(false);
    }

    public void SetStatusText(string message)
    {
        if (txtStatus != null) txtStatus.text = message;
    }
}
