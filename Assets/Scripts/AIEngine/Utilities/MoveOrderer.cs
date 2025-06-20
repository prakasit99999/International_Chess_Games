//using AI.Models;
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace AIEngine.Utilities
{   
    public class MoveOrderer
    {
        public static List<MoveModel> OrderMoves(List<MoveModel> moves, ChessBoardModel board, MoveModel? previousBest)
        {
            if (previousBest == null)
            {
                return moves;
            }
            var orderedMoves = new List<MoveModel> { previousBest };
            orderedMoves.AddRange(moves.Where(m => m != previousBest));
            return orderedMoves;
        }
    }
}