using System.Collections;
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

    [Header("Online Multiplayer Settings")]
    private bool isPolling;
    public bool isApplyingNetworkMove;
    private int lastAppliedMoveNumber = 0;

    private void Awake()
    {
        gameManager = GameManager.Instance;
        gameAPI = gameManager.gameAPI;
        movesAPI = gameManager.movesApi;
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
        gameManager.gameModeManager.SetMode(GameModeManager.GameModes.Online);
        gameManager.SetGameId(gameId);
        gameManager.SetTurn(Team.White);
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

        yield return gameAPI.GetGameStatus(
            gameManager.currentGameId,
            (statusDto) =>
            {
                if (statusDto == null)
                {
                    done = true;
                    return;
                }

                if (statusDto.Status != "in_progress")
                {
                    StopPolling();
                    Debug.Log($"⚠ Game Status Changed: {statusDto.Status}");
                }

                done = true;
            },
            (error) =>
            {
                Debug.LogWarning($"⚠ Status Check Error: {error}");
                done = true;
            });

        yield return new WaitUntil(() => done);
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
                    done = true;
                    return;
                }

                if (moveData.move_number <= lastAppliedMoveNumber)
                {
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
            Debug.LogWarning("⚠ Not your turn.");
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
                Debug.LogError("❌ Send Move Failed");
            }

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
