using System.Collections.Generic;
using AIEngine.Evaluation;
using AIEngine.Algorithms;
using AIEngine.Utilities;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class AISearchTests
{
    /// <summary>
    /// TC-AI01: ทดสอบ Evaluation Function ในสถานะกระดานที่กำหนด
    /// ระบบควรให้คะแนนเป็นบวกแก่ฝ่ายที่ได้เปรียบ และเป็นลบแก่ฝ่ายที่เสียเปรียบ
    /// </summary>
    [Test]
    public void TC_AI01_Evaluation_MaterialAdvantage()
    {
        var board = new ChessBoardModel();
        // เริ่มต้นบอร์ดควรจะใกล้เคียง 0
        float initialScore = Evaluation.Evaluate(board);

        // ขาวได้เปรียบ (เอา Queen ดำออก)
        board.Board[0, 3] = 0;
        float whiteAdvantageScore = Evaluation.Evaluate(board);
        Assert.Greater(whiteAdvantageScore, initialScore, "Score should increase when White has material advantage.");

        // ดำได้เปรียบ (เอา Queen ขาวออก และคืน Queen ดำ)
        board.Board[0, 3] = -5;
        board.Board[7, 3] = 0;
        board.IsWhiteTurn = false; // เปลี่ยนเป็นตาดำเดิน
        float blackAdvantageScore = Evaluation.Evaluate(board);
        // Note: Evaluate() คืนค่ามุมมองของฝ่ายที่กำลังเดิน ถ้าดำได้เปรียบและเป็นตาดำเดิน คะแนนควรเป็นบวกในมุมมองดำ?
        // อ้างอิงโค้ด Evaluation.cs: return board.IsWhiteTurn ? finalScore : -finalScore;
        // ถ้าดำได้เปรียบ finalScore จะติดลบ (เช่น -900) แล้วคูณ -1 จะได้ +900
        Assert.Greater(blackAdvantageScore, 0, "Evaluation should return positive relative score for the side to move if they are advantaged.");
    }

    /// <summary>
    /// TC-AI02: ทดสอบการทำงานของอัลกอริทึม Minimax
    /// ระบบสามารถคำนวณและคืนค่าการเดินหมากที่ถูกต้องตามกติกา
    /// </summary>
    [Test]
    public void TC_AI02_Minimax_ReturnsValidMove()
    {
        var board = new ChessBoardModel();
        var minimax = new Minimax(timeLimitMs: 2000);

        var result = minimax.FindBestMoveWithMetrics(board, depth: 2);

        Assert.IsNotNull(result.Move, "Minimax should return a move.");
        Assert.IsTrue(result.NodesEvaluated > 0, "Minimax should evaluate at least some nodes.");

        // ตรวจสอบว่าเป็นท่าเดินที่ถูกกฎหมาย (โดยการลอง MakeMove)
        Assert.DoesNotThrow(() => {
            var clone = board.Clone();
            clone.MakeMove(result.Move);
        }, "The move returned by Minimax should be valid and applicable to the board.");
    }

    /// <summary>
    /// TC-AI03: เปรียบเทียบผลลัพธ์ของ Minimax และ Alpha-Beta Pruning
    /// การเดินหมากควรเหมือนกัน และ Alpha-Beta ควรใช้จำนวนโหนดน้อยกว่า
    /// </summary>
    [Test]
    public void TC_AI03_Minimax_Vs_AlphaBeta()
    {
        var board = new ChessBoardModel();
        var minimax = new Minimax(timeLimitMs: 5000);
        var alphaBeta = new AlphaBeta(timeLimitMs: 5000);
        int depth = 3;

        var mmResult = minimax.FindBestMoveWithMetrics(board, depth);
        var abResult = alphaBeta.FindBestMoveWithMetrics(board, depth);

        // ในสถานะเริ่มต้น อาจมีท่าเดินที่ดีเท่ากันหลายท่า ดังนั้นเราจะเช็คคะแนน (Score) แทน
        Assert.AreEqual(mmResult.Score, abResult.Score, 0.1f, "Both algorithms should return the same evaluation score at the same depth.");

        // Alpha-Beta ควรจะประหยัดการคำนวณมากกว่า (ประเมินโหนดน้อยกว่า)
        Assert.LessOrEqual(abResult.NodesEvaluated, mmResult.NodesEvaluated, "Alpha-Beta should evaluate fewer or equal nodes compared to Minimax.");

        Debug.Log($"Minimax Nodes: {mmResult.NodesEvaluated}, Alpha-Beta Nodes: {abResult.NodesEvaluated}");
    }
}
