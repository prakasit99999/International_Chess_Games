// using System.Collections;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;
// using UnityEngine.UI;

// public class MatchmakingSystemTest
// {
//     // ======================================================================
//     // 1. Mock API Class: ตัวจำลอง Server
//     // ======================================================================
//     public class MockMatchmakingApi : MatchmakingApi
//     {
//         // ตัวแปรเก็บค่าที่ส่งเข้ามา เพื่อเอาไว้ตรวจสอบ (Spy)
//         public int lastReceivedTimeControl = -1;

//         // Param 'preferredTimeControl' ถูกเพิ่มให้ตรง Signature (virtual)
//         public override IEnumerator JoinQueue(int userId, int min, int max, System.Action<bool, MatchResponse> callback)
//         {
//             // lastReceivedTimeControl = preferredTimeControl; // Removed
//             yield return null; // จำลอง Delay นิดหน่อย

//             // --- POINT 2: จำลองว่า Server เจอคู่ทันที (Player 2) ---
//             var fakeResponse = new MatchResponse();
//             fakeResponse.message = "Match Found!";

//             fakeResponse.matchDetails = new MatchDetails
//             {
//                 gameId = 101,
//                 opponentUsername = "Player 2",  // <--- คู่แข่งคือคนนี้
//                 roomCode = 101,
//                 color = "white"
//             };

//             // ส่ง callback กลับไปบอก Manager ว่าสำเร็จ
//             callback(true, fakeResponse);
//         }

//         // Mock ฟังก์ชัน Cancel (เผื่อ Manager เรียกใช้ตอนปิด)
//         public override IEnumerator CancelQueue(int userId, System.Action<bool, string> callback)
//         {
//             yield return null;
//             callback(true, "Cancelled");
//         }
//     }

//     // ======================================================================
//     // 2. Setup Variables
//     // ======================================================================
//     private GameObject testObject;
//     private MatchmakingManager manager;
//     private MatchmakingUi ui;
//     private MockMatchmakingApi mockApi;

//     [SetUp]
//     public void Setup()
//     {
//         // สร้าง GameObject เปล่าขึ้นมาเพื่อทดสอบ
//         testObject = new GameObject("Test_MatchmakingSystem");

//         // ใส่ Mock API
//         mockApi = testObject.AddComponent<MockMatchmakingApi>();

//         // ใส่ Manager
//         manager = testObject.AddComponent<MatchmakingManager>();

//         // ใส่ UI และสร้าง Element จำลอง (เพื่อไม่ให้ Error NullReference)
//         ui = testObject.AddComponent<MatchmakingUi>();
//         ui.panelMatchmaking = new GameObject("Panel");
//         ui.statusText = new GameObject("StatusText").AddComponent<Text>();
//         ui.btnMatchmakingStart = new GameObject("BtnStart").AddComponent<Button>();
//         ui.btnMatchmakingCancel = new GameObject("BtnCancel").AddComponent<Button>();

//         // เชื่อมโยง Reference
//         manager.matchmakingUi = ui;
//         ui.matchmakingManager = manager;

//         // --- POINT 3: กำหนดให้เราเป็น Player 1 ---
//         PlayerPrefs.SetString("username", "Player 1");
//     }

//     [TearDown]
//     public void Teardown()
//     {
//         Object.Destroy(testObject);
//     }

//     // ======================================================================
//     // 3. The Test Case: หัวใจสำคัญที่คุณต้องการ
//     // ======================================================================
//     [UnityTest]
//     public IEnumerator Test_Player1_Finds_Player2_With_Zero_TimeControl()
//     {
//         // Act: Player 1 กดปุ่มค้นหาห้อง
//         manager.StartMatchmaking();

//         // Wait: รอ 1 เฟรมให้ Mock ทำงานและส่ง Callback กลับมา
//         yield return null;
//         yield return null;

//         // --- Assert 1: เช็คว่า UI แสดงผลว่าเจอ Player 2 ---
//         string statusText = ui.statusText.text;
//         Debug.Log("UI Status: " + statusText); // ดู Log ได้

//         bool foundPlayer2 = statusText.Contains("Player 2");
//         Assert.IsTrue(foundPlayer2,
//             $"UI ควรแสดงข้อความว่าเจอ 'Player 2' แต่แสดงว่า: '{statusText}'");

//         // (Optional) Assert 3: เช็คว่าเตรียมเปลี่ยน Scene โดยการบันทึก GameID
//         Assert.AreEqual(101, PlayerPrefs.GetInt("CurrentGameId"), "GameID ต้องถูกบันทึกลง PlayerPrefs");
//     }
// }