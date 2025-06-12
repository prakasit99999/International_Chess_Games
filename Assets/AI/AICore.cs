//using System;
//using System.Collections.Generic;
//using System.Linq;


//public class AICore
//{
//    public enum Difficulty { Easy, Medium, Hard }

//    public Move FindBestMove(ChessBoard board, Difficulty difficulty)
//    {
//        SearchAlgorithm algorithm;
//        int depth;

//        switch (difficulty)
//        {
//            case Difficulty.Easy:
//                algorithm = new Minimax(); // Minimax ธรรมดา
//                depth = 2; // 1-2 ตาล่วงหน้า
//                break;
//            case Difficulty.Medium:
//                algorithm = new AlphaBeta(); // Corrected class name
//                depth = 4; // 3-4 ตาล่วงหน้า
//                break;
//            case Difficulty.Hard:
//                algorithm = new AlphaBeta(); // Corrected class name
//                depth = 5;
//                break;
//            default:
//                throw new ArgumentException("Invalid difficulty level");
//        }

//        // ตรวจสอบสถานะเสมอและเลือกการเดินที่ได้เปรียบ
//        return OptimizeMoveForDraw(board, algorithm.FindBestMove(board, depth));
//    }

//    private Move OptimizeMoveForDraw(ChessBoard board, Move bestMove)
//    {
//        // หลีกเลี่ยงการเสมอด้วยการเลือกการเดินที่กินหมาก (ถ้ามี)
//        if (board.FiftyMoveCounter > 90)
//        {
//            var captureMoves = MoveGenerator.GenerateMoves(board)
//                .Where(m => board.Board[m.ToX, m.ToY] != 0)
//                .ToList();

//            if (captureMoves.Count > 0)
//                return captureMoves.OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY])).First();
//        }
//        return bestMove;
//    }
//}
