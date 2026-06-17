using System;
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
            Debug.LogError(" GameManager not found in scene.");
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
            Debug.LogWarning("âš ï¸ GameAPI missing. Cannot create offline game.");
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
            Debug.LogError($"âŒ CreateOfflineGame failed: {errorMsg}");
            yield break;
        }

        gameManager.SetGameId(response.gameId);
        gameManager.SetMoveCount(0);
        gameManager.SetGameStarted(true);

        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.GameId = response.gameId;
        }

        Debug.Log($"âœ… Offline Game Started. GameId: {response.gameId}");
    }

    private List<MoveCreateDto> PrepareMoveDtos()
    {
        var result = new List<MoveCreateDto>();

        if (history == null)
            history = FindFirstObjectByType<HistoryMove>();

        if (history == null)
        {
            Debug.LogWarning("âš  History component missing.");
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
                string algorithmType = string.IsNullOrEmpty(moveData.algorithmType)
                    ? GetAlgorithmTypeForTeam(moveData.team)
                    : moveData.algorithmType;

                aiData = new AiPerformanceData
                {
                    Depth = moveData.depth,
                    Nodes = moveData.nodes,
                    MoveTimeMs = moveData.moveTimeMs,
                    Score = moveData.score,
                    AlgorithmType = algorithmType
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
            Debug.LogWarning("âš  MovesAPI missing.");
            yield break;
        }

        yield return movesAPI.SendMovesBatch(
            moves,
            (success) =>
            {
                Debug.Log(success
                    ? "ðŸ“¤ Moves Uploaded"
                    : "âŒ Move Upload Failed");
            });
    }

    private IEnumerator UploadPerformance()
    {
        if (aiPerformanceAPI == null)
        {
            Debug.LogWarning(" AiPerformanceAPI missing.");
            yield break;
        }

        if (PerformanceTracker.Instance == null)
        {
            Debug.LogWarning(" PerformanceTracker missing.");
            yield break;
        }

        var perfDataList = PerformanceTracker.Instance.ExportAll() ?? new List<AiPerformanceData>();

        // Safety net: in AI-vs-AI, guarantee both white/black summaries at end-game.
        if (IsAIVsAIMode())
        {
            bool hasWhite = perfDataList.Exists(p =>
                p != null && string.Equals(p.AiColor, "white", StringComparison.OrdinalIgnoreCase));
            bool hasBlack = perfDataList.Exists(p =>
                p != null && string.Equals(p.AiColor, "black", StringComparison.OrdinalIgnoreCase));

            if (!hasWhite)
            {
                var whiteFallback = BuildPerformanceFromHistoryForTeam(Team.White);
                if (whiteFallback != null)
                    perfDataList.Add(whiteFallback);
            }

            if (!hasBlack)
            {
                var blackFallback = BuildPerformanceFromHistoryForTeam(Team.Black);
                if (blackFallback != null)
                    perfDataList.Add(blackFallback);
            }
        }

        Debug.Log($"[GameResultSyncService] UploadPerformance -> ExportAll count: {(perfDataList != null ? perfDataList.Count : 0)}");

        if (perfDataList == null || perfDataList.Count == 0)
        {
            Debug.LogWarning(" AI Performance empty.");
            yield break;
        }

        foreach (var perfData in perfDataList)
        {
            if (perfData == null || !perfData.HasAnyData())
            {
                Debug.LogWarning("[GameResultSyncService] UploadPerformance skipped an empty perfData entry.");
                continue;
            }

            perfData.GameId = gameManager.currentGameId;
            Debug.Log(
                $"[GameResultSyncService] UploadPerformance sending -> " +
                $"Color: {perfData.AiColor}, Level: {perfData.AiLevel}, Algo: {perfData.AlgorithmType}, " +
                $"Moves: {perfData.TotalMoves}, AvgDepth: {perfData.AverageDepth}, " +
                $"AvgNodes: {perfData.AverageNodesEvaluated}, AvgTime: {perfData.AverageMoveTimeMs}"
            );
            yield return aiPerformanceAPI.SendPerformance(perfData);
        }
    }

    private bool IsAIVsAIMode()
    {
        if (gameManager == null)
            return false;

        var modeManager = gameManager.gameModeManager ?? GameModeManager.Instance;
        return modeManager != null &&
               modeManager.CurrentMode == GameModeManager.GameModes.AIVsAI;
    }

    private AiPerformanceData BuildPerformanceFromHistoryForTeam(Team team)
    {
        if (history == null)
            history = FindFirstObjectByType<HistoryMove>();

        if (history == null)
            return null;

        bool teamIsAI =
            (team == Team.White && gameManager.WhitePlayer == GameManager.PlayerType.AI) ||
            (team == Team.Black && gameManager.BlackPlayer == GameManager.PlayerType.AI);
        if (!teamIsAI)
            return null;

        var historyStack = history.GetMoveHistory();
        if (historyStack == null || historyStack.Count == 0)
            return null;

        int totalMoves = 0;
        int sumDepth = 0;
        int sumNodes = 0;
        int sumTimeMs = 0;
        string algorithmType = null;

        foreach (var move in historyStack)
        {
            if (move.team != team)
                continue;

            totalMoves++;
            sumDepth += move.depth;
            sumNodes += move.nodes;
            sumTimeMs += move.moveTimeMs;

            if (string.IsNullOrEmpty(algorithmType) && !string.IsNullOrEmpty(move.algorithmType))
                algorithmType = move.algorithmType;
        }

        if (totalMoves <= 0)
            return null;

        if (string.IsNullOrEmpty(algorithmType))
            algorithmType = GetAlgorithmTypeForTeam(team);

        return new AiPerformanceData
        {
            AiColor = team == Team.White ? "white" : "black",
            AiLevel = NormalizeDifficulty(GetDifficultyForTeam(team)),
            AlgorithmType = algorithmType,
            AverageDepth = (float)Math.Round((float)sumDepth / totalMoves, 2),
            AverageNodesEvaluated = Mathf.RoundToInt((float)sumNodes / totalMoves),
            AverageMoveTimeMs = Mathf.RoundToInt((float)sumTimeMs / totalMoves),
            TotalMoves = totalMoves
        };
    }


    private IEnumerator FinalizeGame(Team winner, string endReason, int totalMoves)
    {
        if (gameAPI == null)
        {
            Debug.LogWarning("GameAPI missing.");
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
                Debug.Log($"Online Game Finalized Successfully. WhiteRating: {response.whiteRating}, BlackRating: {response.blackRating}");
            },
            (error) =>
            {
                Debug.LogError($"Failed to Finalize Online Game: {error}");
            });
        }
        else
        {
            yield return gameAPI.FinalizeOfflineGame(dto, (response) =>
            {
                Debug.Log("Offline Game Finalized Successfully");
            },
            (error) =>
            {
                Debug.LogError($"Failed to Finalize Offline Game: {error}");
            });
        }
    }

    public IEnumerator SyncFullGameResult(Team winner, string endReason)
    {
        if (gameManager == null)
            yield break;

        bool isOnline = gameManager.gameModeManager != null &&
                        gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.Online;

        if (isOnline)
        {
            Team localTeam = gameManager.myLocalTeam;
            if (localTeam == Team.None)
            {
                Debug.LogWarning("Online result sync skipped: local team unknown.");
                yield break;
            }

            if (winner == Team.None)
            {
                if (localTeam != Team.White)
                {
                    Debug.Log("Online result sync skipped: draw handled by White.");
                    yield break;
                }
            }
            else if (localTeam != winner)
            {
                Debug.Log("Online result sync skipped: only winner sends result.");
                yield break;
            }

            // Wait a frame so the last move send can start (GameOver fires before OnMoveCompleted)
            yield return null;

            // Ensure the final local move is sent BEFORE finalizing the online game
            var onlineSession = FindFirstObjectByType<OnlineSessionManager>();
            if (onlineSession != null)
            {
                const float timeoutSeconds = 3f;
                float elapsed = 0f;

                while (onlineSession.IsSendingMove && elapsed < timeoutSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (onlineSession.IsSendingMove)
                    Debug.LogWarning("Timeout waiting for last move send before finalize.");
            }
        }

        if (gameManager.gameModeManager != null &&
            gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.LocalMultiplayer)
        {
            Debug.Log("LocalMultiplayer: skip sync.");
            yield break;
        }

        Debug.Log($"🔄 SyncFullGameResult Started → Winner: {winner}, Reason: {endReason}");

        int gameId = gameManager.currentGameId;
        if (gameId <= 0)
        {
            Debug.LogWarning("No GameId → Skip Sync");
            yield break;
        }

        List<MoveCreateDto> moves = PrepareMoveDtos();

        if (moves.Count > 0)
            yield return UploadMoves(moves);

        if (PerformanceTracker.Instance != null && PerformanceTracker.Instance.ExportAll().Count > 0)
            yield return UploadPerformance();

        bool shouldFinalize = true;
        if (gameAPI != null)
        {
            bool done = false;
            GameStatusDto statusDto = null;
            string errorMsg = null;

            yield return gameAPI.GetGameStatus(
                gameId,
                (status) =>
                {
                    statusDto = status;
                    done = true;
                },
                (error) =>
                {
                    errorMsg = error;
                    done = true;
                });

            yield return new WaitUntil(() => done);

            if (statusDto == null)
            {
                if (!string.IsNullOrEmpty(errorMsg))
                    Debug.LogWarning($"Skip finalize: status check error: {errorMsg}");
                else
                    Debug.LogWarning("Skip finalize: status check returned null.");

                shouldFinalize = false;
            }
            else if (!string.Equals(statusDto.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Skip finalize: game already '{statusDto.Status}'.");
                shouldFinalize = false;
            }
        }

        if (shouldFinalize)
            yield return FinalizeGame(winner, endReason, moves.Count);

        Debug.Log("✅ Full Game Sync Completed");
    }


    public IEnumerator ExitLocalSequence()
    {
        if (gameManager == null)
            yield break;

        if (PauseManager.isPaused)
            PauseManager.Resume();

        bool isGameOver = gameManager.IsGameOver();
        Debug.Log(isGameOver
            ? "🟢 Local Exit → Game already over, skipping abandon finalize"
            : "🟢 Local Exit → Finalizing Game");
        yield return FinalizeAbandonedIfNeeded("exit");

        gameManager.ResetGame();

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public IEnumerator ExitOnlineSequence()
    {
        if (gameManager == null)
            yield break;

        if (PauseManager.isPaused)
            PauseManager.Resume();

        Debug.Log(" Online Exit â†’ Finalizing Game");

    }


    public IEnumerator ReplayOfflineGame()
    {
        if (gameManager == null)
            yield break;

        if (gameManager.gameModeManager != null &&
            gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.Online)
        {
            Debug.LogWarning(" Replay is offline-only.");
            yield break;
        }

        if (gameManager.gameModeManager != null &&
            gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.LocalMultiplayer)
        {
            gameManager.ResetGame();
            gameManager.SetGameId(-1);
            gameManager.SetMoveCount(0);
            gameManager.ApplyOfflineModeSettings();
            Debug.Log(" Replay started (LocalMultiplayer, no API).");
            yield break;
        }

        yield return FinalizeAbandonedIfNeeded("replay");

        if (gameAPI == null)
        {
            Debug.LogWarning(" GameAPI missing.");
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
            Debug.LogError($"Failed to CreateOfflineGame: {errorMsg}");
            yield break;
        }

        gameManager.ResetGame();
        gameManager.SetGameId(response.gameId);
        gameManager.SetMoveCount(0);
        gameManager.ApplyOfflineModeSettings();

        if (PerformanceTracker.Instance != null)
        {
            PerformanceTracker.Instance.ResetData();
            PerformanceTracker.Instance.GameId = response.gameId;
        }

        Debug.Log($" Replay started. New GameId: {response.gameId}");
    }

    private IEnumerator FinalizeAbandonedIfNeeded(string source)
    {
        if (gameManager == null)
            yield break;

        bool isGameOver = gameManager.IsGameOver();
        int gameId = gameManager.currentGameId;

        bool isLocalMultiplayer = gameManager.gameModeManager != null &&
                                  gameManager.gameModeManager.CurrentMode == GameModeManager.GameModes.LocalMultiplayer;

        if (isGameOver || isLocalMultiplayer || gameId <= 0 || gameAPI == null)
            yield break;

        bool shouldFinalize = true;
        bool done = false;
        GameStatusDto statusDto = null;
        string errorMsg = null;

        yield return gameAPI.GetGameStatus(
            gameId,
            (status) =>
            {
                statusDto = status;
                done = true;
            },
            (error) =>
            {
                errorMsg = error;
                done = true;
            });

        yield return new WaitUntil(() => done);

        if (statusDto == null)
        {
            if (!string.IsNullOrEmpty(errorMsg))
                Debug.LogWarning($"Skip {source}-finalize: status check error: {errorMsg}");
            else
                Debug.LogWarning($"Skip {source}-finalize: status check returned null.");

            shouldFinalize = false;
        }
        else if (!string.Equals(statusDto.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"Skip {source}-finalize: game already '{statusDto.Status}'.");
            shouldFinalize = false;
        }

        if (!shouldFinalize)
            yield break;

        GameResultDto dto = new GameResultDto
        {
            GameId = gameId,
            Result = "draw",
            ResultReason = "abandoned",
            MoveCount = gameManager.moveCount
        };

        yield return gameAPI.FinalizeOfflineGame(dto, null, null);
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

    private string GetAlgorithmTypeForTeam(Team team)
    {
        string difficulty = NormalizeDifficulty(GetDifficultyForTeam(team));
        return difficulty == "easy" ? "minimax" : "alpha_beta";
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
        {
            Debug.LogWarning("[GameResultSyncService] RecordMove skipped because gameManager is missing.");
            return;
        }

        Debug.Log(
            $"[GameResultSyncService] RecordMove start -> Team: {moveData.team}, " +
            $"Move: {moveData.startPosition} -> {moveData.endPosition}, " +
            $"HasAIStats: {aiStats != null}, GameId: {gameManager.currentGameId}, CurrentMoveCount: {gameManager.moveCount}"
        );

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
            Debug.Log($"[GameResultSyncService] RecordMove stored dto -> MoveNumber: {gameManager.moveCount}, RecordedMoves: {recordedMoves.Count}");
        }
        else
        {
            Debug.LogWarning("[GameResultSyncService] RecordMove did not create dto because currentGameId <= 0.");
        }

        if (PerformanceTracker.Instance != null)
        {
            bool isWhiteAI = gameManager.WhitePlayer == GameManager.PlayerType.AI;
            bool isBlackAI = gameManager.BlackPlayer == GameManager.PlayerType.AI;
            bool isMoveByAI =
                (moveData.team == Team.White && isWhiteAI) ||
                (moveData.team == Team.Black && isBlackAI);

            AiPerformanceData statsToTrack = aiStats;
            if (statsToTrack == null && isMoveByAI)
            {
                string normalizedDifficulty = NormalizeDifficulty(GetDifficultyForTeam(moveData.team));
                statsToTrack = new AiPerformanceData
                {
                    AiColor = moveData.team == Team.White ? "white" : "black",
                    AiLevel = normalizedDifficulty,
                    AlgorithmType = GetAlgorithmTypeForTeam(moveData.team),
                    Depth = moveData.depth,
                    Nodes = moveData.nodes,
                    MoveTimeMs = moveData.moveTimeMs,
                    Score = moveData.score
                };

                Debug.Log(
                    $"[GameResultSyncService] RecordMove created fallback AI stats -> Team: {moveData.team}, " +
                    $"Color: {statsToTrack.AiColor}, Level: {statsToTrack.AiLevel}, Algo: {statsToTrack.AlgorithmType}, " +
                    $"Depth: {statsToTrack.Depth}, Nodes: {statsToTrack.Nodes}, Time: {statsToTrack.MoveTimeMs}, Score: {statsToTrack.Score}"
                );
            }

            if (statsToTrack != null)
            {
                if (string.IsNullOrEmpty(statsToTrack.AiColor))
                    statsToTrack.AiColor = moveData.team == Team.White ? "white" : "black";

                if (string.IsNullOrEmpty(statsToTrack.AiLevel))
                    statsToTrack.AiLevel = NormalizeDifficulty(GetDifficultyForTeam(moveData.team));

                if (string.IsNullOrEmpty(statsToTrack.AlgorithmType))
                    statsToTrack.AlgorithmType = GetAlgorithmTypeForTeam(moveData.team);
            }

            if (statsToTrack != null)
            {
                Debug.Log(
                    $"[GameResultSyncService] RecordMove forwarding AI stats -> " +
                    $"Color: {statsToTrack.AiColor}, Level: {statsToTrack.AiLevel}, Algo: {statsToTrack.AlgorithmType}, " +
                    $"Depth: {statsToTrack.Depth}, Nodes: {statsToTrack.Nodes}, Time: {statsToTrack.MoveTimeMs}, Score: {statsToTrack.Score}"
                );

                PerformanceTracker.Instance.AddMove(
                    statsToTrack.AiColor,
                    statsToTrack.AiLevel,
                    statsToTrack.AlgorithmType,
                    statsToTrack.Depth,
                    statsToTrack.Nodes,
                    statsToTrack.MoveTimeMs,
                    statsToTrack.Score
                );

                Debug.Log("[GameResultSyncService] RecordMove forwarded AI stats to PerformanceTracker.");
                return;
            }

            Debug.Log(
                $"[GameResultSyncService] RecordMove no AI stats forwarded -> " +
                $"HasAIStats: {aiStats != null}, IsMoveByAI: {isMoveByAI}"
            );
            return;
        }

        Debug.Log(
            $"[GameResultSyncService] RecordMove no AI stats forwarded -> " +
            $"HasAIStats: {aiStats != null}, HasTracker: {PerformanceTracker.Instance != null}"
        );
    }
}
