using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image;

public class LoginUi : MonoBehaviour
{

    [Header("Login UI")]
    public GameObject emailLogin;
    public GameObject passwordLogin;
    public GameObject ShowLoginVaildatons;
    public GameObject txtLoginVaildaton;

    [Header("Password Toggle")]
    public Image passwordToggleImage; // ภาพของปุ่ม Toggle (Button Image)
    public Sprite passwordUnmaskIcon; // Free Flat Toggle Right Icon (Show)
    public Sprite passwordMaskIcon;   // Free Flat Toggle Left Icon (Hide)

    [Header("Register UI")]
    public GameObject usernameSigUp;
    public GameObject passwordSigUp;
    public GameObject emailSigUp;
    public GameObject LoginFromPanel;
    public GameObject SignUpFromPanel;
    public GameObject ShowSigUpVaildatons;
    public GameObject txtSigUpVaildaton;
    public GameObject switchBtnLogin;
    public GameObject switchBtnSignUp;

    public Color cickColor = Color.green;
    public Color nomalColor = Color.white;


    private void Start()
    {
        // 🔹 ผูก event
        authApi.Instance.OnLoginSuccess = HandleLoginSuccess;
        authApi.Instance.OnLoginFailed = HandleLoginFailed;
        authApi.Instance.OnRegisterSuccess = HandleRegisterSuccess;
        authApi.Instance.OnRegisterFailed = HandleRegisterFailed;
        authApi.Instance.OnLogoutSuccess = HandleLogoutSuccess;
        authApi.Instance.OnLogoutFailed = HandleLogoutFailed;

        // 🔹 ค่าเริ่มต้น
        LoginFromPanel.SetActive(true);
        SignUpFromPanel.SetActive(false);
        switchBtnLogin.GetComponent<Image>().color = cickColor;
        switchBtnSignUp.GetComponent<Image>().color = nomalColor;

        // 🔹 ตั้งค่าเริ่มต้นให้รหัสผ่านซ่อนอยู่
        if (passwordLogin != null)
        {
            TMP_InputField passInput = passwordLogin.GetComponent<TMP_InputField>();
            passInput.contentType = TMP_InputField.ContentType.Password;
            passInput.ForceLabelUpdate();
        }

        if (passwordToggleImage != null && passwordMaskIcon != null)
        {
            passwordToggleImage.sprite = passwordMaskIcon;
        }
    }


    private void HandleLoginSuccess(AuthSuccessResponse resp)
    {
        Debug.Log("Login success UI side!");
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.SetLoginData(
                resp.token,
                resp.userId,
                resp.username,
                resp.email,
                resp.status
            );
        }

        // Set PerformanceTracker UserId for invite system and other features
        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.UserId = resp.userId;
            Debug.Log($"✅ PerformanceTracker.UserId set to: {resp.userId}");
        }

        emailLogin.GetComponent<TMP_InputField>().text = "";
        passwordLogin.GetComponent<TMP_InputField>().text = "";
        ShowLoginVaildatons.SetActive(false);

        SceneManager.LoadScene("OnlineLobby");
    }

    private void HandleLoginFailed(string error)
    {
        ShowLoginVaildatons.SetActive(true);
        txtLoginVaildaton.GetComponent<UnityEngine.UI.Text>().text = error;
    }

    private void HandleLogoutSuccess()
    {
        Debug.Log("Logout success UI side!");


        // หรือไป Scene Login โดยตรง
        SceneManager.LoadScene("LoginScene");
    }

    private void HandleLogoutFailed(string error)
    {
        Debug.LogError("Logout failed: " + error);
        // สามารถโชว์ Popup หรือ Text แจ้งผู้ใช้
    }

    private void HandleRegisterSuccess()
    {
        usernameSigUp.GetComponent<TMP_InputField>().text = "";
        emailSigUp.GetComponent<TMP_InputField>().text = "";
        passwordSigUp.GetComponent<TMP_InputField>().text = "";

        LoginFromPanel.SetActive(true);
        SignUpFromPanel.SetActive(false);
        ShowSigUpVaildatons.SetActive(false);

        switchBtnLogin.GetComponent<Image>().color = cickColor;
        switchBtnSignUp.GetComponent<Image>().color = nomalColor;
    }

    private void HandleRegisterFailed(string error)
    {
        ShowSigUpVaildatons.SetActive(true);
        txtSigUpVaildaton.GetComponent<UnityEngine.UI.Text>().text = error;
    }

    public void swichBtn()
    {
        if (LoginFromPanel.activeSelf)
        {
            LoginFromPanel.SetActive(false);
            SignUpFromPanel.SetActive(true);
            // ???????????????????????????? Sign 
            switchBtnLogin.GetComponent<Image>().color = nomalColor;
            switchBtnSignUp.GetComponent<Image>().color = cickColor;
        }
        else
        {
            LoginFromPanel.SetActive(true);
            SignUpFromPanel.SetActive(false);
            // ???????????????????????????? Login
            switchBtnLogin.GetComponent<Image>().color = cickColor;
            switchBtnSignUp.GetComponent<Image>().color = nomalColor;
        }
    }

    public void LoginUser()
    {
        string email = emailLogin.GetComponent<TMP_InputField>().text;
        string pass = passwordLogin.GetComponent<TMP_InputField>().text;

        StartCoroutine(authApi.Instance.LoginRequest(email, pass));

    }

    public void RegisterUser()
    {
        string username = usernameSigUp.GetComponent<TMP_InputField>().text;
        string email = emailSigUp.GetComponent<TMP_InputField>().text;
        string pass = passwordSigUp.GetComponent<TMP_InputField>().text;

        StartCoroutine(authApi.Instance.RegisterRequest(username, email, pass));
    }


    public void LogoutUser()
    {
        int userId = PlayerPrefs.GetInt("user_id", -1);
        if (userId != -1)
        {
            StartCoroutine(authApi.Instance.LogoutRequest(userId));
        }
        else
        {
            Debug.LogWarning("No user logged in.");
        }
    }

    public void Home()
    {
        //ออกจาก login ไผ 
        SceneManager.LoadScene("MainMenu");
    }

    /// สลับการแสดง/ซ่อนรหัสผ่าน และเปลี่ยนไอคอน
    public void TogglePasswordVisibility()
    {
        TMP_InputField input = passwordLogin.GetComponent<TMP_InputField>();

        if (input.contentType == TMP_InputField.ContentType.Password)
        {
            // แสดงรหัสผ่าน
            input.contentType = TMP_InputField.ContentType.Standard;
            if (passwordToggleImage != null && passwordUnmaskIcon != null)
            {
                passwordToggleImage.sprite = passwordUnmaskIcon;
            }
        }
        else
        {
            // ซ่อนรหัสผ่าน
            input.contentType = TMP_InputField.ContentType.Password;
            if (passwordToggleImage != null && passwordMaskIcon != null)
            {
                passwordToggleImage.sprite = passwordMaskIcon;
            }
        }

        // บังคับ update text ที่แสดงอยู่
        input.ForceLabelUpdate();
    }






}
