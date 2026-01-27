using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class profileApi : MonoBehaviour
{
    public static profileApi Instance;
    public GameObject UersName;
    public GameObject Raitng;
    public GameObject Win;
    public GameObject Lose;
    public GameObject Drawn;


    private string apiUrl = "http://localhost:8080/api/User";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        StartCoroutine(GetProfileRequest());
    }

    IEnumerator GetProfileRequest()
    {
        string token = PlayerPrefs.GetString("auth_token", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No authentication token found.");
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl + "/profile"))
        {
            www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("auth_token"));
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                ProfileResponse profile = JsonUtility.FromJson<ProfileResponse>(responseText);
                if (profile != null)
                {
                    SetProfile(profile.username, profile.rating, profile.win, profile.lose, profile.drawn);
                }

                else
                {
                    Debug.LogError("Failed to parse user profile.");
                }
            }
            else
            {
                Debug.LogError($"Failed to load profile: {www.error}");
            }
        }
    }

    public void SetProfile(string username, int rating, int win, int lose, int drawn)
    {
        if (UersName == null || Raitng == null || Win == null || Lose == null || Drawn == null)
        {
            Debug.LogError("Profile UI elements are not assigned.");
            return;
        }
        UersName.GetComponent<TMPro.TextMeshProUGUI>().text = username;
        Raitng.GetComponent<TMPro.TextMeshProUGUI>().text = rating.ToString();
        Win.GetComponent<TMPro.TextMeshProUGUI>().text = win.ToString();
        Lose.GetComponent<TMPro.TextMeshProUGUI>().text = lose.ToString();
        Drawn.GetComponent<TMPro.TextMeshProUGUI>().text = drawn.ToString();
    }
}
