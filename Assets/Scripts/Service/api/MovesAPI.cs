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
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    onMoveReceived?.Invoke(null);
                }
                else
                {
                    MoveDto moveData = null;
                    try
                    {
                        moveData = JsonUtility.FromJson<MoveDto>(request.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"JSON Parse Error: {e.Message}");
                        onMoveReceived?.Invoke(null);
                        yield break; // ออกจากฟังก์ชันทันที
                    }
                    if (moveData != null)
                    {
                        onMoveReceived?.Invoke(moveData);
                    }
                }
            }
            else
            {
                onMoveReceived?.Invoke(null);
            }
        }
    }
}