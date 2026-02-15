using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChessPiece;

public class GameResultSyncService : MonoBehaviour
{
    private GameManager gameManager;
    private HistoryMove history;
    private GameAPI gameAPI;
    private MovesAPI movesAPI;
    private AiPerformanceAPI aiPerformanceAPI;
    private List<MoveCreateDto> recordedMoves = new List<MoveCreateDto>();

    private void Awake()
    {
        history = FindFirstObjectByType<HistoryMove>();
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("❌ GameManager not found in scene.");
            return;
        }

        gameAPI = gameManager.gameAPI;
        movesAPI = gameManager.movesApi;
        aiPerformanceAPI = gameManager.aiPerformanceApi;
    }

    private void Start()
    {
        StartCoroutine(StartOfflineGameIfNeeded());
    }

    private IEnumerator StartOfflineGameIfNeeded()
    {
        if (gameManager == null)
            yield break;

        // wait a frame to allow GameManager/GameModeManager to load prefs
        yield return null;

        var modeManager = gameManager.gameModeManager ?? GameModeManager.Instance;
        var mode = modeManager != null ? modeManager.CurrentMode : GameModeManager.GameModes.SinglePlayer;

        if (mode == GameModeManager.GameModes.Online ||
            mode == GameModeManager.GameModes.LocalMultiplayer)
            yield break;

        if (gameManager.currentGameId > 0)
            yield break;

        if (gameAPI == null)
        {
            Debug.LogWarning("⚠️ GameAPI missing. Cannot create offline game.");
            yield break;
        }

        GameCreateDto dto = BuildOfflineCreateDto();

        bool done = false;
        GameStartResponse response = null;
        string errorMsg = null;

        yield return gameAPI.CreateOfflineGame(dto,
            (res) =>
            {
                response = res;
                done = true;
            },
            (err) =>
            {
                errorMsg = err;
                done = true;
            });

        yield return new WaitUntil(() => done);

        if (response == null || response.gameId <= 0)
        {
            Debug.LogError($"❌ CreateOfflineGame failed: {errorMsg}");
            yield break;
        }

        gameManager.SetGameId(response.gameId);
        gameManager.SetMoveCount(0);
        gameManager.SetGameStarted(true);

        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.GameId = response.gameId;
        }

        Debug.Log($"✅ Offline Game Started. GameId: {response.gameId}");
    }

    private List<MoveCreateDto> PrepareMoveDtos()
    {
        var result = new List<MoveCreateDto>();

        if (history == null)
            history = FindFirstObjectByType<HistoryMove>();

        if (history == null)
        {
            Debug.LogWarning("⚠ History component missing.");
            return result;
        }

        var historyStack = history.GetMoveHistory();
        var historyList = new List<HistoryMove.HistoryMoveData>(historyStack);
        historyList.Reverse();

        int moveNumber = 1;

        foreach (var moveData in historyList)
        {
            AiPerformanceData aiData = null;

            if (moveData.depth > 0)
            {
                aiData = new AiPerformanceData
                {
                    Depth = moveData.depth,
                    Nodes = moveData.nodes,
                    MoveTimeMs = moveData.moveTimeMs,
                    Score = moveData.score,
                    AlgorithmType = "minimax"
                };
            }

            var dto = MoveMapper.ToDto(
                moveData,
                gameManager.currentGameId,
                moveNumber,
                aiData
            );

            result.Add(dto);
            moveNumber++;
        }

        return result;
    }

    private IEnumerator UploadMoves(List<MoveCreateDto> moves)
    {
        if (movesAPI == null)
        {
            Debug.LogWarning("⚠ MovesAPI missing.");
            yield break;
        }

        yield return movesAPI.SendMovesBatch(
            moves,
            (success) =>
            {
                Debug.Log(success
                    ? "📤 Moves Uploaded"
                    : "❌ Move Upload Failed");
            });
    }

    private IEnumerator UploadPerformance()
    {
        if (aiPerformanceAPI == null)
        {
            Debug.LogWarning("⚠ AiPerformanceAPI missing.");
            yield break;
        }

        if (PerformanceTracker.Instance == null)
        {
            Debug.LogWarning("⚠ PerformanceTracker missing.");
            yield break;
        }

        var perfData = PerformanceTracker.Instance.Export();

        if (perfData == null || !perfData.HasAnyData())
        {
            Debug.LogWarning("⚠ AI Performance empty.");
            yield break;
        }

        perfData.GameId = gameManager.currentGameId;

        yield return aiPerformanceAPI.SendPerformance(perfData);
    }

    private IEnumerator FinalizeGame(Team winner, string endReason, int totalMoves)
    {
        if (gameAPI == null)
        {
            Debug.LogWarning("⚠ GameAPI missing.");
            yield break;
        }

        string resultStr = ConvertResultString(winner);

        GameResultDto dto = new GameResultDto
        {
            GameId = gameManager.currentGameId,
            Result = resultStr,
            ResultReason = endReason,
            MoveCount = totalMoves
        };

        bool isOnline = gameManager.gameModeManager != null &&
                        gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.Online;

        if (isOnline)
        {
            yield return gameAPI.FinalizeOnlineGame(dto, (response) =>
            {
                Debug.Log($"🏁 Online Game Finalized Successfully. WhiteRating: {response.whiteRating}, BlackRating: {response.blackRating}");
            },
            (error) =>
            {
                Debug.LogError($"❌ Failed to Finalize Online Game: {error}");
            });
        }
        else
        {
            yield return gameAPI.FinalizeOfflineGame(dto, (response) =>
            {
                Debug.Log("🏁 Offline Game Finalized Successfully");
            },
            (error) =>
            {
                Debug.LogError($"❌ Failed to Finalize Offline Game: {error}");
            });
        }
    }

    public IEnumerator SyncFullGameResult(Team winner, string endReason)
    {
        if (gameManager == null)
            yield break;

        if (gameManager.gameModeManager != null &&
            gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.LocalMultiplayer)
        {
            Debug.Log("⚠ LocalMultiplayer: skip sync.");
            yield break;
        }

        Debug.Log($"🔄 SyncFullGameResult Started → Winner: {winner}, Reason: {endReason}");

        int gameId = gameManager.currentGameId;

        if (gameId <= 0)
        {
            Debug.LogWarning("⚠ No GameId → Skip Sync");
            yield break;
        }

        List<MoveCreateDto> moves = PrepareMoveDtos();

        if (moves.Count > 0)
            yield return UploadMoves(moves);

        if (PerformanceTracker.Instance != null && PerformanceTracker.Instance.Export().HasAnyData())
            yield return UploadPerformance();

        yield return FinalizeGame(winner, endReason, moves.Count);

        Debug.Log("✅ Full Game Sync Completed");
    }

    public IEnumerator ExitLocalSequence()
    {
        if (gameManager == null)
            yield break;

        if (PauseManager.isPaused)
            PauseManager.Resume();

        Debug.Log("🟢 Local Exit → Finalizing Game");

        int gameId = gameManager.currentGameId;

        bool isLocalMultiplayer = gameManager.gameModeManager != null &&
                                  gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.LocalMultiplayer;

        if (!isLocalMultiplayer && gameId > 0 && gameAPI != null)
        {
            GameResultDto dto = new GameResultDto
            {
                GameId = gameId,
                Result = "draw",
                ResultReason = "abandoned",
                MoveCount = gameManager.moveCount
            };

            yield return gameAPI.FinalizeOfflineGame(dto, null, null);
        }

        gameManager.ResetGame();

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public IEnumerator ReplayOfflineGame()
    {
        if (gameManager == null)
            yield break;

        if (gameManager.gameModeManager != null &&
            gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.Online)
        {
            Debug.LogWarning("❌ Replay is offline-only.");
            yield break;
        }

        if (gameManager.gameModeManager != null &&
            gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.LocalMultiplayer)
        {
            gameManager.ResetGame();
            gameManager.SetGameId(-1);
            gameManager.SetMoveCount(0);
            gameManager.SetGameStarted(true);
            gameManager.SetCurrentTurn(Team.White);
            Debug.Log("✅ Replay started (LocalMultiplayer, no API).");
            yield break;
        }

        if (gameAPI == null)
        {
            Debug.LogWarning("⚠ GameAPI missing.");
            yield break;
        }

        GameCreateDto dto = BuildOfflineCreateDto();

        bool done = false;
        GameStartResponse response = null;
        string errorMsg = null;

        yield return gameAPI.CreateOfflineGame(dto,
            (res) =>
            {
                response = res;
                done = true;
            },
            (err) =>
            {
                errorMsg = err;
                done = true;
            });

        yield return new WaitUntil(() => done);

        if (response == null || response.gameId <= 0)
        {
            Debug.LogError($"❌ CreateOfflineGame failed: {errorMsg}");
            yield break;
        }

        gameManager.ResetGame();
        gameManager.SetGameId(response.gameId);
        gameManager.SetMoveCount(0);
        gameManager.SetGameStarted(true);
        gameManager.SetCurrentTurn(Team.White);

        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.ResetData();
            PerformanceTracker.Instance.GameId = response.gameId;
        }

        Debug.Log($"✅ Replay started. New GameId: {response.gameId}");
    }

    private GameCreateDto BuildOfflineCreateDto()
    {
        var mode = gameManager.gameModeManager != null
            ? gameManager.gameModeManager.CurrentMode
            : GameModeManager.GameModes.SinglePlayer;

        string gameType = mode switch
        {
            GameModeManager.GameModes.AIVsAI => "ai_vs_ai",
            GameModeManager.GameModes.LocalMultiplayer => "local_multiplayer",
            _ => "single_player"
        };

        return new GameCreateDto
        {
            GameType = gameType,
            MatchMode = 0,
            WhitePlayerType = MapPlayerType(gameManager.WhitePlayer, GetDifficultyForTeam(Team.White)),
            BlackPlayerType = MapPlayerType(gameManager.BlackPlayer, GetDifficultyForTeam(Team.Black)),
            WhitePlayerId = null,
            BlackPlayerId = null
        };
    }

    private string MapPlayerType(GameManager.PlayerType type, string difficulty)
    {
        if (type == GameManager.PlayerType.Human)
            return "human";

        string diff = NormalizeDifficulty(difficulty);

        return diff switch
        {
            "hard" => "ai_hard",
            "medium" => "ai_medium",
            _ => "ai_easy"
        };
    }

    private string GetDifficultyForTeam(Team team)
    {
        var modeManager = gameManager.gameModeManager ?? GameModeManager.Instance;
        if (modeManager == null)
            return "easy";

        if (modeManager.CurrentMode == GameModeManager.GameModes.AIVsAI)
        {
            return team == Team.White
                ? modeManager.SelectedWhiteDifficulty
                : modeManager.SelectedBlackDifficulty;
        }

        return modeManager.SelectedDifficulty;
    }

    private string NormalizeDifficulty(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "easy";

        string v = value.Trim().ToLowerInvariant();
        if (v == "normal") v = "medium";
        return v;
    }

    private string ConvertResultString(Team winner)
    {
        switch (winner)
        {
            case Team.White:
                return "white_wins";
            case Team.Black:
                return "black_wins";
            default:
                return "draw";
        }
    }

    public void RecordMove(HistoryMove.HistoryMoveData moveData, AiPerformanceData aiStats = null)
    {
        if (gameManager == null)
            return;

        gameManager.moveCount++;

        if (gameManager.currentGameId > 0)
        {
            var dto = MoveMapper.ToDto(
                moveData,
                gameManager.currentGameId,
                gameManager.moveCount,
                aiStats
            );

            recordedMoves.Add(dto);
        }

        if (aiStats != null && PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.AddMove(
                aiStats.Depth,
                aiStats.Nodes,
                aiStats.MoveTimeMs,
                aiStats.Score
            );
        }
    }
}
