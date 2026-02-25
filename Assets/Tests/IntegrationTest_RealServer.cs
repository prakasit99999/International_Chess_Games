using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class IntegrationTest_RealServer
{
    // ตัวแปรสำหรับเก็บ Object และ API
    private GameObject testObject;
    private GameAPI gameApi;
    private AiPerformanceAPI perfApi;
    private MovesAPI movesApi; // ✅ เพิ่ม MovesAPI

    // ตัวแปรเก็บค่าระหว่างการทดสอบ
    private int realGameId = -1;
    private bool apiRequestSuccess = false;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // 1. สร้าง GameObject จำลอง
        testObject = new GameObject("IntegrationTestObject");

        // 2. ติด Component API ที่เราเขียนไว้
        gameApi = testObject.AddComponent<GameAPI>();
        perfApi = testObject.AddComponent<AiPerformanceAPI>();
        movesApi = testObject.AddComponent<MovesAPI>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // ล้างค่าหลังจากจบ Test
        if (testObject != null)
        {
            Object.Destroy(testObject);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator Test_FullFlow_SinglePlayer()
    {
        // ========================================================
        // 🏁 STEP 1: สร้างเกม (Start Game - Single Player)
        // ========================================================
        Debug.Log("🚀 [Step 1] Creating Single Player Game...");

        var createDto = new GameCreateDto
        {
            GameType = "single_player",

            // White = Human (Guest ส่ง 0)
            WhitePlayerType = "human",
            WhitePlayerId = 0,

            // Black = AI
            BlackPlayerType = "ai_hard",
            BlackPlayerId = 0
        };

        apiRequestSuccess = false;
        yield return gameApi.CreateOfflineGame(createDto, (id) =>
        {

            apiRequestSuccess = true;
        });

        Assert.IsTrue(apiRequestSuccess, "❌ Create Game Failed: API did not return success.");
        Assert.Greater(realGameId, 0, $"❌ Invalid GameID returned: {realGameId}");
        Debug.Log($"✅ [Step 1 Passed] Game Created! ID: {realGameId}");
        // ========================================================
        // ♟️ STEP 2: ส่งข้อมูลการเดินหมาก (Batch Moves)
        // ========================================================
        Debug.Log("🚀 [Step 2] Sending Moves Batch...");

        if (movesApi != null)
        {
            var moves = new List<MoveCreateDto>
            {
                // --- Move 1: White (Human) ---
                new MoveCreateDto
                {
                    GameId = realGameId,
                    MoveNumber = 1,

                    // เดินเบี้ย e2 -> e4
                    StartX = 4, StartY = 1,
                    EndX = 4, EndY = 3,

                    PieceType = 0,           // 0 = Pawn
                    PlayerTurn = 0,          // 0 = White

                    // ⚠️ คนเดิน: ส่ง AlgorithmType เป็น 0 (None)
                    AlgorithmType = 0,

                    // ค่า Default อื่นๆ (เปลี่ยนจาก string "None" เป็นเลข 0)
                    CapturedPieceType = 0,   // 0 = None
                    CapturedPieceTeam = 0,   // 0 = None
                    PromotedTo = 0,          // 0 = None
                    PromotedFrom = 0,        // 0 = None

                    IsCapture = false,
                    IsCheck = false,
                    IsEnPassant = false,
                    IsCastling = false,
                    IsPawnTwoStep = true,
                    PieceHasMovedBefore = false,

                    AiEvaluationScore = 0,
                    AiDepthSearched = 0,
                    AiNodesEvaluated = 0,
                    MoveTimeMilliseconds = 100
                },

                // --- Move 2: Black (AI) ---
                new MoveCreateDto
                {
                    GameId = realGameId,
                    MoveNumber = 2,

                    // AI เดินเบี้ย e7 -> e5
                    StartX = 4, StartY = 6,
                    EndX = 4, EndY = 4,

                    PieceType = 0,           // 0 = Pawn
                    PlayerTurn = 1,          // 1 = Black

                    // ⚠️ AI เดิน: 1 = Minimax, 2 = AlphaBeta
                    AlgorithmType = 1,

                    CapturedPieceType = 0,   // 0 = None
                    CapturedPieceTeam = 0,   // 0 = None
                    PromotedTo = 0,
                    PromotedFrom = 0,

                    IsCapture = false,
                    IsCheck = false,
                    IsEnPassant = false,
                    IsCastling = false,
                    IsPawnTwoStep = true,
                    PieceHasMovedBefore = false,

                    // ข้อมูล AI
                    AiEvaluationScore = (int)10,
                    AiDepthSearched = 3,
                    AiNodesEvaluated = 1500,
                    MoveTimeMilliseconds = 250
                }
            };

            bool moveSuccess = false;
            yield return movesApi.SendMovesBatch(moves, (success) => moveSuccess = success);

            Assert.IsTrue(moveSuccess, "❌ Send Moves Batch Failed");
            Debug.Log($"✅ [Step 2 Passed] Sent {moves.Count} moves successfully.");
        }

        // ========================================================
        // 🏆 STEP 3: จบเกม (End Game)
        // ========================================================
        Debug.Log("🚀 [Step 3] Ending Game...");

        var resultDto = new GameResultDto
        {
            GameId = realGameId,
            Result = "White",       // สมมติว่า White ชนะ
            ResultReason = "Checkmate"
        };

        apiRequestSuccess = false;
        yield return gameApi.FinalizeOfflineGame(resultDto, (id) => apiRequestSuccess = true);

        Assert.IsTrue(apiRequestSuccess, "❌ End Game Failed");
        Debug.Log("✅ [Step 3 Passed] Game Ended Successfully.");

        // ========================================================
        // 📊 STEP 4: ส่งสถิติ AI (AI Performance)
        // ========================================================
        Debug.Log("🚀 [Step 4] Sending AI Stats...");

        var perfData = new AiPerformanceData
        {
            GameId = realGameId,
            AiLevel = "Hard",
            AlgorithmType = "Minimax",
            AverageDepth = 3.5f,
            AverageNodesEvaluated = 15000,

            // ⚠️ ส่งเป็น int (เช่น 250) ห้ามส่งทศนิยม
            AverageMoveTimeMs = 250,
            TotalMoves = 2
        };

        // เรียกใช้ API ส่ง Performance
        // (AiPerformanceAPI.cs ของคุณเป็น Coroutine ที่ไม่มี callback แต่ถ้าส่งผ่านจะ Log Success)
        yield return perfApi.SendPerformance(perfData);

        Debug.Log("✅ [Step 4 Passed] AI Stats Request Sent.");
        Debug.Log("🎉🎉 TEST COMPLETE: System Integrated Successfully! 🎉🎉");
    }
}