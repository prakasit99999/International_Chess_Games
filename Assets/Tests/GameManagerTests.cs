using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameManagerTests
{
    private GameObject gameGameObject;
    private GameModeManager gameModeManager;
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


}