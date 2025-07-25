using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static AIEngine.Core.AICore;
namespace Game.Interfaces
{
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public interface IChessAI
    {
        
        Task<Vector2Int[]> CalculateMoveAsync(ChessBoard board, ChessPiece.Team team, AIDifficulty difficulty);
    }
}