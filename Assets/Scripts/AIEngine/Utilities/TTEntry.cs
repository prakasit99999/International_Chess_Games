using UnityEngine;
namespace AIEngine.Utilities
{

    public class TTEntry 
    {
        public enum TTFlag { Exact, LowerBound, UpperBound }
        public int Depth { get; set; }
        public int Score { get; set; }
        public TTFlag Flag { get; set; }
        public MoveModel BestMove { get; set; }
        public TTEntry(int depth, int score, TTFlag flag, MoveModel bestMove)
        {
            Depth = depth;
            Score = score;
            Flag = flag;
            BestMove = bestMove;
        }
    }
}