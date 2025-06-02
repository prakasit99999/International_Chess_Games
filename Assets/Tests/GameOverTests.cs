using NUnit.Framework;
using UnityEngine;
using TMPro;
using static ChessPiece;

public class GameOverTests
{
    private GameManager gameManager;
    private GameObject winPanel;
    private GameObject losePanel;
    private GameObject drawPanel;
    private GameManager drawInfoPanel;
    private TMP_Text winText;
    private TMP_Text loseText;
    private TMP_Text drawText;
    private TMP_Text fiftyMoveText;

    [SetUp]
    public void Setup()
    {
        // สร้าง GameManager จำลอง
        gameManager = new GameObject("GameManager").AddComponent<GameManager>();

        // สร้าง Draw Panel (drawGamePanel)
        drawPanel = new GameObject("DrawGamePanel");
        drawText = drawPanel.AddComponent<TextMeshProUGUI>();
        gameManager.drawGamePanel = drawPanel;
        gameManager.drawTxt = drawText;

        // สร้าง Win Panel
        winPanel = new GameObject("WinGamePanel");
        winText = winPanel.AddComponent<TextMeshProUGUI>();
        gameManager.winGamePanel = winPanel;
        gameManager.winTxt = winText;

        // สร้าง Lose Panel
        losePanel = new GameObject("LoseGamePanel");
        loseText = losePanel.AddComponent<TextMeshProUGUI>();
        gameManager.loseGamePanel = losePanel;
        gameManager.loseTxt = loseText;

        // ตั้งชื่อผู้เล่น
        gameManager.SetPlayerNames("PlayerWhite", "PlayerBlack");

        // ปิด panel ทั้งหมดเริ่มต้น
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        drawPanel.SetActive(false);
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameManager.gameObject);
        Object.DestroyImmediate(winPanel);
        Object.DestroyImmediate(losePanel);
        Object.DestroyImmediate(drawPanel);
    }

    [Test]
    public void GameOver_WhiteWins_ShowsWinPanel()
    {
        gameManager.SetCurrentTurn(Team.White);
        gameManager.GameOver(Team.White);

        Assert.IsTrue(winPanel.activeSelf, "Win panel ควรแสดง");
        Assert.IsFalse(losePanel.activeSelf, "Lose panel ไม่ควรแสดง");
        Assert.IsFalse(drawPanel.activeSelf, "Draw panel ไม่ควรแสดง");
        Assert.AreEqual("PlayerWhite ชนะ!", winText.text);
    }

    [Test]
    public void GameOver_BlackWins_ShowsLosePanelForWhite()
    {
        gameManager.SetCurrentTurn(Team.White);
        gameManager.GameOver(Team.Black);

        Assert.IsTrue(losePanel.activeSelf, "Lose panel ควรแสดง");
        Assert.IsFalse(winPanel.activeSelf, "Win panel ไม่ควรแสดง");
        Assert.AreEqual("PlayerWhite แพ้!", loseText.text);
    }

    [Test]
    public void GameOver_Draw_ShowsDrawPanel()
    {
        gameManager.GameOver(Team.None);

        Assert.IsTrue(drawPanel.activeSelf);
        Assert.IsFalse(winPanel.activeSelf);
        Assert.IsFalse(losePanel.activeSelf);
        Assert.AreEqual("⚖️ เกมเสมอ!", drawText.text);
    }

    [Test]
    public void GameOver_StopsTimeScale()
    {
        Time.timeScale = 1f;
        gameManager.GameOver(Team.White);
        Assert.AreEqual(0f, Time.timeScale);
    }

    [Test]
    public void GameOver_CalledTwice_OnlyFirstProcessed()
    {
        gameManager.SetCurrentTurn(Team.White);
        gameManager.GameOver(Team.White);
        string firstWinText = winText.text;

        // เรียก GameOver อีกครั้งด้วย draw
        gameManager.GameOver(Team.None);

        Assert.AreEqual(firstWinText, winText.text, "ควรใช้ข้อความเดิมจากครั้งแรก");
        Assert.IsFalse(drawPanel.activeSelf, "Draw panel ไม่ควรแสดงซ้ำ");
    }

    [Test]
    public void GameOver_DrawByFiftyMove_ShowsCorrectMessage()
    {
        gameManager.GameOver(Team.None);
        gameManager.drawTxt.text = "⚖️ เสมอจากกฎ 50 เดิน!";

        Assert.AreEqual("⚖️ เสมอจากกฎ 50 เดิน!", drawText.text);
        Assert.IsTrue(drawPanel.activeSelf);
    }

    [Test]
    public void GameOver_Draw_DoesNotShowWinOrLosePanel()
    {
        gameManager.GameOver(Team.None);

        Assert.IsTrue(drawPanel.activeSelf);
        Assert.IsFalse(winPanel.activeSelf);
        Assert.IsFalse(losePanel.activeSelf);
    }

    [Test]
    public void GameOver_BlackWins_ShowsCorrectLoserName()
    {
        gameManager.SetPlayerNames("WhitePlayer", "BlackPlayer");
        gameManager.SetCurrentTurn(Team.White);
        gameManager.GameOver(Team.Black);

        Assert.AreEqual("WhitePlayer แพ้!", loseText.text);
    }
}
