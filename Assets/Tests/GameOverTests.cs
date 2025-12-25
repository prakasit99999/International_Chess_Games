using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ChessPiece;

public class GameOverTests
{
    private GameManager gameManager;
    private ChessBoard chessBoard;
    private HistoryMove historyMove;
    private HistoryMoveUI historyMoveUI;
    private GameObject winPanel;
    private GameObject losePanel;
    private GameObject drawPanel;
    private GameObject drawInfoPanel;
    private TMP_Text winText;
    private TMP_Text loseText;
    private TMP_Text drawText;
    private TMP_Text fiftyMoveText;

    [SetUp]
    public void Setup()
    {
        // GameManager
        var gameManagerGO = new GameObject("GameManager");
        gameManager = gameManagerGO.AddComponent<GameManager>();

        MethodInfo awakeMethod = typeof(GameManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        awakeMethod.Invoke(gameManager, null);

        // ChessBoard
        var chessBoardGO = new GameObject("ChessBoard");
        chessBoard = chessBoardGO.AddComponent<ChessBoard>();
        var pieceGO = new GameObject("Pawn");
        var piece = pieceGO.AddComponent<ChessPiece>();
        piece.SetBoardManager(chessBoard);

        awakeMethod = typeof(ChessBoard).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        awakeMethod.Invoke(chessBoard, null);

        FieldInfo chessBoardField = typeof(GameManager).GetField("chessBoard", BindingFlags.Instance | BindingFlags.NonPublic);
        chessBoardField?.SetValue(gameManager, chessBoard);

        // HistoryMove
        var historyMoveGO = new GameObject("HistoryMove");
        historyMove = historyMoveGO.AddComponent<HistoryMove>();

        // HistoryMoveUI
        var historyMoveUIGO = new GameObject("HistoryMoveUI");
        historyMoveUI = historyMoveUIGO.AddComponent<HistoryMoveUI>();

        awakeMethod = typeof(HistoryMoveUI).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        awakeMethod.Invoke(historyMoveUI, null);

        // ✅ ScrollRect + Content
        var scrollRectGO = new GameObject("ScrollRect");
        var scrollRect = scrollRectGO.AddComponent<ScrollRect>();

        var contentGO = new GameObject("ScrollContent");
        var contentRect = contentGO.AddComponent<RectTransform>();

        scrollRect.content = contentRect;
        historyMoveUI.scrollRect = scrollRect;
        historyMoveUI.contentParent = contentRect.transform;

        // ✅ MoveEntryPrefab + PlayerTurnText
        historyMoveUI.moveEntryPrefab = new GameObject("MoveEntryPrefab");
        historyMoveUI.moveEntryPrefab.AddComponent<TextMeshProUGUI>();
        historyMoveUI.moveEntryPrefab.AddComponent<Button>();
        historyMoveUI.playerTurnText = new GameObject("PlayerTurnText").AddComponent<TextMeshProUGUI>();

        // ✅ ChessPiece prefab
        var mockPiecePrefab = new GameObject("MockPiece");
        mockPiecePrefab.AddComponent<ChessPiece>();
        mockPiecePrefab.AddComponent<SpriteRenderer>();
        chessBoard.piecePrefab = mockPiecePrefab;

        chessBoard.whiteSprites = new Sprite[6];
        chessBoard.blackSprites = new Sprite[6];
        chessBoard.pieceWhite = new GameObject("WhiteGroup").transform;
        chessBoard.pieceBlack = new GameObject("BlackGroup").transform;

        // ✅ Panels
        winPanel = new GameObject("WinGamePanel");
        winText = winPanel.AddComponent<TextMeshProUGUI>();
        gameManager.winGamePanel = winPanel;
        gameManager.winTxt = winText;

        losePanel = new GameObject("LoseGamePanel");
        loseText = losePanel.AddComponent<TextMeshProUGUI>();
        gameManager.loseGamePanel = losePanel;
        gameManager.loseTxt = loseText;

        drawPanel = new GameObject("DrawGamePanel");
        drawText = drawPanel.AddComponent<TextMeshProUGUI>();
        gameManager.drawGamePanel = drawPanel;
        gameManager.drawTxt = drawText;

        drawInfoPanel = new GameObject("DrawInfoPanel");
        fiftyMoveText = drawInfoPanel.AddComponent<TextMeshProUGUI>();
        gameManager.drawInfoPanel = drawInfoPanel;
        gameManager.fiftyMoveText = fiftyMoveText;

        // ✅ Player Names
        gameManager.SetPlayerNames("PlayerWhite", "PlayerBlack");

        // ปิด panels เริ่มต้น
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        drawPanel.SetActive(false);
        drawInfoPanel.SetActive(false);
    }


    [TearDown]
    public void Teardown()
    {
        // คืนค่า TimeScale
        Time.timeScale = 1f;

        // ลบ GameObject ทั้งหมด
        if (winPanel != null) Object.DestroyImmediate(winPanel);
        if (losePanel != null) Object.DestroyImmediate(losePanel);
        if (drawPanel != null) Object.DestroyImmediate(drawPanel);
        if (drawInfoPanel != null) Object.DestroyImmediate(drawInfoPanel);

        if (historyMove != null) Object.DestroyImmediate(historyMove.gameObject);
        if (historyMoveUI != null) Object.DestroyImmediate(historyMoveUI.gameObject);

        if (gameManager != null) Object.DestroyImmediate(gameManager.gameObject);
        if (chessBoard != null) Object.DestroyImmediate(chessBoard.gameObject);
    }

    [Test]
    public void GameOver_WhiteWins_ShowsWinPanel()
    {
        // จัดเตรียม
        gameManager.SetCurrentTurn(Team.White);

        // กระทำ
        gameManager.GameOver(Team.White, "checkmate");

        // ตรวจสอบ
        Assert.IsTrue(winPanel.activeSelf);
        Assert.IsFalse(losePanel.activeSelf);
        Assert.IsFalse(drawPanel.activeSelf);
        Assert.AreEqual("PlayerWhite ชนะ!", winText.text);
    }

    [Test]
    public void GameOver_BlackWins_ShowsLosePanelForWhite()
    {
        // จัดเตรียม
        gameManager.SetCurrentTurn(Team.White);

        // กระทำ
        gameManager.GameOver(Team.Black, "checkmate");

        // ตรวจสอบ
        Assert.IsTrue(losePanel.activeSelf);
        Assert.IsFalse(winPanel.activeSelf);
        Assert.IsFalse(drawPanel.activeSelf);
        Assert.AreEqual("PlayerWhite แพ้!", loseText.text);
    }

    [Test]
    public void GameOver_Draw_ShowsDrawPanel()
    {
        // กระทำ
        gameManager.GameOver(Team.None, "stalemate");

        // ตรวจสอบ
        Assert.IsTrue(drawPanel.activeSelf);
        Assert.IsFalse(winPanel.activeSelf);
        Assert.IsFalse(losePanel.activeSelf);
        Assert.AreEqual("⚖️ เกมเสมอ!", drawText.text);
    }

    [Test]
    public void GameOver_StopsTimeScale()
    {
        // จัดเตรียม
        Time.timeScale = 1f;

        // กระทำ
        gameManager.GameOver(Team.White, "checkmate");

        // ตรวจสอบ
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void GameOver_CalledTwice_OnlyFirstProcessed()
    {
        // จัดเตรียม
        gameManager.SetCurrentTurn(Team.White);

        // กระทำครั้งแรก
        gameManager.GameOver(Team.White, "checkmate");
        string firstWinText = winText.text;

        // กระทำครั้งที่สอง
        gameManager.GameOver(Team.None, "stalemate");

        // ตรวจสอบ
        Assert.AreEqual(firstWinText, winText.text);
        Assert.IsFalse(drawPanel.activeSelf);
    }

    [Test]
    public void GameOver_BlackWins_ShowsCorrectLoserName()
    {
        // จัดเตรียม
        gameManager.SetPlayerNames("WhitePlayer", "BlackPlayer");
        gameManager.SetCurrentTurn(Team.White);

        // กระทำ
        gameManager.GameOver(Team.Black, "checkmate");

        // ตรวจสอบ
        Assert.AreEqual("WhitePlayer แพ้!", loseText.text);
    }

    [Test]
    public void GameOver_Draw_DoesNotShowWinOrLosePanel()
    {
        // กระทำ
        gameManager.GameOver(Team.None, "stalemate");

        // ตรวจสอบ
        Assert.IsTrue(drawPanel.activeSelf);
        Assert.IsFalse(winPanel.activeSelf);
        Assert.IsFalse(losePanel.activeSelf);
    }

    [Test]
    public void GameOver_ResetsFiftyMoveUI()
    {
        // จัดเตรียม
        gameManager.UpdateFiftyMoveCounter(30);

        // กระทำ
        gameManager.GameOver(Team.White, "checkmate");

        // ตรวจสอบ
        Assert.IsFalse(drawInfoPanel.activeSelf);
    }
}