using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchmakingManager : MonoBehaviour
{
    public TMP_Text statusText;
    public TMP_Text opponentNameText;

    private bool isSearching = false;
    private string username;
    // Start is called before the first frame update
    void Start()
    {
        username = PlayerPrefs.GetString("username", "Guest");
        statusText.text = "cick Find Match start";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnFindMatch()
    {
        if (isSearching) return;
        isSearching = true;
        statusText.text = "Searching for matches...";

        StartCoroutine(MatchmakingApi.Instance.JoinQueue(username, 800, 2000, 10,
        (resp) =>
        {
            Debug.Log("JoinQueue success: " + resp);
            StartCoroutine(CheckLoop());
        },
        (err) =>
        {
            statusText.text = "JoinQueue failed!";
            isSearching = false;
            Debug.LogError(err);
        }));
    }

    public void OnCancel()
    {
        if (!isSearching) return;
        isSearching = false;
        statusText.text = "Search canceled";

        StartCoroutine(MatchmakingApi.Instance.CancelQueue(username,
        (resp) => { Debug.Log("CancelQueue success: " + resp); },
        (err) => { Debug.LogError(err); }));
    }

    private IEnumerator CheckLoop()
    {
        while (isSearching)
        {
            yield return MatchmakingApi.Instance.CheckForMatch(username,
            (match) =>
            {
                statusText.text = $"Match found! Opponent: {match.OpponentUsername}";
                opponentNameText.text = match.OpponentUsername;

                // Load the actual game scene after 2 seconds
                StartCoroutine(LoadGameAfterDelay(match));
                isSearching = false;
            },
            () =>
            {
                statusText.text = "No match found yet... Please wait a moment";
            },
            (err) =>
            {
                statusText.text = "An error occurred!";
                Debug.LogError(err);
            });

            yield return new WaitForSeconds(3f); // Check every 3 seconds
        }
    }

    private IEnumerator LoadGameAfterDelay(MatchmakingApi.MatchFoundDTOs match)
    {
        yield return new WaitForSeconds(2f);
        PlayerPrefs.SetString("room_code", match.RoomCode);
        PlayerPrefs.SetString("opponent_username", match.OpponentUsername);
        SceneManager.LoadScene("GameScene"); // Go to the actual game room
    }
}
