using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using static System.Net.Mime.MediaTypeNames;
using Image = UnityEngine.UI.Image;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginUi : MonoBehaviour
{
    [Header("Login UI")]
    public GameObject emailLogin;
    public GameObject passwordLogin;
    public GameObject ShowLoginVaildatons;
    public GameObject txtLoginVaildaton;

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
        //authApi.Instance.OnLogoutSuccess = HandleLogoutSuccess;
        //authApi.Instance.OnLogoutFailed = HandleLogoutFailed;

        // 🔹 ค่าเริ่มต้น
        LoginFromPanel.SetActive(true);
        SignUpFromPanel.SetActive(false);
        switchBtnLogin.GetComponent<Image>().color = cickColor;
        switchBtnSignUp.GetComponent<Image>().color = nomalColor;
    }


    private void HandleLoginSuccess(authApi.AuthSuccessResponse resp)
    {
        Debug.Log("Login success UI side!");
        PlayerPrefs.SetString("auth_token", resp.token);
        PlayerPrefs.SetInt("user_id", resp.userId);
        PlayerPrefs.SetString("username", resp.username);
        PlayerPrefs.SetString("email", resp.email);
        PlayerPrefs.SetString("status", resp.status);

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






}
