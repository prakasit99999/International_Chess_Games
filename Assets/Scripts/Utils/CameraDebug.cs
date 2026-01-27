using UnityEngine;

public class CameraDebug : MonoBehaviour
{
    private float lastZ;

    void Start()
    {
        lastZ = transform.rotation.eulerAngles.z;
        Debug.Log($"📷 เริ่มเกม: Camera Rotation Z = {lastZ}");
    }

    void Update()
    {
        // ตรวจสอบค่า Z ปัจจุบัน
        float currentZ = transform.rotation.eulerAngles.z;

        // ถ้าค่า Z เปลี่ยนไปจากเฟรมที่แล้ว ให้แจ้งเตือน!
        if (Mathf.Abs(currentZ - lastZ) > 0.1f) // ใช้ 0.1 เผื่อค่าคลาดเคลื่อนเล็กน้อย
        {
            Debug.LogError($"🚨 จับได้แล้ว! มีคนหมุนกล้อง! จาก {lastZ} เป็น {currentZ}");
            Debug.LogError($"StackTrace (ใครเรียก?): {System.Environment.StackTrace}");

            lastZ = currentZ; // อัปเดตค่าล่าสุด
        }
    }
}