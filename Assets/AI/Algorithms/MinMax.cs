//using System;
//using System.Collections.Generic;

//public class Minimax : SearchAlgorithm
//{


//    public override Move FindBestMove(ChessBoard board, int depth)
//    {
//        List<Move> moves = MoveGenerator.GenerateMoves(board);
//        Move bestMove = moves[0];
//        int bestScore = int.MinValue;

//        foreach (Move move in moves)
//        {
//            ChessBoard newBoard = board.Clone();
//            newBoard.MakeMove(move);
//            // คำนวณคะแนนจากการค้นหาแบบ Recursive
//            int score = MinimaxRecursive(newBoard, depth - 1, false);
//            if (score > bestScore)
//            {
//                bestScore = score;
//                bestMove = move;
//            }
//        }

//        return bestMove;
//    }

//    protected int MinimaxRecursive(ChessBoard board, int depth, bool isMaximizing)
//    {
//        if (depth == 0 || board.IsGameOver())
//            return Evaluation.Evaluate(board);

//        List<Move> moves = MoveGenerator.GenerateMoves(board);
//        int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

//        foreach (Move move in moves)
//        {
//            ChessBoard newBoard = board.Clone();
//            newBoard.MakeMove(move);

//            int score = MinimaxRecursive(newBoard, depth - 1, !isMaximizing);
//            bestScore = isMaximizing 
//                ? Math.Max(bestScore, score) 
//                : Math.Min(bestScore, score);
//        }

//        return bestScore;
//    }
//}