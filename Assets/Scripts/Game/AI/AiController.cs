using System.Collections;
using Game.Interfaces;
using UnityEngine;
using static ChessPiece;

public class AiController : MonoBehaviour
{
    [Header("Game Objects")]
    private GameManager gameManager;
    private ChessBoard chessBoard;
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
                    Debug.LogWarning("⚠️ AI components missing. Cannot calculate AI move.");
                    warnedMissingAI = true;
                }
                yield return null;
                continue;
            }

            Team team = gameManager.CurrentTurn;
            if (pendingTeam == null || pendingTeam.Value != team)
            {
                pendingTeam = team;
                chessAI.ClearCalculatedMove();
                chessAI.StartCalculateMove(chessBoard, team, gameManager.CurrentTurn, GetDifficultyForTeam(team));
            }

            var move = chessAI.GetCalculatedMove();

            if (move.HasValue)
            {
                ExecuteAIMove(move.Value.from, move.Value.to);
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

    private void ExecuteAIMove(Vector2Int from, Vector2Int to)
    {
        if (gameManager == null || chessBoard == null)
            return;

        if (!chessBoard.PiecesOnBoard.TryGetValue(from, out ChessPiece piece))
            return;

        if (piece.team != gameManager.CurrentTurn)
            return;

        chessBoard.SelectPiece(piece);
        chessBoard.MoveSelectedPiece(to, null);
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
}
