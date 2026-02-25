using AIEngine.Utilities;

namespace AIEngine.Utilities
{
    public struct SearchResult
    {
        public MoveModel Move;
        public int Depth;
        public int NodesEvaluated;
        public float TimeMs;
        public float Score;
    }
}
