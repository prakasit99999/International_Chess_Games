using System.Collections;
using UnityEngine;
using static ChessPiece;

public class OnlineSyncService : MonoBehaviour
{
    private GameManager gameManager;
    private OnlineSessionManager onlineSessionManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        onlineSessionManager = FindFirstObjectByType<OnlineSessionManager>();

        if (gameManager == null)
            Debug.LogError("❌ GameManager not found in scene.");
    }

    public void OnChessBoardMoveCompleted(MoveResult result)
    {
        if (gameManager == null || gameManager.gameModeManager == null)
            return;

        if (gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.Online)
        {
            if (onlineSessionManager != null && !onlineSessionManager.isApplyingNetworkMove)
            {
                onlineSessionManager.OnLocalPlayerMoved(
                    result.From,
                    result.To,
                    result.PieceType
                );
            }
            return;
        }

        // Offline/Local modes: turn is handled by GameManager.HandleMoveCompleted
    }

    public IEnumerator HandleOnlineResign()
    {
        if (gameManager == null || gameManager.gameModeManager == null)
            yield break;

        if (gameManager.IsGameOver())
        {
            Debug.Log("Resign skipped: game already over.");
            yield break;
        }

        if (gameManager.gameModeManager.CurrentMode != GameModeManager.GameModes.Online)
        {
            Debug.LogWarning("❌ GiveUp called in non-online mode");
            yield break;
        }

        int gameId = gameManager.currentGameId;
        if (gameId <= 0)
            yield break;

        if (gameManager.gameAPI == null)
        {
            Debug.LogWarning("⚠ GameAPI missing.");
            yield break;
        }

        bool isRanked = gameManager.matchMode == 1;
        string reason = isRanked ? "resign_ranked" : "resign_normal";

        yield return gameManager.gameAPI.ResignOnlineGame(
            gameId,
            reason,
            (response) => Debug.Log("✅ Resigned successfully"),
            (error) => Debug.LogError($"❌ Resign failed: {error}")
        );

        gameManager.HandleGameOver(
            gameManager.GetOpponentTeam(gameManager.myLocalTeam),
            "Resign"
        );
    }
}
