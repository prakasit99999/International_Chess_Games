using System.Collections.Generic;
using UnityEngine;
namespace AIEngine.Utilities
{
    public class TranspositionTable 
    {
        private readonly Dictionary<ulong, TTEntry> _table = new Dictionary<ulong, TTEntry>();

        public void Clear()=> _table.Clear();

        public bool TryGet(ulong key, out TTEntry entry) => _table.TryGetValue(key, out entry);

        public void Store(ulong key, int depth, int score, TTEntry.TTFlag flag, MoveModel bestMove)
        {
            // replacement: keep deeper info or replace
            if( _table.TryGetValue(key, out TTEntry existingEntry))
            {
                if(existingEntry.Depth <= depth)
                {
                    existingEntry.Depth = depth;
                    existingEntry.Score = score;
                    existingEntry.Flag = flag;
                    existingEntry.BestMove = bestMove;
                    _table[key] = existingEntry;
                }

            }
            else
            {
                _table[key] = new TTEntry(depth, score, flag, bestMove);
            }
        }
    }
}

