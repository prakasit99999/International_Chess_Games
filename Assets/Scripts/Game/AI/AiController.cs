using System.Collections;
using System.Diagnostics;
using static UnityEngine.Debug;
using System.Collections.Generic;
using Game.Interfaces;
using AIEngine.Adapters;
using UnityEngine;
using static ChessPiece;

public class AiController : MonoBehaviour
{
    [Header("Game Objects")]
    private GameManager gameManager;
    private ChessBoard chessBoard;
    private HistoryMove historyMove;
    private IChessAI chessAI;
    private Coroutine aiLoop;
    private Team? pendingTeam;
    private bool warnedMissingAI;
    public bool isAIMode = false;

    private void Awake()
    {
        InitializeRefs();
    }

    private void OnEnable()
    {
        InitializeRefs();
    }

    private void InitializeRefs()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (chessBoard == null)
            chessBoard = FindFirstObjectByType<ChessBoard>();
        if (historyMove == null)
            historyMove = FindFirstObjectByType<HistoryMove>();
        if (chessAI == null)
            chessAI = GetComponent<IChessAI>();
    }

    public void StartAI()
    {
        if (aiLoop != null)
            StopCoroutine(aiLoop);

        aiLoop = StartCoroutine(AIPlayLoop());
    }


    private IEnumerator AIPlayLoop()
    {
        while (gameManager != null && !gameManager.IsGameOver())
        {
            while (PauseManager.isPaused || ChessBoard.Instance.IsPromoting())
                yield return null;

            if (!IsCurrentPlayerAI())
            {
                pendingTeam = null;
                yield return null;
                continue;
            }

            if (chessAI == null || chessBoard == null)
            {
                if (!warnedMissingAI)
                {
                    // Debug.LogWarning("⚠️ AI components missing. Cannot calculate AI move.");
                    warnedMissingAI = true;
                }
                yield return null;
                continue;
            }

            if (chessBoard.PiecesOnBoard == null || chessBoard.PiecesOnBoard.Count == 0)
            {
                pendingTeam = null;
                yield return null;
                continue;
            }

            Team team = gameManager.CurrentTurn;
            if (pendingTeam == null || pendingTeam.Value != team)
            {
                pendingTeam = team;
                chessAI.ClearCalculatedMove();
                var difficulty = GetDifficultyForTeam(team);
                chessAI.StartCalculateMove(chessBoard, team, gameManager.CurrentTurn, difficulty);
            }

            var move = chessAI.GetCalculatedMove();

            if (move.HasValue)
            {
                var difficulty = GetDifficultyForTeam(team);
                var aiStats = CreateAiStats(difficulty);
                var chosenMove = move.Value;
                if (difficulty == AIDifficulty.Easy &&
                    IsBacktrackMove(team, chosenMove.from, chosenMove.to) &&
                    TryGetAlternativeMove(team, chosenMove.from, chosenMove.to, out var alt))
                {
                    chosenMove = alt;
                }

                ExecuteAIMove(chosenMove.from, chosenMove.to, aiStats);
                chessAI.ClearCalculatedMove();
                pendingTeam = null;
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                yield return null;
            }
        }
    }

    private bool IsCurrentPlayerAI()
    {
        if (gameManager == null)
            return false;

        return (gameManager.CurrentTurn == Team.White &&
                gameManager.WhitePlayer == GameManager.PlayerType.AI)
            ||
               (gameManager.CurrentTurn == Team.Black &&
                gameManager.BlackPlayer == GameManager.PlayerType.AI);
    }

    private void ExecuteAIMove(Vector2Int from, Vector2Int to, AiPerformanceData aiStats = null)
    {
        if (gameManager == null || chessBoard == null)
            return;

        if (!chessBoard.PiecesOnBoard.TryGetValue(from, out ChessPiece piece))
            return;

        if (piece.team != gameManager.CurrentTurn)
            return;

        var turnBeforeMove = gameManager.CurrentTurn;

        chessBoard.SelectPiece(piece);
        chessBoard.MoveSelectedPiece(to, aiStats);

        // Fallback: if turn didn't switch (offline), switch manually
        if (gameManager.CurrentTurn == turnBeforeMove)
        {
            if (chessBoard.PiecesOnBoard.TryGetValue(to, out ChessPiece moved) && moved == piece)
            {
                gameManager.SwitchTurn();
            }
        }
    }

    private AIDifficulty GetDifficultyForTeam(Team team)
    {
        var modeManager = gameManager != null
            ? gameManager.gameModeManager
            : GameModeManager.Instance;

        string diff = modeManager != null ? modeManager.SelectedDifficulty : "medium";

        if (modeManager != null && modeManager.CurrentMode == GameModeManager.GameModes.AIVsAI)
        {
            diff = (team == Team.White)
                ? modeManager.SelectedWhiteDifficulty
                : modeManager.SelectedBlackDifficulty;
        }

        return MapDifficulty(diff);
    }

    private AIDifficulty MapDifficulty(string value)
    {
        if (string.IsNullOrEmpty(value))
            return AIDifficulty.Easy;

        string v = value.Trim().ToLowerInvariant();
        if (v == "normal") v = "medium";

        return v switch
        {
            "hard" => AIDifficulty.Hard,
            "medium" => AIDifficulty.Normal,
            "easy" => AIDifficulty.Easy,
            _ => AIDifficulty.Easy
        };
    }

    private AiPerformanceData CreateAiStats(AIDifficulty difficulty)
    {
        if (chessAI is not UnityAIBoardAdapter adapter)
            return null;

        string level = difficulty switch
        {
            AIDifficulty.Hard => "hard",
            AIDifficulty.Normal => "medium",
            _ => "easy"
        };

        string algorithmType = (level == "easy") ? "minimax" : "alpha_beta";

        var data = new AiPerformanceData
        {
            AiLevel = level,
            AlgorithmType = algorithmType,
            Depth = adapter.LastDepth,
            Nodes = adapter.NodesEvaluated,
            MoveTimeMs = Mathf.RoundToInt(adapter.LastMoveTimeMs),
            Score = adapter.LastEvalScore
        };

        PerformanceTracker.Instance?.AddMove(data.Depth, data.Nodes, data.MoveTimeMs, data.Score);
        return data;
    }

    private bool IsBacktrackMove(Team team, Vector2Int from, Vector2Int to)
    {
        if (historyMove == null)
            return false;

        var history = historyMove.GetMoveHistory();
        if (history == null || history.Count == 0)
            return false;

        foreach (var move in history)
        {
            if (move.team != team)
                continue;

            return move.startPosition == to && move.endPosition == from;
        }

        return false;
    }

    private bool TryGetAlternativeMove(Team team, Vector2Int avoidFrom, Vector2Int avoidTo, out (Vector2Int from, Vector2Int to) alt)
    {
        alt = default;

        if (chessBoard == null || chessBoard.PiecesOnBoard == null)
            return false;

        // Snapshot the pieces to avoid enumerating while the board is temporarily modified
        // inside GetValidMoves -> DoesMoveExposeKing.
        var piecesSnapshot = new List<ChessPiece>(chessBoard.PiecesOnBoard.Values);
        foreach (var piece in piecesSnapshot)
        {
            if (piece == null || piece.team != team)
                continue;

            var moves = piece.GetValidMoves();
            for (int i = 0; i < moves.Count; i++)
            {
                var to = moves[i];
                var from = piece.boardPosition;
                if (from == avoidFrom && to == avoidTo)
                    continue;

                alt = (from, to);
                return true;
            }
        }

        return false;
    }
}
