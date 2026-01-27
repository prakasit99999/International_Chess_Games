using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MatchmakingPollingTests
{
    // ======================================================================
    // 1. Mock API: จำลอง Server ที่ "รอแป๊บนึง" ถึงจะเจอคู่
    // ======================================================================
    public class MockPollingApi : MatchmakingApi
    {
        // ตัวแปรควบคุม simulation: ถ้าเป็น true แปลว่า Player 2 เข้ามาแล้ว
        public bool isPlayer2Ready = false;

        // JoinQueue: เข้ามาแล้วให้ "รอ" เสมอ (GameId = 0)
        public override IEnumerator JoinQueue(string username, int min, int max, System.Action<bool, MatchResponse> callback)
        {
            yield return null; // Delay 1 frame

            var response = new MatchResponse();
            response.message = "Waiting for opponent...";
            response.matchDetails = new MatchDetails
            {
                gameId = 0 // 0 = ยังไม่เจอ
            };

            callback(true, response);
        }

        // CheckQueue: จะถูกเรียกทุกๆ 2 วินาที
        public override IEnumerator CheckQueue(string username, System.Action<bool, MatchResponse> callback)
        {
            yield return null;

            if (isPlayer2Ready)
            {
                // === Simulation: ตอนนี้ Player 2 มาแล้ว ===
                var response = new MatchResponse();
                response.message = "Match Found!";
                response.matchDetails = new MatchDetails
                {
                    gameId = 888,
                    roomCode = 100,
                    opponentUsername = "Player 2",
                    color = "white",
                    gameType = "online_multiplayer"
                };
                callback(true, response);
            }
            else
            {
                // === Simulation: ยังไม่เจอ รอต่อไป ===
                var response = new MatchResponse { matchDetails = new MatchDetails { gameId = 0 } };
                callback(true, response);
            }
        }

        // Mock Cancel (กัน Error)
        public override IEnumerator CancelQueue(string username, System.Action<bool, string> callback)
        {
            yield return null;
            callback(true, "Cancelled");
        }
    }

    // ======================================================================
    // 2. Setup (เตรียมของ)
    // ======================================================================
    private GameObject testObject;
    private MatchmakingManager manager;
    private MatchmakingUi ui;
    private MockPollingApi mockApi;

    [SetUp]
    public void Setup()
    {
        testObject = new GameObject("Test_Polling_System");

        // ใส่ Mock API
        mockApi = testObject.AddComponent<MockPollingApi>();

        // ใส่ Manager & UI
        manager = testObject.AddComponent<MatchmakingManager>();
        ui = testObject.AddComponent<MatchmakingUi>();

        // สร้าง UI Elements จำลอง
        ui.panelMatchmaking = new GameObject("Panel");
        ui.statusText = new GameObject("StatusText").AddComponent<Text>();
        ui.btnMatchmakingStart = new GameObject("BtnStart").AddComponent<Button>();
        ui.btnMatchmakingCancel = new GameObject("BtnCancel").AddComponent<Button>();

        // เชื่อมต่อ
        manager.matchmakingUi = ui;
        ui.matchmakingManager = manager;

        // Login จำลอง
        PlayerPrefs.SetString("username", "Player 1");

        // Ignore known error from GameManager (since we don't need GameAPI in this matchmaking test)
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "❌ API ไม่ถูกพบ! ตรวจสอบว่า API อยู่ในฉาก");
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(testObject);
    }

    // ======================================================================
    // 3. The Polling Test Case
    // ======================================================================
    [UnityTest]
    public IEnumerator Test_Polling_Until_Player2_Joins()
    {
        // 1. กด Start
        manager.StartMatchmaking();

        // รอ 2 เฟรม (ให้ JoinQueue ตอบกลับมาว่า gameId=0)
        yield return null;
        yield return null;

        // --- จุดที่แก้ไข (Fixed Assert) ---
        // เพิ่ม "Searching" เข้าไป เพราะ Manager ของคุณใช้คำนี้
        bool isWaiting = ui.statusText.text.Contains("Joining") ||
                         ui.statusText.text.Contains("Waiting") ||
                         ui.statusText.text.Contains("Searching"); // <--- เพิ่มตัวนี้

        Assert.IsTrue(isWaiting, $"สถานะต้องเป็นการรอคิว แต่ได้ข้อความว่า: '{ui.statusText.text}'");

        // เช็คว่ายังไม่เจอ Player 2
        Assert.IsFalse(ui.statusText.text.Contains("Player 2"), "ตอนเริ่มต้องยังไม่เจอ Player 2");

        // 2. รอเวลาผ่านไปสักพัก (ยังไม่สั่งให้เจอ)
        yield return new WaitForSeconds(1.0f);

        // 3. สั่ง Mock ว่า Player 2 มาแล้ว
        mockApi.isPlayer2Ready = true;

        // 4. รอนานกว่า 2 วินาที (เพื่อให้ Manager วนลูป Polling มาเจอ)
        yield return new WaitForSeconds(2.5f);

        // 5. ตรวจสอบผลลัพธ์
        string finalStatus = ui.statusText.text;
        Debug.Log("Status Check: " + finalStatus);

        Assert.IsTrue(finalStatus.Contains("Player 2"),
            $"UI ควรอัปเดตเป็น Player 2 แต่แสดง: '{finalStatus}'");

        Assert.AreEqual(888, PlayerPrefs.GetInt("CurrentGameId"));
    }
}