using System.Collections.Generic;
using UnityEngine;

public class TestMoveSender : MonoBehaviour
{
    public MovesAPI movesApi; // ลาก MovesAPI มาใส่ใน Inspector
    public int testGameId = 1; // ใส่ GameID ที่มีอยู่จริงใน DB (เพื่อไม่ให้ติด Error FK)

    [ContextMenu("🚀 Test Send Batch")] // คลิกขวาที่ Component แล้วกดปุ่มนี้ได้เลย
    public void TestSendBatch()
    {
        if (movesApi == null)
        {
            Debug.LogError("❌ ลืมใส่ MovesAPI ใน Inspector ครับ!");
            return;
        }

        Debug.Log("🔄 กำลังสร้างข้อมูลจำลอง...");

        // 1. จำลองข้อมูลการเดิน 2 ตา
        List<MoveCreateDto> fakeMoves = new List<MoveCreateDto>();

        // ตาที่ 1: เบี้ยขาวเดิน
        MoveCreateDto m1 = new MoveCreateDto();
        m1.GameId = testGameId;
        m1.MoveNumber = 1;
        m1.StartX = 0; m1.StartY = 1;
        m1.EndX = 0; m1.EndY = 2;
        m1.PieceType = 0;
        m1.PlayerTurn = 0;
        m1.AlgorithmType = 0; // คนเล่น (None)
        m1.MoveTimeMilliseconds = 1500;
        fakeMoves.Add(m1);

        // ตาที่ 2: ม้าดำเดิน
        MoveCreateDto m2 = new MoveCreateDto();
        m2.GameId = testGameId;
        m2.MoveNumber = 2;
        m2.StartX = 1; m2.StartY = 7;
        m2.EndX = 2; m2.EndY = 5;
        m2.PieceType = 1;
        m2.PlayerTurn = 1;
        m2.AlgorithmType = 1; // AI เล่น (Minimax)
        m2.AiEvaluationScore = (int)10;
        m2.AiDepthSearched = 3;
        m2.MoveTimeMilliseconds = 500;
        fakeMoves.Add(m2);

        // 2. ส่งข้อมูลผ่าน MovesAPI (ฟังก์ชันที่คุณเพิ่งแก้)
        StartCoroutine(movesApi.SendMovesBatch(fakeMoves, (success) =>
        {
            if (success)
            {
                Debug.Log("✅ TEST PASSED: ส่งข้อมูลสำเร็จ! Server ตอบกลับ OK");
            }
            else
            {
                Debug.LogError("❌ TEST FAILED: ส่งข้อมูลไม่ผ่าน (เช็ค Log ด้านบน)");
            }
        }));
    }
}