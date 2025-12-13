using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AiPerformanceAPI : MonoBehaviour
{
    // ตั้งค่าเป็น URL ของ ASP.NET ของคุณ
    // ตัวอย่าง: http://localhost:8080/api/AiPerformance
    [SerializeField] private string baseUrl = "http://localhost:8080/api/AiPerformance";

    public IEnumerator SendPerformance(AiPerformanceData dto)
    {
        string json = JsonUtility.ToJson(dto);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl, json))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            bool isError = request.result != UnityWebRequest.Result.Success;
            if (isError)
            {
                Debug.LogError($"AI Performance Upload Failed: {request.error} | Response: {request.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"AI Performance Upload Success: {request.downloadHandler.text}");
            }
        }
    }
}
