using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AiPerformanceAPI : MonoBehaviour
{
    // ตั้งค่าเป็น URL ของ ASP.NET ของคุณ
    // ตัวอย่าง: http://127.0.0.1:8080/api/AiPerformance
    [SerializeField] private string baseUrl = "http://127.0.0.1:8080/api/AiPerformance";

    public IEnumerator SendPerformance(AiPerformanceData dto)
    {
        string json = JsonUtility.ToJson(dto);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        Debug.Log($"[AI Performance API] AI Performance Upload Started\n" +
                  $"URL: {baseUrl}\n" +
                  $"JSON: {json}");

        using (UnityWebRequest request =
               new UnityWebRequest(baseUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            // request.timeout = 10; // Timeout 10 วินาที
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"AI Performance Upload Failed\n" +
                    $"Error: {request.error}\n" +
                    $"Response: {request.downloadHandler.text}"
                );
            }
            else
            {
                Debug.Log(
                    $"AI Performance Upload Success\n" +
                    $"Response: {request.downloadHandler.text}"
                );
            }
        }
    }

    public void SetBaseUrl(string url)
    {
        this.baseUrl = url;
    }
}
