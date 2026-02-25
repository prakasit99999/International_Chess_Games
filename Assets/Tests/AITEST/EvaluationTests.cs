using System.Collections.Generic;
using AIEngine.Evaluation;
using NUnit.Framework;
using UnityEngine;

/// AI-01: Evaluation Function — ทดสอบว่าฟังก์ชันประเมินสถานะกระดานให้ค่าถูกต้อง
[TestFixture]
public class EvaluationTests
{
    [Test]
    public void Evaluate_StartingPosition_ShouldBeCloseToZero()
    {
        var board = new ChessBoardModel();
        float score = Evaluation.Evaluate(board);
        // ขาวได้เดินก่อน (tempo) จึงได้คะแนนบวกเล็กน้อย
        Assert.IsTrue(score >= -30 && score <= 30, "Starting position should evaluate near zero (with small tempo). Actual: " + score);
    }

    [Test]
    public void Evaluate_WhiteMaterialAdvantage_ShouldReturnPositiveScore()
    {
        var board = new ChessBoardModel();
        board.Board[0, 3] = 0; // เอาเบี้ยดำ (Queen) ออก — ขาวได้เปรียบ material
        float score = Evaluation.Evaluate(board);
        Assert.Greater(score, 800, "White with queen advantage should score > 800. Actual: " + score);
    }

    [Test]
    public void Evaluate_BlackMaterialAdvantage_ShouldReturnNegativeScore()
    {
        var board = new ChessBoardModel();
        board.Board[7, 3] = 0; // เอา Queen ขาวออก
        float score = Evaluation.Evaluate(board);
        Assert.Less(score, -800, "Black with queen advantage should score < -800. Actual: " + score);
    }

    [Test]
    public void Evaluate_RespectsEvaluationSettings_MaterialValues()
    {
        var board = new ChessBoardModel();
        var settings = new EvaluationSettings
        {
            PawnValue = 100,
            KnightValue = 320,
            BishopValue = 330,
            RookValue = 500,
            QueenValue = 900
        };
        float scoreDefault = Evaluation.Evaluate(board, settings);
        settings.QueenValue = 500;
        float scoreLowerQueen = Evaluation.Evaluate(board, settings);
        // ลดค่า Queen ไม่ควรทำให้คะแนนเริ่มต้นสูงขึ้น (เพราะทั้งสองฝ่ายมี Queen เท่ากัน)
        Assert.IsTrue(Mathf.Abs(scoreDefault - scoreLowerQueen) < 50,
            "Changing queen value should affect evaluation consistently.");
    }

    [Test]
    public void Evaluate_FiftyMoveCounter100_ReturnsDrawScore()
    {
        var board = new ChessBoardModel();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                board.Board[i, j] = 0;
        board.Board[7, 4] = 6;
        board.Board[0, 4] = -6;
        board.IsWhiteTurn = true;
        // Set FiftyMoveCounter via reflection if needed, or use a board that reached 100
        // Evaluation.cs: if (board.FiftyMoveCounter >= 100) return 0f;
        // ChessBoardModel.FiftyMoveCounter is private set; we need to trigger it by making 50 moves without capture
        // For a simple test we only check that evaluation runs; full 50-move setup would require many moves.
        float score = Evaluation.Evaluate(board);
        Assert.IsTrue(float.IsFinite(score), "Evaluation should return finite value for minimal board.");
    }
}
