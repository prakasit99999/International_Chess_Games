using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameManagerTests
{
    private GameObject gameGameObject;
    private GameManager gameManager;
    private MockGameAPI mockGameApi;
    private MockMovesAPI mockMovesApi;



    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // 1. ✅ สร้าง ChessBoard หลอก (สำคัญมาก ไม่งั้น Start() พัง)
        var boardGO = new GameObject("ChessBoard");
        boardGO.AddComponent<ChessBoard>();

        // 2. สร้าง GameManager
        gameGameObject = new GameObject("GameManager_Test");
        gameManager = gameGameObject.AddComponent<GameManager>();

        // 3. ติด Mock API
        mockGameApi = gameGameObject.AddComponent<MockGameAPI>();
        mockMovesApi = gameGameObject.AddComponent<MockMovesAPI>();

        // 4. เชื่อม Dependency
        gameManager.gameAPI = mockGameApi;
        gameManager.movesApi = mockMovesApi;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // ล้างค่าทิ้งเมื่อจบแต่ละ Test
        Object.Destroy(gameGameObject);
        yield return null;
    }

    // ✅ Test Case: Checklist ข้อ 2 (Start Game)
    [UnityTest]
    public IEnumerator T01_StartGame_Should_Get_GameId()
    {
        // Act: สั่งเริ่มเกม
        gameManager.ModeSelect();

        // Wait: รอ Coroutine ทำงาน
        yield return null;

        // Assert: ตรวจสอบผลลัพธ์
        Assert.IsTrue(mockGameApi.CreateGameCalled, "GameAPI.CreateGame ต้องถูกเรียก");
        Assert.AreEqual(999, gameManager.currentGameId, "GameManager ต้องได้รับ GameID 999 จาก Mock");
        Assert.IsTrue(gameManager.isGameStarted, "IsGameStarted ต้องเป็น True");
    }

    // ✅ Test Case: Checklist ข้อ 5 (Move Recording)
    [UnityTest]
    public IEnumerator T02_RecordMove_Should_Store_In_List()
    {
        // Arrange: เริ่มเกมเพื่อให้ได้ ID ก่อน
        gameManager.currentGameId = 999;

        // Act: จำลองการเดินหมาก
        var fakeMove = new HistoryMove.HistoryMoveData(); // ใส่ค่า fake ตามโครงสร้าง
        gameManager.RecordMove(fakeMove, null);

        // Assert
        Assert.AreEqual(1, gameManager.moveCount, "MoveCount ต้องเพิ่มเป็น 1");
        // *หมายเหตุ: ต้องเปลี่ยน recordedMoves เป็น public หรือใช้ Reflection เพื่อเช็ค Count ของ List
        return null;
    }



    // ✅ Test Case: Checklist ข้อ 6 & 9 (Game Over & Batch Upload)
    [UnityTest]
    public IEnumerator T03_GameOver_Should_Send_Batch_Then_EndGame()
    {
        // Arrange
        gameManager.currentGameId = 999;
        gameManager.RecordMove(new HistoryMove.HistoryMoveData(), null); // เดิน 1 ตา

        // Act
        gameManager.GameOver(ChessPiece.Team.White, "checkmate");

        // Wait: รอ API ปลอมทำงาน (อาจต้องรอหลายเฟรมเพราะมี yield return ซ้อนกัน)
        yield return null;
        yield return null;
        yield return null;

        // Assert
        Assert.IsTrue(mockMovesApi.BatchSent, "ต้องมีการส่ง Batch Moves");
        Assert.AreEqual(1, mockMovesApi.MoveCountReceived, "จำนวน Move ที่ส่งต้องเท่ากับ 1");
        Assert.IsTrue(mockGameApi.EndGameCalled, "ต้องเรียก EndGame หลังส่ง Moves เสร็จ");
        Assert.IsTrue(gameManager.gameIsOver, "สถานะเกมต้องจบ");
    }
}