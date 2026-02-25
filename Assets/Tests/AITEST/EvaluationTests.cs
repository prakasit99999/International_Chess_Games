using System.Collections.Generic;
using AIEngine.Evaluation;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class EvaluationTests
{
    private ChessBoardModel GetEmptyBoard()
    {
        var board = new ChessBoardModel();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                board.Board[i, j] = 0;
        return board;
    }

    [Test]
    public void Test_EmptyBoard_ReturnsZero()
    {
        var board = GetEmptyBoard();
        // เพิ่ม Kings เพื่อให้บอร์ดสมบูรณ์ (ถ้าไม่มี King ระบบอาจคำนวณผิดพลาดได้ในบาง Engine)
        board.Board[7, 4] = 6;  // White King
        board.Board[0, 4] = -6; // Black King

        float score = Evaluation.Evaluate(board);
        // เนื่องจากมีค่า Tempo (Side to move) คะแนนอาจจะไม่ใช่ 0 เป๊ะๆ แต่ควรจะใกล้เคียงมาก (เช่น 10 หรือ -10)
        Assert.IsTrue(Mathf.Abs(score) <= 15, "Empty board with only kings should be near zero. Actual: " + score);
    }

    [Test]
    public void Test_WhiteHasExtraQueen_ReturnsPositive()
    {
        var board = GetEmptyBoard();
        board.Board[7, 4] = 6;  // White King
        board.Board[0, 4] = -6; // Black King
        board.Board[4, 4] = 5;  // White Queen
        board.IsWhiteTurn = true;

        float score = Evaluation.Evaluate(board);
        Assert.Greater(score, 800, "White having an extra queen should result in a high positive score.");
    }

    [Test]
    public void Test_BlackHasExtraRook_ReturnsNegative()
    {
        var board = GetEmptyBoard();
        board.Board[7, 4] = 6;   // White King
        board.Board[0, 4] = -6;  // Black King
        board.Board[4, 4] = -4;  // Black Rook
        board.IsWhiteTurn = true; // Turn is white, but black is ahead

        float score = Evaluation.Evaluate(board);
        // Perspective: IsWhiteTurn = true, score should be negative because Black is better
        Assert.Less(score, -400, "Black having an extra rook should result in a significant negative score for White.");
    }

    [Test]
    public void Test_PassedPawn_GivesBonus()
    {
        var board = GetEmptyBoard();
        board.Board[7, 4] = 6;  // White King
        board.Board[0, 4] = -6; // Black King

        // สถานะ 1: เบี้ยขาวปกติ
        board.Board[6, 0] = 1;
        float normalScore = Evaluation.Evaluate(board);

        // สถานะ 2: เบี้ยขาวที่เป็น Passed Pawn (ไม่มีเบี้ยดำขวาง)
        // (ใน GetEmptyBoard ไม่มีเบี้ยดำอยู่แล้ว ดังนั้นเบี้ยที่ตำแหน่งใดๆ ก็เป็น passed pawn ถ้าไม่มีเบี้ยดำในไฟล์ข้างๆ)
        // ลองขยับไปแถวที่สูงขึ้นเพื่อให้ได้ Rank Bonus
        board.Board[6, 0] = 0;
        board.Board[2, 0] = 1;
        float passedScore = Evaluation.Evaluate(board);

        Assert.Greater(passedScore, normalScore, "A passed pawn further up the board should receive a higher evaluation bonus.");
    }

    [Test]
    public void Test_IsolatedPawn_AppliesPenalty()
    {
        var board = GetEmptyBoard();
        board.Board[7, 4] = 6;
        board.Board[0, 4] = -6;

        // เพิ่มเบี้ยขาว 2 ตัวที่อยู่ติดกัน (ไม่โดดเดี่ยว)
        board.Board[6, 3] = 1;
        board.Board[6, 4] = 1;
        float supportedScore = Evaluation.Evaluate(board);

        // เพิ่มเบี้ยขาวตัวเดียว (โดดเดี่ยวในไฟล์)
        board.Board[6, 4] = 0;
        float isolatedScore = Evaluation.Evaluate(board);

        // หมายเหตุ: การเปรียบเทียบนี้อาจขึ้นอยู่กับค่า PST ด้วย
        // แต่โดยหลักการ Isolated Pawn Penalty ควรทำให้คะแนนลดลงเมื่อเทียบกับ Material เท่ากันที่มีโครงสร้างดีกว่า
        // ในที่นี้เราเทียบ เบี้ย 2 ตัว vs เบี้ย 1 ตัวไม่ได้ ต้องเทียบ 1 vs 1

        var board1 = GetEmptyBoard();
        board1.Board[7, 4] = 6; board1.Board[0, 4] = -6;
        board1.Board[6, 3] = 1; board1.Board[6, 2] = 1; // Not isolated

        var board2 = GetEmptyBoard();
        board2.Board[7, 4] = 6; board2.Board[0, 4] = -6;
        board2.Board[6, 3] = 1; board2.Board[6, 1] = 1; // Isolated (file 3 has no pawn in file 2 or 4)

        float score1 = Evaluation.Evaluate(board1);
        float score2 = Evaluation.Evaluate(board2);

        Assert.Greater(score1, score2, "Pawn structure with isolated pawns should evaluate lower than connected pawns.");
    }

    [Test]
    public void Test_FiftyMoveRule_ReturnsZero()
    {
        var board = new ChessBoardModel();
        // ตั้งค่าตัวหมากให้ขาวได้เปรียบสุดๆ
        board.Board[0, 3] = 0; // Black Queen gone

        // ตรวจสอบว่าก่อนเริ่มกฎ 50 ตา คะแนนเป็นบวก
        Assert.Greater(Evaluation.Evaluate(board), 500);

        // ตั้งค่า FiftyMoveCounter เป็น 100 (50 moves)
        board.SetFiftyMoveCounter(100);

        float score = Evaluation.Evaluate(board);
        Assert.AreEqual(0f, score, "Evaluation must return 0 when the 50-move rule is triggered, regardless of material.");
    }
}
