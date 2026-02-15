using System.Collections;
using UnityEngine;
using static ChessPiece;

public class GameSyncService : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private OnlineSyncService onlineSync;
    [SerializeField] private GameResultSyncService resultSync;
    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (onlineSync == null)
            onlineSync = GetComponent<OnlineSyncService>() ?? gameObject.AddComponent<OnlineSyncService>();

        if (resultSync == null)
            resultSync = GetComponent<GameResultSyncService>() ?? gameObject.AddComponent<GameResultSyncService>();
    }

    private void OnEnable()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        gameManager.OnMoveCompleted += HandleMoveCompleted;
        gameManager.OnResignRequested += HandleResignRequested;
        gameManager.OnExitRequested += HandleExitRequested;
        gameManager.OnGameOver += HandleGameOver;
        gameManager.OnReplayRequested += HandleReplayRequested;
    }

    private void OnDisable()
    {
        if (gameManager == null)
            return;

        gameManager.OnMoveCompleted -= HandleMoveCompleted;
        gameManager.OnResignRequested -= HandleResignRequested;
        gameManager.OnExitRequested -= HandleExitRequested;
        gameManager.OnGameOver -= HandleGameOver;
        gameManager.OnReplayRequested -= HandleReplayRequested;
    }

    public void OnChessBoardMoveCompleted(MoveResult result)
    {
        if (onlineSync != null)
            onlineSync.OnChessBoardMoveCompleted(result);
    }

    public IEnumerator HandleOnlineResign()
    {
        if (onlineSync == null)
            yield break;

        yield return onlineSync.HandleOnlineResign();
    }

    public IEnumerator SyncFullGameResult(Team winner, string endReason)
    {
        if (resultSync == null)
            yield break;

        yield return resultSync.SyncFullGameResult(winner, endReason);
    }

    public IEnumerator ExitLocalSequence()
    {
        if (resultSync == null)
            yield break;

        yield return resultSync.ExitLocalSequence();
    }

    public void RecordMove(HistoryMove.HistoryMoveData moveData, AiPerformanceData aiStats = null)
    {
        if (resultSync != null)
            resultSync.RecordMove(moveData, aiStats);
    }

    private void HandleMoveCompleted(MoveResult result)
    {
        OnChessBoardMoveCompleted(result);
    }

    private void HandleResignRequested()
    {
        StartCoroutine(HandleOnlineResign());
    }

    private void HandleExitRequested()
    {
        StartCoroutine(ExitLocalSequence());
    }

    private void HandleGameOver(Team winner, string endReason)
    {
        StartCoroutine(SyncFullGameResult(winner, endReason));
    }

    private void HandleReplayRequested()
    {
        if (resultSync == null)
            return;

        StartCoroutine(resultSync.ReplayOfflineGame());
    }
}
