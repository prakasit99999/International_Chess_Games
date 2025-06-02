using System.Collections;
using System.Collections.Generic;
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

    [SetUp]
    public void Setup()
    {
        gameManager = new GameObject().AddComponent<GameManager>();
        infoPanel = new GameObject("DrawInfoPanel");
        infoText = infoPanel.AddComponent<TextMeshProUGUI>();
        gameManager.drawInfoPanel = infoPanel;
        gameManager.fiftyMoveText = infoText;

        // ChessBoard
        chessBoard = new GameObject("ChessBoard").AddComponent<ChessBoard>();
        typeof(ChessBoard).GetField("Instance").SetValue(null, chessBoard); // จำลอง Singleton

        // Set GameManager.Instance
        typeof(GameManager).GetField("Instance").SetValue(null, gameManager);

        // Dummy piece
        dummyPiece = new GameObject("DummyPiece").AddComponent<ChessPiece>();
        chessBoard.SetselectedPiece(dummyPiece);

        infoPanel.SetActive(false);
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(gameManager.gameObject);
        Object.Destroy(infoPanel);
        Object.Destroy(chessBoard.gameObject);
        Object.Destroy(dummyPiece.gameObject);
    }

    [Test]
    public void UpdateFiftyMoveCounter_IncrementsAndUpdatesUI()
    {
        dummyPiece.pieceType = PieceType.Knight;
        for (int i = 0; i < 3; i++)
        {
            CallUpdateFiftyMoveRuleCounter(wasCapture: false);
        }

        Assert.IsTrue(infoPanel.activeSelf, "ควรแสดง drawInfoPanel");
        Assert.AreEqual("📏 กฎ 50 เดิน: 3/50", infoText.text);

    }

    [Test]
    public void UpdateFiftyMoveCounter_ResetsOnCapture()
    {
        dummyPiece.pieceType = PieceType.Knight;

        CallUpdateFiftyMoveRuleCounter(false); // เดิน 1 ครั้ง
        CallUpdateFiftyMoveRuleCounter(true);  // มีการกิน

        Assert.IsFalse(infoPanel.activeSelf, "ควรซ่อน drawInfoPanel");
        //Assert.AreEqual("📏 กฎ 50 เดิน: 1/50", infoText.text); // UI ยังมีค่าเก่าอยู่
    }

    [Test]
    public void UpdateFiftyMoveCounter_ResetsOnPawnMove()
    {
        dummyPiece.pieceType = PieceType.Pawn;

        CallUpdateFiftyMoveRuleCounter(false);

        Assert.IsFalse(infoPanel.activeSelf, "ควรซ่อน drawInfoPanel เมื่อลากเบี้ย");
    }

    [Test]
    public void UpdateFiftyMoveCounter_ColorRed_When48()
    {
        dummyPiece.pieceType = PieceType.Knight;

        typeof(ChessBoard)
            .GetField("_movesWithoutCaptureOrPawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(chessBoard, 47);

        CallUpdateFiftyMoveRuleCounter(false);

        Assert.AreEqual("📏 กฎ 50 เดิน: 48/50", infoText.text);
        Assert.AreEqual(Color.red, infoText.color);
    }


    [Test]
    public void UpdateFiftyMoveCounter_TriggersGameOver_WhenReach50()
    {
        dummyPiece.pieceType = PieceType.Knight;

        // จัดให้ตัวแปรภายในเป็น 49 ก่อน
        typeof(ChessBoard)
            .GetField("_movesWithoutCaptureOrPawn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(chessBoard, 49);

        LogAssert.Expect(LogType.Log, "เสมอ! 50 การเดินโดยไม่มีการยึดหรือเดินเบี้ย");
        CallUpdateFiftyMoveRuleCounter(false);
    }

    // 👇 ช่วยเรียก method ที่เป็น private ได้
    private void CallUpdateFiftyMoveRuleCounter(bool wasCapture)
    {
        var method = typeof(ChessBoard).GetMethod("UpdateFiftyMoveRuleCounter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(chessBoard, new object[] { wasCapture });
    }

}
