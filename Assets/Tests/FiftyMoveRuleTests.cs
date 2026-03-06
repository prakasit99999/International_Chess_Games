using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using static ChessPiece;

public class FiftyMoveRuleTests
{
    private GameManager gameManager;
    private GameObject infoPanel;
    private TMP_Text infoText;
    private ChessBoard chessBoard;
    private ChessPiece dummyPiece;
    private GameObject dummyTilePrefab;
    private GameObject dummyPiecePrefab;
    private GameUIManager gameUIManager;

    [SetUp]
    public void Setup()
    {
        // ✅ Create dummy tilePrefab and piecePrefab
        dummyTilePrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
        dummyTilePrefab.name = "TilePrefab";

        dummyPiecePrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dummyPiecePrefab.name = "PiecePrefab";

        // ✅ Create ChessBoard and set static Instance
        chessBoard = new GameObject("ChessBoard").AddComponent<ChessBoard>();
        typeof(ChessBoard).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            ?.SetValue(null, chessBoard);

        // Set tilePrefab and piecePrefab using reflection if needed
        var tileField = typeof(ChessBoard).GetField("tilePrefab", BindingFlags.Public | BindingFlags.Instance);
        var pieceField = typeof(ChessBoard).GetField("piecePrefab", BindingFlags.Public | BindingFlags.Instance);
        tileField?.SetValue(chessBoard, dummyTilePrefab);
        pieceField?.SetValue(chessBoard, dummyPiecePrefab);

        // ✅ Create GameManager and set Instance
        gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        typeof(GameManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            ?.SetValue(null, gameManager);

        // ✅ GameUIManager
        var gameUIManagerGO = new GameObject("GameUIManager");
        gameUIManager = gameUIManagerGO.AddComponent<GameUIManager>();

        // Link to GameManager
        var uiManagerField = typeof(GameManager).GetField("gameUIManager", BindingFlags.NonPublic | BindingFlags.Instance);
        uiManagerField?.SetValue(gameManager, gameUIManager);

        // ✅  drawGamePanel
        var drawGamePanel = new GameObject("DrawGamePanel");
        var drawTxt = drawGamePanel.AddComponent<TextMeshProUGUI>();
        gameUIManager.drawGamePanel = drawGamePanel;
        gameUIManager.drawTxt = drawTxt;

        // ✅ winGamePanel
        var winGamePanel = new GameObject("WinGamePanel");
        var winTxt = winGamePanel.AddComponent<TextMeshProUGUI>();
        gameUIManager.winGamePanel = winGamePanel;
        gameUIManager.winTxt = winTxt;

        // ✅ loseGamePanel
        var loseGamePanel = new GameObject("LoseGamePanel");
        var loseTxt = loseGamePanel.AddComponent<TextMeshProUGUI>();
        gameUIManager.loseGamePanel = loseGamePanel;
        gameUIManager.loseTxt = loseTxt;

        // ✅ Draw Info UI
        infoPanel = new GameObject("DrawInfoPanel");
        infoText = infoPanel.AddComponent<TextMeshProUGUI>();
        gameUIManager.drawInfoPanel = infoPanel;
        gameUIManager.fiftyMoveText = infoText;
        Debug.Log($"drawInfoPanel: {gameUIManager.drawInfoPanel}");
        Debug.Log($"fiftyMoveText: {gameUIManager.fiftyMoveText}");
        Assert.AreSame(gameManager, GameManager.Instance, "GameManager.Instance ควรตรงกับ gameManager ที่สร้างในเทสต์");

        infoPanel.SetActive(false);

        // ✅ Dummy piece
        dummyPiece = new GameObject("DummyPiece").AddComponent<ChessPiece>();
        typeof(ChessBoard).GetField("selectedPiece", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(chessBoard, dummyPiece);
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameManager.gameObject);
        Object.DestroyImmediate(chessBoard.gameObject);
        Object.DestroyImmediate(dummyPiece.gameObject);
        Object.DestroyImmediate(infoPanel);
        Object.DestroyImmediate(dummyTilePrefab);
        Object.DestroyImmediate(dummyPiecePrefab);

        if (gameUIManager != null)
        {
            if (gameUIManager.drawGamePanel != null)
                Object.DestroyImmediate(gameUIManager.drawGamePanel);

            if (gameUIManager.winGamePanel != null)
                Object.DestroyImmediate(gameUIManager.winGamePanel);

            if (gameUIManager.loseGamePanel != null)
                Object.DestroyImmediate(gameUIManager.loseGamePanel);

            Object.DestroyImmediate(gameUIManager.gameObject);
        }
    }


    [Test]
    public void UpdateFiftyMoveCounter_IncrementsAndUpdatesUI()
    {
        dummyPiece.pieceType = PieceType.Knight;

        for (int i = 0; i < 30; i++)
            CallUpdateFiftyMoveRuleCounter(false);

        Assert.IsTrue(infoPanel.activeSelf, "ควรแสดง drawInfoPanel เมื่อ count >= 30");
        Assert.AreEqual("📏 กฎ 50 เดิน: 30/50", infoText.text);
    }


    [Test]
    public void UpdateFiftyMoveCounter_ResetsOnCapture()
    {
        dummyPiece.pieceType = PieceType.Knight;
        CallUpdateFiftyMoveRuleCounter(false);
        CallUpdateFiftyMoveRuleCounter(true);
        Assert.IsFalse(infoPanel.activeSelf);
    }

    [Test]
    public void UpdateFiftyMoveCounter_ResetsOnPawnMove()
    {
        dummyPiece.pieceType = PieceType.Pawn;
        CallUpdateFiftyMoveRuleCounter(false);
        Assert.IsFalse(infoPanel.activeSelf);
    }

    [Test]
    public void UpdateFiftyMoveCounter_ColorRed_When48()
    {
        dummyPiece.pieceType = PieceType.Knight;
        typeof(ChessBoard).GetField("_movesWithoutCaptureOrPawn", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(chessBoard, 47);
        CallUpdateFiftyMoveRuleCounter(false);

        Assert.AreEqual("📏 กฎ 50 เดิน: 48/50", infoText.text);
        Assert.AreEqual(Color.red, infoText.color);
    }

    [Test]
    public void UpdateFiftyMoveCounter_TriggersGameOver_WhenReach50()
    {
        dummyPiece.pieceType = PieceType.Knight;
        typeof(ChessBoard).GetField("_movesWithoutCaptureOrPawn", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(chessBoard, 49);
        LogAssert.Expect(LogType.Log, "เสมอ! 50 การเดินโดยไม่มีการยึดหรือเดินเบี้ย");
        CallUpdateFiftyMoveRuleCounter(false);
    }

    private void CallUpdateFiftyMoveRuleCounter(bool wasCapture)
    {
        var method = typeof(ChessBoard).GetMethod("UpdateFiftyMoveRuleCounter", BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(chessBoard, new object[] { wasCapture });
    }
}
