using NUnit.Framework;
using UnityEngine;
using static ChessPiece;
using System.Reflection; 

public class ChessLogicTests
{
    private ChessBoard chessBoard;
    private GameManager gameManager;
    private GameObject boardObject;
    private GameObject managerObject;

    [SetUp]
    public void Setup()
    {
        // --- ส่วน Setup ---
        // สร้าง Object ที่จำเป็นสำหรับการทดสอบ
        boardObject = new GameObject("TestChessBoard");
        chessBoard = boardObject.AddComponent<ChessBoard>();

        managerObject = new GameObject("TestGameManager");
        gameManager = managerObject.AddComponent<GameManager>();
        GameManager.Instance = gameManager; // ตั้งค่า Singleton

        // 🔹 ใช้ Reflection เพื่อเชื่อม GameManager เข้ากับ ChessBoard (เนื่องจากเป็น private)
        var gameManagerField = typeof(ChessBoard).GetField("gameManager", BindingFlags.NonPublic | BindingFlags.Instance);
        gameManagerField?.SetValue(chessBoard, gameManager);
    }

    [TearDown]
    public void Teardown()
    {
        // ทำลาย Object หลัง Test เสร็จสิ้น
        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(managerObject);
    }

    // ========== TEST CASES ==========

    [Test]
    public void IsKingInCheck_BlackKingInCheck_ReturnsTrue()
    {
        // Arrange: ใช้ Helper ในการตั้งค่ากระดาน
        string fen = "4k3/8/8/8/8/8/8/R3K3 w Q - 0 1";
        ChessBoardTestHelper.LoadFen(chessBoard, fen);

        // Act
        bool isBlackKingInCheck = chessBoard.IsKingInCheck(Team.Black);

        // Assert
        Assert.IsTrue(isBlackKingInCheck, "คิงดำควรจะอยู่ในสถานะรุก");
    }

    [Test]
    public void Checkmate_BackRankMate_KingIsInCheckAndHasZeroLegalMoves()
    {
        // Arrange: ตั้งค่ากระดานที่จะเกิด Back-rank mate
        string fen = "6k1/8/8/8/8/8/R7/4K3 w - - 0 1";
        ChessBoardTestHelper.LoadFen(chessBoard, fen);

        // Act: ขาวเดินเรือจาก a2 ไป a8 เพื่อรุกฆาต
        ChessBoardReflectionHelper.MovePiece(chessBoard, new Vector2Int(0, 1), new Vector2Int(0, 7));

        // Assert: ตรวจสอบ 2 เงื่อนไขของการรุกฆาต
        // 1. คิงดำต้องอยู่ในสถานะ "รุก"
        Assert.IsTrue(chessBoard.IsKingInCheck(Team.Black), "คิงดำควรจะถูกรุกโดยเรือ");

        // 2. ทีมสีดำต้องไม่มีตาเดินที่ถูกกฎหมายเหลืออยู่เลย
        var blackLegalMoves = ChessBoardReflectionHelper.GetAllLegalMovesForTeam(chessBoard, Team.Black);
        Assert.AreEqual(0, blackLegalMoves.Count, "ทีมสีดำไม่ควรมีตาเดินที่ถูกกฎหมายเหลืออยู่");
    }

    [Test]
    public void Stalemate_KingIsTrapped_KingIsNotInCheckAndHasZeroLegalMoves()
    {
        // Arrange: ตั้งค่ากระดานที่จะเกิด Stalemate (อับ)
        string fen = "7k/5Q2/5K2/8/8/8/8/8 b - - 0 1";
        ChessBoardTestHelper.LoadFen(chessBoard, fen);

        // Act: ไม่มีการเดิน เพราะเป็นตาของสีดำและเดินไม่ได้แล้ว

        // Assert: ตรวจสอบ 2 เงื่อนไขของการอับ
        // 1. คิงดำต้อง "ไม่" อยู่ในสถานะรุก
        Assert.IsFalse(chessBoard.IsKingInCheck(Team.Black), "คิงดำไม่ควรจะอยู่ในสถานะรุก");

        // 2. ทีมสีดำต้องไม่มีตาเดินที่ถูกกฎหมายเหลืออยู่เลย
        var blackLegalMoves = ChessBoardReflectionHelper.GetAllLegalMovesForTeam(chessBoard, Team.Black);
        Assert.AreEqual(0, blackLegalMoves.Count, "ทีมสีดำไม่ควรมีตาเดินที่ถูกกฎหมายเหลืออยู่ (Stalemate)");
    }
}