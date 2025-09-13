using AI.Utilities;
using AIEngine.Core;
using Game.Interfaces;
using System.Threading.Tasks;
using UnityEngine;
using static ChessPiece;

namespace AI.Adapters
{
    public class UnityAIBoardAdapter : IChessAI
    {
        private Vector2Int[] calculatedMove;
        private bool isThinking;

        public void StartCalculateMove(ChessBoard board, Team team, AIDifficulty difficulty)
        {
            calculatedMove = null;
            isThinking = true;

            // 🔹 แปลง Unity ChessBoard → ChessBoardModel (ฝั่ง AI)
            var boardModel = BoardConverter.Convert(board, team);

            // 🔹 เรียก AICore หา best move
            var moveModel = AICore.Instance.FindBestMove(
                boardModel,
                ConvertDifficulty(difficulty)
            );

            if (moveModel != null)
            {
                // แปลงพิกัด จาก array (0 = บนสุด) → Unity (0 = ล่างสุด)
                int unityFromX = moveModel.FromX;
                int unityFromY = moveModel.FromY;

                int unityToX = moveModel.ToX;
                int unityToY = moveModel.ToY;

                //Debug.Log($"[AI MOVE MODEL] From=({moveModel.FromX},{moveModel.FromY}) To=({moveModel.ToX},{moveModel.ToY})");
                //Debug.Log($"[UNITY MOVE] From=({unityFromX},{unityFromY}) To=({unityToX},{unityToY})");
                if (moveModel != null)
                {
                    Debug.Log($"[AI MOVE MODEL] From=({moveModel.FromX},{moveModel.FromY}) To=({moveModel.ToX},{moveModel.ToY})");
                    calculatedMove = new Vector2Int[]
                    {
                    BoardConverter.ConvertPositionToUnity(new Vector2Int(moveModel.FromX, moveModel.FromY)),
                    BoardConverter.ConvertPositionToUnity(new Vector2Int(moveModel.ToX, moveModel.ToY))
                    };
                    Debug.Log($"[UNITY MOVE] From={calculatedMove[0]} To={calculatedMove[1]}");
                }
            }
            isThinking = false;
        }

        public Vector2Int[] GetCalculatedMove()
        {
            if (!isThinking && calculatedMove != null)
            {
                var move = calculatedMove;
                calculatedMove = null;
                return move;
            }
            return null;
        }

        private AICore.Difficulty ConvertDifficulty(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy: return AICore.Difficulty.Easy;
                case AIDifficulty.Normal: return AICore.Difficulty.Normal;
                case AIDifficulty.Hard: return AICore.Difficulty.Hard;
                default: return AICore.Difficulty.Easy;
            }
        }
    }
}


