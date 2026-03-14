using System;
using UnityEngine;
using UnityEngine.UI;


public class InvitePopupUi : MonoBehaviour
{

    [Header("UI Elements")]
    public GameObject panelInvite;
    public Button btnAccept;
    public Button btnDecline;
    public Text txtStatus;

    [Header("Auto Hide")]
    public bool autoHide = true;
    public float autoHideSeconds = 30f;

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

    private Action onAcceptCallback;
    private Action onDeclineCallback;
    private Action onTimeoutCallback;
    private Coroutine autoHideCoroutine;

    public void Setup(Action onAccept, Action onDecline, Action onTimeout = null)
    {
        this.onAcceptCallback = onAccept;
        this.onDeclineCallback = onDecline;
        this.onTimeoutCallback = onTimeout;
    }

    public void OnAccept()
    {
        Debug.Log("Accept Invite");
        StopAutoHide();
        onAcceptCallback?.Invoke();
    }

    public void OnDecline()
    {
        Debug.Log("Decline Invite");
        StopAutoHide();
        onDeclineCallback?.Invoke();
    }

    public void ShowInvitePopup()
    {
        panelInvite.SetActive(true);
        StartAutoHide();
    }

    public void HideInvitePopup()
    {
        panelInvite.SetActive(false);
        StopAutoHide();
    }

    public void SetStatusText(string message)
    {
        if (txtStatus != null) txtStatus.text = message;
    }

    private void StartAutoHide()
    {
        if (!autoHide || autoHideSeconds <= 0f) return;
        StopAutoHide();
        autoHideCoroutine = StartCoroutine(AutoHideRoutine());
    }

    private void StopAutoHide()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    private System.Collections.IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(autoHideSeconds);
        if (panelInvite != null) panelInvite.SetActive(false);
        autoHideCoroutine = null;
        onTimeoutCallback?.Invoke();
    }
}
