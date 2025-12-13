using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json; // ต้องมี Library Newtonsoft.Json หรือใช้ JsonUtility ของ Unity ก็ได้

public class MatchmakingApi : MonoBehaviour
{
    // เปลี่ยนเป็น IP ของเครื่อง Server หรือ localhost
    private string baseUrl = "http://localhost:5000/api/Matchmaking";
    // struct สำหรับรับผลลัพธ์ JSON จาก Server
    [Serializable]
    public class MatchResponse
    {
        public string message;
        public MatchDetails matchDetails;
    }

    [Serializable]
    public class MatchDetails
    {
        public int gameId;          // รับค่า gameId (int)
        public string roomCode;     // รับค่า roomCode (string)
        public string opponentUsername;
        public string color;        // "white" หรือ "black"
        public string gameType;     // เพิ่มตัวนี้ให้ตรงกับ Backend
        public int timeControlMinutes;
    }
    // ฟังก์ชัน 1: ขอเข้าคิว (Join Queue)
    public IEnumerator JoinQueue(string username, int timeControl, int minRate, int maxRate, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/join?username={username}&minRating={minRate}&maxRating={maxRate}&preferredTimeControl={timeControl}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(true, request.downloadHandler.text);
            }
            else
            {
                callback(false, request.error);
            }
        }
    }
    // ฟังก์ชัน 2: เช็คว่าเจอคู่หรือยัง (Check Match)
    public IEnumerator CheckForMatch(string username, Action<bool, MatchResponse> callback)
    {
        string url = $"{baseUrl}/check?username={username}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // แปลง JSON ที่ Server ส่งกลับมาเป็น Object
                // ถ้าใช้ Newtonsoft: var response = JsonConvert.DeserializeObject<MatchResponse>(request.downloadHandler.text);
                // ถ้าใช้ Unity JsonUtility:
                var response = JsonUtility.FromJson<MatchResponse>(request.downloadHandler.text);
                callback(true, response);
            }
            else
            {
                // กรณี 404 Not Found (ยังไม่เจอคู่) หรือ Error อื่นๆ
                callback(false, null);
            }
        }
    }

    // ฟังก์ชัน 3: ยกเลิกการหาห้อง (Cancel Queue)
    public IEnumerator CancelQueue(string username, Action<bool, string> callback)
    {
        string url = $"{baseUrl}/cancel?username={username}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(true, "Cancelled");
            }
            else
            {
                callback(false, request.error);
            }
        }
    }

    public IEnumerator CheckQueue(string username, Action<bool, MatchResponse> callback)
    {
        string url = $"{baseUrl}/check?username={username}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // แปลง JSON เป็น Object
                var response = JsonUtility.FromJson<MatchResponse>(request.downloadHandler.text);
                
                // ตรวจสอบว่าได้ข้อมูลห้องมาจริงไหม
                if (response != null && response.matchDetails != null && response.matchDetails.gameId != 0)
                {
                    callback(true, response);
                }
                else
                {
                    callback(false, null); // ยังไม่เจอ หรือ Server ตอบมาแต่ไม่มี detail
                }
            }
            else
            {
                // ยังไม่เจอคู่ หรือ Error
                callback(false, null);
            }
        }
    }
}