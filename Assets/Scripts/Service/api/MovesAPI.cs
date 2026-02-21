using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MovesAPI : MonoBehaviour
{
    // ✅ ชี้ไปที่ endpoint batch
    [SerializeField] private string baseUrl = "http://localhost:8080/api/Move/batch";
    public virtual IEnumerator SendMovesBatch(List<MoveCreateDto> movesList, Action<bool> onComplete)
    {
        MoveBatchRequest dto = new MoveBatchRequest();
        dto.moves = movesList;
        string json = JsonUtility.ToJson(dto);

        Debug.Log($"📦 [MovesAPI] JSON Payload: {json}");
        var request = new UnityWebRequest(baseUrl, "POST"); // อย่าลืมเช็ค URL ว่าลงท้าย /batch
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        string token = SessionManager.Instance != null ? SessionManager.Instance.Token : string.Empty;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ [MovesAPI] Batch Success: {request.downloadHandler.text}");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError($"❌ [MovesAPI] Batch Failed: {request.error} | {request.downloadHandler.text}");
            onComplete?.Invoke(false);
        }
    }

    public static class JsonHelper
    {
        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            string json = JsonUtility.ToJson(wrapper);

            // Hack: ตัดคำว่า {"Items": และ } ตัวสุดท้ายออก เพื่อให้ได้ Array เพียวๆ
            int startIndex = "{\"Items\":".Length;
            int endIndex = json.Length - 1;
            return json.Substring(startIndex, endIndex - startIndex);
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }

    [Serializable]
    private class MoveDtoPascal
    {
        public int MoveNumber;
        public int StartX;
        public int StartY;
        public int EndX;
        public int EndY;
        public string FromPosition;
        public string ToPosition;
        public string PlayerTurn;
        public string Status;
        public string Winner;
        public int PromotedTo;
        public bool IsCastling;
        public bool IsEnPassant;
    }

    // ✅ ส่งตาเดินทีละตา (Real-time)
    public virtual IEnumerator SendMove(MoveCreateDto moveData, Action<bool> onComplete)
    {
        // URL ต้องตรงกับ MoveController [HttpPost] ของคุณ
        string url = "http://localhost:8080/api/Move";

        // แปลงข้อมูลเป็น JSON
        string json = JsonUtility.ToJson(moveData);
        Debug.Log($"📡 [API] Sending Move: {json}");

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        string token = SessionManager.Instance != null ? SessionManager.Instance.Token : string.Empty;
        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", "Bearer " + token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ [API] Move Sent Successfully!");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError($"❌ [API] Send Move Failed: {request.error} | Response: {request.downloadHandler.text}");
            onComplete?.Invoke(false);
        }
    }

    // ✅ ดึงตาเดินล่าสุด (Polling)
    public virtual IEnumerator GetLatestMove(int gameId, Action<MoveDto> onMoveReceived)
    {
        string url = $"http://localhost:8080/api/Move/latest/{gameId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            string token = SessionManager.Instance != null ? SessionManager.Instance.Token : string.Empty;
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", "Bearer " + token);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MoveDto moveData = null;
                if (string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    onMoveReceived?.Invoke(null);
                }
                else
                {
                    Debug.Log($"[MovesAPI] LatestMove raw: {request.downloadHandler.text}");
                    try
                    {
                        moveData = JsonUtility.FromJson<MoveDto>(request.downloadHandler.text);
                        if (moveData == null || moveData.moveNumber == 0)
                        {
                            if (request.downloadHandler.text.Contains("\"MoveNumber\""))
                            {
                                var pascal = JsonUtility.FromJson<MoveDtoPascal>(request.downloadHandler.text);
                                if (pascal != null && pascal.MoveNumber > 0)
                                {
                                    moveData = new MoveDto
                                    {
                                        moveNumber = pascal.MoveNumber,
                                        startX = pascal.StartX,
                                        startY = pascal.StartY,
                                        endX = pascal.EndX,
                                        endY = pascal.EndY,
                                        fromPosition = pascal.FromPosition,
                                        toPosition = pascal.ToPosition,
                                        playerTurn = pascal.PlayerTurn,
                                        status = pascal.Status,
                                        winner = pascal.Winner,
                                        promotedTo = pascal.PromotedTo,
                                        isCastling = pascal.IsCastling,
                                        isEnPassant = pascal.IsEnPassant
                                    };
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"JSON Parse Error: {e.Message}");
                        onMoveReceived?.Invoke(null);
                        yield break; // ออกจากฟังก์ชันทันที
                    }
                }
                Debug.Log($"[MovesAPI] LatestMove parsed moveNumber={(moveData != null ? moveData.moveNumber : 0)}");
                if (moveData != null)
                {
                    onMoveReceived?.Invoke(moveData);
                }
                else
                {
                    onMoveReceived?.Invoke(null);
                }
                
            }
        }
    }
}

