using System.Collections;
using System.Collections.Generic;
using AIEngine.Evaluation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class EvaluationTests
{
    [Test]
    public void Evaluate_StartingPosition_ShouldBeCloseToZero()
    {
        // Arrange
        var board = new ChessBoardModel(); // ตำแหน่งเริ่มต้น
        //board.LoadPositionFromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // Act
        int score = (int)Evaluation.Evaluate(board);

        // Assert
        // คะแนนเริ่มต้นควรจะใกล้เคียง 0 (อาจมีค่าบวกเล็กน้อยที่ขาวได้เดินก่อน)
        Assert.IsTrue(score >= 0 && score < 20);
    }
    [Test]
    public void Evaluate_MaterialAdvantage_ShouldReflectCorrectScore()
    {
        // Arrange
        var board = new ChessBoardModel();
        // สถานการณ์: ขาวมีควีน แต่ดำไม่มี (นอกนั้นเหมือนกัน)
        //board.LoadPositionFromFen("rnb1kbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        // Act
        int score = (int)Evaluation.Evaluate(board);

        // Assert
        // คะแนนควรเป็นบวก และมากกว่า 900 (ค่าของควีน)
        Assert.Greater(score, 900);

    }
}

