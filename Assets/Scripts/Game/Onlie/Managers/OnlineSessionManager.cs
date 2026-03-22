using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ChessPiece;

public class OnlineSessionManager : MonoBehaviour
{

    [Header("Game Objects")]
    private ChessBoard chessBoard;
    private GameManager gameManager;

    [Header("API Services")]
    private MatchmakingApi matchmakingApi;
    private GameAPI gameAPI;
    private MovesAPI movesAPI;
    private Coroutine pollingCoroutine;
    private bool isFinalizingGame = false;

    [Header("Online Multiplayer Settings")]
    private bool isPolling;
    public bool isApplyingNetworkMove;
    public bool IsSendingMove { get; private set; }
    private int lastAppliedMoveNumber = 0;
    private bool isInitializedFromPrefs = false;

    private void Awake()
    {
        gameManager = GameManager.Instance;
        gameAPI = gameManager.gameAPI;
        movesAPI = gameManager.movesApi;
    }

    private void Start()
    {
        StartCoroutine(InitializeFromScene());
    }

    private void OnEnable()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;

        if (gameManager != null)
            gameManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnTurnChanged -= HandleTurnChanged;
    }

    private IEnumerator InitializeFromScene()
    {
        int safety = 120;
        while (safety-- > 0 && (gameManager == null || chessBoard == null))
        {
            EnsureReferences();
            if (gameManager != null && chessBoard != null)
                break;

            yield return null;
        }

        TryInitializeFromPlayerPrefs();
    }

    private void EnsureReferences()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();

        if (chessBoard == null)
            chessBoard = ChessBoard.Instance ?? FindFirstObjectByType<ChessBoard>();

        if (gameManager != null)
        {
            if (gameAPI == null)
                gameAPI = gameManager.gameAPI;
            if (movesAPI == null)
                movesAPI = gameManager.movesApi;
        }
    }

    private void TryInitializeFromPlayerPrefs()
    {
        if (isInitializedFromPrefs)
            return;

        int gameId = PlayerPrefs.GetInt("CurrentGameId", -1);
        string myColor = PlayerPrefs.GetString("MyColor", string.Empty);

        if (gameId <= 0 || string.IsNullOrEmpty(myColor))
            return;

        string normalizedColor = NormalizeColor(myColor);

        StartOnlineGame(gameId, normalizedColor);
        ApplyPlayerNames(normalizedColor);

        isInitializedFromPrefs = true;
    }

    private string NormalizeColor(string color)
    {
        if (string.IsNullOrEmpty(color))
            return "white";

        string c = color.Trim().ToLowerInvariant();
        return (c == "white" || c == "black") ? c : "white";
    }

    private void ApplyPlayerNames(string myColor)
    {
        if (gameManager == null)
            return;

        string myName = SessionManager.Instance != null
            ? SessionManager.Instance.Username
            : PlayerPrefs.GetString("username", "Player");

        string opponentName = PlayerPrefs.GetString("OpponentName", "Opponent");

        if (string.Equals(myColor, "white", StringComparison.OrdinalIgnoreCase))
        {
            gameManager.SetPlayerNames(myName, opponentName);
        }
        else
        {
            gameManager.SetPlayerNames(opponentName, myName);
        }
    }

    private void HandleTurnChanged(Team currentTurn)
    {
        if (gameManager == null || gameManager.gameModeManager == null)
            return;

        if (gameManager.gameModeManager.CurrentMode != GameModeManager.GameModes.Online)
            return;

        if (gameManager.myLocalTeam == Team.None)
            return;

        if (gameManager.IsMyTurn())
        {
            StopPolling();
        }
        else
        {
            StartPolling();
        }
    }


    public void StartOnlineGame(int gameId, string myColor)
    {
        EnsureReferences();

        gameManager.gameModeManager.SetMode(GameModeManager.GameModes.Online);
        gameManager.SetGameId(gameId);
        gameManager.SetTurn(Team.White);
        gameManager.SetPlayerTypes(GameManager.PlayerType.Human, GameManager.PlayerType.Human);
        gameManager.SetLocalTeam(
            myColor == "white" ? Team.White : Team.Black
        );
        gameManager.SetGameStarted(true);
        lastAppliedMoveNumber = 0;
        if (!gameManager.IsMyTurn())
            StartPolling();
    }

    public void StartPolling()
    {
        if (isPolling)
            return;

        isPolling = true;
        pollingCoroutine = StartCoroutine(PollLoop());
    }

    public void StopPolling()
    {
        isPolling = false;

        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
    }

    private IEnumerator PollLoop()
    {
        while (isPolling)
        {
            if (gameManager.IsMyTurn())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            yield return new WaitForSeconds(2f);

            if (gameManager.IsGameOver())
                yield break;

            yield return CheckGameStatus();

            if (gameManager.IsGameOver())
                yield break;

            yield return CheckLatestMove();
        }
    }

    private IEnumerator CheckGameStatus()
    {
        if (gameAPI == null)
            yield break;

        bool done = false;
        GameStatusDto statusDto = null;
        string errorMsg = null;

        yield return gameAPI.GetGameStatus(
            gameManager.currentGameId,
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
                Debug.LogWarning($"âš  Status Check Error: {errorMsg}");
            yield break;
        }

        if (statusDto.Status != "in_progress")
        {
            if (isFinalizingGame)
                yield break;

            isFinalizingGame = true;

            Debug.Log($"âš  Game Status Changed: {statusDto.Status}");

            // Fetch latest move before finishing the game to sync board state
            yield return CheckLatestMove();

            ApplyGameOverFromStatus(statusDto.Status);
            StopPolling();
        }
    }

    private void ApplyGameOverFromStatus(string status)
    {
        if (gameManager == null || gameManager.IsGameOver())
            return;

        string s = string.IsNullOrEmpty(status) ? string.Empty : status.Trim().ToLowerInvariant();

        switch (s)
        {
            case "white_wins":
                gameManager.GameOver(Team.White, s);
                break;
            case "black_wins":
                gameManager.GameOver(Team.Black, s);
                break;
            case "draw":
            case "stalemate":
            case "fifty_move_rule":
                gameManager.GameOver(Team.None, s);
                break;
            default:
                Debug.LogWarning($"âš  Unknown game status: {status}");
                gameManager.GameOver(Team.None, s);
                break;
        }
    }


    private IEnumerator CheckLatestMove()
    {
        if (movesAPI == null)
            yield break;

        bool done = false;

        yield return movesAPI.GetLatestMove(
            gameManager.currentGameId,
            (moveData) =>
            {
                if (moveData == null)
                {
                    Debug.Log("[OnlineSession] LatestMove is null");
                    done = true;
                    return;
                }

                Debug.Log($"[OnlineSession] LatestMove moveNumber={moveData.move_number} lastApplied={lastAppliedMoveNumber}");
                if (moveData.move_number <= lastAppliedMoveNumber)
                {
                    Debug.Log("[OnlineSession] LatestMove ignored (not newer)");
                    done = true;
                    return;
                }

                ApplyNetworkMove(moveData);
                done = true;
            });

        yield return new WaitUntil(() => done);
    }

    private void ApplyNetworkMove(MoveDto moveData)
    {
        if (chessBoard == null)
            return;

        isApplyingNetworkMove = true;

        chessBoard.ApplyNetworkMove(moveData);

        isApplyingNetworkMove = false;

        lastAppliedMoveNumber = moveData.move_number;
        gameManager.SetMoveCount(moveData.move_number);

        gameManager.SwitchTurn();
    }


    public void OnLocalPlayerMoved(Vector2Int from, Vector2Int to, ChessPiece.PieceType pieceType)
    {
        if (gameManager.gameModeManager.CurrentMode != GameModeManager.GameModes.Online)
            return;

        if (!gameManager.IsMyTurn())
        {
            Debug.LogWarning("âš  Not your turn.");
            return;
        }

        if (movesAPI == null)
            return;

        MoveCreateDto dto = CreateMoveDto(from, to, pieceType);

        StartCoroutine(SendMove(dto));
    }

    private MoveCreateDto CreateMoveDto(Vector2Int from, Vector2Int to, ChessPiece.PieceType pieceType)
    {
        return new MoveCreateDto
        {
            GameId = gameManager.currentGameId,
            MoveNumber = gameManager.moveCount + 1,
            StartX = from.x,
            StartY = from.y,
            EndX = to.x,
            EndY = to.y,
            PieceType = (int)pieceType,
            PlayerTurn = (gameManager.CurrentTurn == Team.White) ? 0 : 1
        };
    }

    private IEnumerator SendMove(MoveCreateDto dto)
    {
        bool done = false;
        IsSendingMove = true;

        yield return movesAPI.SendMove(dto, (success) =>
        {
            if (success)
            {
                lastAppliedMoveNumber = dto.MoveNumber;
                gameManager.SetMoveCount(dto.MoveNumber);
                gameManager.SwitchTurn();
                StartPolling();
            }
            else
            {
                Debug.LogError("âŒ Send Move Failed");
            }

            IsSendingMove = false;
            done = true;
        });

        yield return new WaitUntil(() => done);
    }

    public void ExitOnline()
    {
        if (gameManager.currentGameId <= 0 || gameAPI == null)
            return;

        StartCoroutine(gameAPI.ResignOnlineGame(
            gameManager.currentGameId,
            "abandoned",
            null,
            null));
    }
}
