
using System.Collections;
using System.Collections.Generic;
using AIEngine.Adapters;
using AIEngine.Utilities;
using Game.Interfaces;
using TMPro;
using UnityEngine;
using static ChessPiece;

public class GameManager : MonoBehaviour
{
    [Header("private AI")]
    private IChessAI chessAI;
    private Coroutine aiLoop;
    private Coroutine fiftyMoveCoroutine;
    private Coroutine syncCoroutine;
    private Coroutine onlinePollingCoroutine;
    private bool isAITurnActive = false;
    private string difficultyWhite;
    private string difficultyBlack;
    private string aiColor;

    [Header("private Game")]
    private GameModes currentMode = GameModes.SinglePlayer;
    private ChessPiece.Team currentTurn = ChessPiece.Team.White;
    private ChessBoard chessBoard;
    private HistoryMoveUI historyMoveUI;
    private List<MoveCreateDto> recordedMoves = new List<MoveCreateDto>();
    private int lastAppliedMoveNumber = 0; // ✅ ติดตาม move number ที่ apply ไปแล้ว (ป้องกันการ apply ซ้ำ)

    [Header("API Services")]
    public GameAPI gameAPI;
    public AiPerformanceAPI aiPerformanceApi;
    public MovesAPI movesApi;
    public MatchmakingApi matchmakingApi; // ✅ เพิ่ม MatchmakingApi reference
    public UserAPI userApi; // ✅ เพิ่ม UserAPI reference

    [Header("Game Data")]
    public int currentGameId = -1;
    public int moveCount = 0;

    [Header("UI")]
    public GameObject drawGamePanel;
    public GameObject winGamePanel;
    public GameObject loseGamePanel;
    public GameObject drawInfoPanel;
    public TMP_Text winTxt;
    public TMP_Text loseTxt;
    public TMP_Text drawTxt;
    public TMP_Text fiftyMoveText;
    public TMP_Text txtNameWhite;
    public TMP_Text txtNameBlack;

    [Header("Game State")]
    public static GameManager Instance;
    public Team CurrentTurn => currentTurn;
    public enum PlayerType { Human, AI }
    public enum GameModes { LocalMultiplayer, SinglePlayer, AIVsAI, Online }
    public AIDifficulty aiDifficultyWhite;
    public AIDifficulty aiDifficultyBlack;
    public AIDifficulty aiDifficulty;
    public bool isGameStarted;
    public bool isWhiteTurn;
    public bool isBlackTurn;
    public PlayerType WhitePlayer = PlayerType.Human;
    public PlayerType BlackPlayer = PlayerType.AI;
    public string whitePlayerName = "White";
    public string blackPlayerName = "Black";
    public bool isAIMode = false;
    public bool gameIsOver = false;

    // ✅ เพิ่มตัวแปรสำหรับจำว่า "ฉันคือใคร" ใน Online Mode
    public ChessPiece.Team myLocalTeam = ChessPiece.Team.None;

    [Header("Online Multiplayer Settings")]
    public bool isOnlineMode = false;
    private bool isWaitingForOpponent = false;
    private string myOnlineColor = "white";

    public GameManager(GameModes currentMode)
    {
        this.currentMode = currentMode;
    }

    [System.Obsolete]
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            chessBoard = FindObjectOfType<ChessBoard>();
            historyMoveUI = FindObjectOfType<HistoryMoveUI>();
        }
        else
        {
            Destroy(gameObject);
        }
        if (gameAPI == null) gameAPI = FindFirstObjectByType<GameAPI>();
        if (movesApi == null) movesApi = FindFirstObjectByType<MovesAPI>();
        if (matchmakingApi == null) matchmakingApi = FindFirstObjectByType<MatchmakingApi>(); // ✅ Init MatchmakingApi
        if (userApi == null) userApi = FindFirstObjectByType<UserAPI>(); // ✅ Init UserAPI
        if (aiPerformanceApi == null) aiPerformanceApi = FindFirstObjectByType<AiPerformanceAPI>();
        if (gameAPI == null || movesApi == null || aiPerformanceApi == null)
        {
            Debug.LogError("❌ API ไม่ถูกพบ! ตรวจสอบว่า API อยู่ในฉาก");
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        // --- 1. ส่วนเตรียมความพร้อม (ทำครั้งเดียวในชีวิต Object) ---
        if (chessBoard == null) chessBoard = FindObjectOfType<ChessBoard>();
        if (chessBoard != null)
        {
            chessBoard.SetGameManager(this);
            // หา Component ต่างๆ ให้ครบ
            if (chessAI == null) chessAI = GetComponent<UnityAIBoardAdapter>() ?? gameObject.AddComponent<UnityAIBoardAdapter>();
            if (aiPerformanceApi == null) aiPerformanceApi = GetComponent<AiPerformanceAPI>() ?? gameObject.AddComponent<AiPerformanceAPI>();
            // --- 2. เริ่มเกมรอบแรก ---
            StartGameSession();

        }
        else
        {
            Debug.LogError("❌ ChessBoard ไม่ถูกพบ!");
        }
    }

    void StartGameSession()
    {
        Debug.Log("🚀 Starting New Game Session...");

        // 1. รีเซ็ต Tracker
        PerformanceTracker.Instance?.ResetData();

        // 2. เลือกโหมดและสร้างเกม (เรียก API)
        ModeSelect();

        // 3. ตั้งค่า Tracker ตามระดับความยาก (ย้ายมาจาก Start เดิม)
        if (PerformanceTracker.Instance != null && (WhitePlayer == PlayerType.AI || BlackPlayer == PlayerType.AI))
        {
            string difficulty = PlayerPrefs.GetString("AI_Difficulty", "Easy");
            if (currentMode == GameModes.AIVsAI)
            {
                difficulty = PlayerPrefs.GetString("AI_Black_Difficulty", "Easy");
            }

            string level = difficulty.ToLower();
            if (level == "normal") level = "medium";

            // Easy ใช้ Minimax, อื่นๆ ใช้ AlphaBeta
            string algorithmType = (level == "easy") ? "minimax" : "alpha_beta";

            PerformanceTracker.Instance.AiLevel = level;
            PerformanceTracker.Instance.AlgorithmType = algorithmType;
            PerformanceTracker.Instance.GameId = currentGameId;
        }

        // 4. เริ่ม AI Loop 
        if (WhitePlayer == PlayerType.AI || BlackPlayer == PlayerType.AI)
        {
            if (aiLoop != null) StopCoroutine(aiLoop);
            aiLoop = StartCoroutine(AIPlayLoop());
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (PauseManager.isPaused || gameIsOver) return;
    }

    private IEnumerator AIPlayLoop()
    {
        while (!gameIsOver)
        {
            // รอ pause / promotion
            while (PauseManager.isPaused || gameIsOver || ChessBoard.Instance.IsPromoting())

                yield return null;

            if (IsCurrentPlayerAI())
            {
                // ✅ ให้ Unity render 1 frame ก่อนเริ่มคำนวณ AI เพื่อป้องกัน FPS freeze
                yield return null;


                var difficulty = (currentTurn == Team.White) ? aiDifficultyWhite : aiDifficultyBlack;

                // 🟢 ส่ง currentTurn ที่ถูกต้อง
                chessAI.StartCalculateMove(chessBoard, currentTurn, CurrentTurn, difficulty);

                yield return new WaitForSeconds(0.3f);

                // ✅ GetCalculatedMove() คืนค่าเป็น (Vector2Int from, Vector2Int to)?
                var move = chessAI.GetCalculatedMove();

                if (move.HasValue)
                {
                    // Execute AI Move
                    ExecuteAIMove(move.Value.from, move.Value.to);

                    // Clear cache move
                    chessAI.ClearCalculatedMove();

                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    Debug.LogWarning("❌ AI ไม่สามารถหา move ได้");
                }
            }

            yield return null;
        }
    }

    private void StartPolling()
    {
        if (onlinePollingCoroutine != null) StopCoroutine(onlinePollingCoroutine);
        isWaitingForOpponent = true;
        onlinePollingCoroutine = StartCoroutine(PollOpponentMove());
    }

    private IEnumerator PollOpponentMove()
    {
        while (isWaitingForOpponent)
        {
            // ✅ 1. ถ้าเป็นตาเรา ไม่ต้อง Poll (รอ SwitchTurn สั่งหยุด)
            if (IsMyTurn())
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            yield return new WaitForSeconds(2f);

            if (movesApi == null || gameAPI == null)
            {
                Debug.LogError("⚠️ API is missing!");
                yield break;
            }
            // -----------------------------------------------------------
            // 2. เช็คสถานะเกม (Resign / Abandoned)
            // -----------------------------------------------------------
            bool isGameEnded = false;
            yield return gameAPI.GetGameStatus(currentGameId, (statusDto) =>
            {
                if (statusDto == null) return;

                if (statusDto.status == "finished" || statusDto.status == "abandoned" || statusDto.status == "resignation")
                {
                    Debug.Log($"🏁 Game Over detected: {statusDto.status}");

                    ChessPiece.Team winner = ChessPiece.Team.None;
                    if (statusDto.winner == "white") winner = ChessPiece.Team.White;
                    if (statusDto.winner == "black") winner = ChessPiece.Team.Black;

                    isWaitingForOpponent = false;
                    onlinePollingCoroutine = null;
                    isGameEnded = true; // ตัดจบ Loop

                    GameOver(winner, statusDto.status);
                }
            }, (error) => { Debug.LogWarning($"⚠️ Check Status Error: {error}"); });

            if (isGameEnded) yield break;
            // -----------------------------------------------------------
            // 3. เช็ค Move ล่าสุด
            // -----------------------------------------------------------
            yield return movesApi.GetLatestMove(currentGameId, (moveData) =>
            {
                if (moveData == null) return;
                // ✅ เช็ค: MoveNumber (PascalCase) ต้องมากกว่าที่เราเคย Apply ไปแล้ว
                if (moveData.move_number <= lastAppliedMoveNumber)
                {
                    return;
                }
                Debug.Log($"📥 Received Move #{moveData.move_number} : {moveData.startX},{moveData.startY} -> {moveData.endX},{moveData.endY}");
                Debug.Log($"⚡ New Move Found #{moveData.move_number}: {moveData.from_position} -> {moveData.to_position}");
                // ✅ สั่งกระดานขยับ
                chessBoard.isNetworkMove = true;
                chessBoard.ApplyNetworkMove(moveData);
                chessBoard.isNetworkMove = false;
                // ✅ อัปเดตตัวนับ
                lastAppliedMoveNumber = moveData.move_number;
                moveCount = moveData.move_number;
                // ✅ สลับตามาเป็นของเรา (สำคัญ! บรรทัดนี้จะไปเซ็ต isWaitingForOpponent = false ให้เอง)
                SwitchTurn();
            });
        }
    }

    private AIDifficulty ParseDifficulty(string diff)
    {
        switch (diff)
        {
            case "Easy": return AIDifficulty.Easy;
            case "Normal": return AIDifficulty.Normal;
            case "Hard": return AIDifficulty.Hard;
            default: return AIDifficulty.Easy;
        }
    }

    private bool IsCurrentPlayerAI()
    {
        return (currentTurn == Team.White && WhitePlayer == PlayerType.AI) ||
            (currentTurn == Team.Black && BlackPlayer == PlayerType.AI);
    }

    private void ExecuteAIMove(Vector2Int from, Vector2Int to)
    {
        if (!chessBoard.PiecesOnBoard.TryGetValue(from, out ChessPiece piece))
        {
            Debug.LogWarning($"❌ AI tried to move from {from}, but no piece found!");
            return;
        }

        if (piece.team != currentTurn)
        {
            Debug.LogWarning($"🚨 AI tried to move wrong team! Piece={piece.team}, CurrentTurn={currentTurn}");
            return;
        }

        chessBoard.SelectPiece(piece);

        // สร้าง AiPerformanceData จาก adapter
        AiPerformanceData aiStats = null;
        var adapter = chessAI as AIEngine.Adapters.UnityAIBoardAdapter;


        if (adapter != null)
        {
            var difficulty = (currentTurn == Team.White) ? aiDifficultyWhite : aiDifficultyBlack;
            var algorithmType = (difficulty == AIDifficulty.Easy) ? "minimax" : "alpha_beta";
            aiStats = new AiPerformanceData
            {
                Score = adapter.LastEvalScore,
                Depth = adapter.LastDepth,
                Nodes = adapter.NodesEvaluated,
                MoveTimeMs = (int)adapter.LastMoveTimeMs,
                AlgorithmType = algorithmType,
            };

            if (PerformanceTracker.Instance != null)
            {
                PerformanceTracker.Instance.AddMove(aiStats.Depth, aiStats.Nodes, aiStats.MoveTimeMs, aiStats.Score);
            }
        }

        chessBoard.MoveSelectedPiece(to, aiStats);

        // ✅ log ก่อนสลับตา
        Debug.Log($"[CHECK] Piece={piece.team}, CurrentTurn={currentTurn}, From={from}, To={to}");

    }

    private void SetupAIVsAIMode()
    {
        currentMode = GameModes.AIVsAI;
        difficultyWhite = PlayerPrefs.GetString("AI_White_Difficulty", "Easy");
        difficultyBlack = PlayerPrefs.GetString("AI_Black_Difficulty", "Easy");
        SetAIDifficulty(difficultyWhite, difficultyBlack);
        SetPlayerTypes(PlayerType.AI, PlayerType.AI);
        Debug.Log($"[MODE] AI vs AI - White: {difficultyWhite}, Black: {difficultyBlack}");
    }

    private void SetupLocalMultiplayerMode()
    {
        currentMode = GameModes.LocalMultiplayer;
        SetPlayerTypes(PlayerType.Human, PlayerType.Human);
        Debug.Log("[MODE] Local Multiplayer - Human vs Human");
    }

    private void SetupSinglePlayerMode()
    {
        currentMode = GameModes.SinglePlayer;
        // ดึงระดับความยากจาก PlayerPrefs (ถ้าไม่มี default = Easy)
        string difficulty = PlayerPrefs.GetString("AI_Difficulty", "Easy");
        // กำหนดตายตัว: ขาว = Human, ดำ = AI
        SetPlayerTypes(PlayerType.Human, PlayerType.AI);
        // ตั้ง difficulty: ฝั่งขาว (Human) = None, ฝั่งดำ (AI) = ตามค่าที่เลือก
        aiDifficultyWhite = AIDifficulty.None;
        aiDifficultyBlack = ParseDifficulty(difficulty);
        // ส่งค่าความยากไปให้ AI system
        SetAIDifficulty(aiDifficultyWhite.ToString(), aiDifficultyBlack.ToString());
        // ตั้งชื่อผู้เล่น
        SetPlayerNamesAndTypes();
        Debug.Log($"[MODE] Single Player - White: Human, Black: AI ({difficulty})");
    }

    private void ResetGameData()
    {
        // แจ้งเตือนถ้า API หาย (แต่ไม่ให้พัง)
        if (gameAPI == null) Debug.LogError("⚠️ gameAPI is NULL!");
        if (movesApi == null) Debug.LogError("⚠️ movesApi is NULL!");
        if (aiPerformanceApi == null) Debug.LogError("⚠️ aiPerformanceApi is NULL!");
        currentGameId = -1;
        moveCount = 0;
        // เพิ่มการเช็ค Null ตรงนี้ เพื่อแก้ Error บรรทัด 290
        if (recordedMoves == null) recordedMoves = new List<MoveCreateDto>();
        recordedMoves.Clear();

        gameIsOver = false;
    }

    private void SetupOnlineMultiplayerMode()
    {
        currentMode = GameModes.Online;
        Debug.Log("[MODE] Online Multiplayer");
        // 1. เช็คว่ามี GameID ส่งมาจากหน้า Matchmaking ไหม
        if (PlayerPrefs.HasKey("CurrentGameId"))
        {
            isOnlineMode = true;
            currentGameId = PlayerPrefs.GetInt("CurrentGameId");
            myOnlineColor = PlayerPrefs.GetString("MyColor"); // "white" หรือ "black"
            // ✅ ตั้งค่าทีมของเรา
            if (myOnlineColor == "white") myLocalTeam = ChessPiece.Team.White;
            else if (myOnlineColor == "black") myLocalTeam = ChessPiece.Team.Black;
            else myLocalTeam = ChessPiece.Team.None; // Error Case
            string opponentName = PlayerPrefs.GetString("OpponentName");
            string myName = PlayerPrefs.GetString("username", "Me"); // ✅ ดึงชื่อเรามาแสดง
            // ตั้งค่าชื่อบน UI
            if (myOnlineColor == "white")
            {
                txtNameWhite.text = myName;
                txtNameBlack.text = opponentName;
            }
            else
            {
                txtNameWhite.text = opponentName;
                txtNameBlack.text = myName;
            }
            Debug.Log($"[Online] Game Started! ID: {currentGameId}, Color: {myOnlineColor}");
            Debug.Log($"🎮 Online Setup: My Team = {myLocalTeam}, Game ID = {currentGameId}");
            // 2. ถ้าเป็นสีดำ (ต้องรอขาวเดินก่อน) -> เริ่ม Polling รอรับค่า
            if (!IsMyTurn())
            {
                Debug.Log("zzz รอคู่แข่งเดิน (Start Polling)...");
                isWaitingForOpponent = true;
                if (onlinePollingCoroutine != null) StopCoroutine(onlinePollingCoroutine);
                onlinePollingCoroutine = StartCoroutine(PollOpponentMove());
            }
            // ✅ อัปเดตสถานะเป็น "playing"
            int userId = PlayerPrefs.GetInt("UserId", 0);
            if (userApi != null)
            {
                StartCoroutine(userApi.UpdateStatus(userId, "playing", (success, message) =>
                {
                    if (!success) Debug.LogWarning($"⚠️ Failed to update status to playing: {message}");
                }));
            }
            // ล้างค่าทิ้ง เพื่อไม่ให้บั๊กเวลากลับมาหน้าเมนู
            PlayerPrefs.DeleteKey("CurrentGameId");
        }
        else
        {
            Debug.LogError("❌ Online Mode selected but No Game ID found in PlayerPrefs!");
        }
    }

    private string GetPlayerTypeString(PlayerType type, AIDifficulty difficulty)
    {
        if (type == PlayerType.Human) return "human";

        return difficulty switch
        {
            AIDifficulty.Easy => "ai_easy",
            AIDifficulty.Normal => "ai_medium",
            AIDifficulty.Hard => "ai_hard",
            _ => "ai_easy"
        };
    }

    private ChessPiece.Team GetOpponentTeam(ChessPiece.Team team)
    {
        return (team == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
    }
    // ฟังก์ชันใหม่สำหรับส่งข้อมูล Batch + จบเกม
    private IEnumerator AutoHideFiftyMovePanel(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (drawInfoPanel != null) drawInfoPanel.SetActive(false);
        fiftyMoveCoroutine = null;
    }

    private IEnumerator SyncAndEndGame(Team winner, string endReason)
    {
        Debug.Log($"🔄 SyncAndEndGame Started... Winner: {winner}, Reason: {endReason}");

        //  1. ดึงข้อมูลการเดินจาก HistoryMove และแปลงเป็น DTO
        List<MoveCreateDto> finalMovesToSend = new List<MoveCreateDto>();
        var historyComponent = FindObjectOfType<HistoryMove>();

        if (historyComponent != null)
        {
            // GetHistory() คืนค่าเป็น Stack (LIFO: ล่าสุดอยู่บน)
            // เราต้องแปลงเป็น List แล้ว Reverse เพื่อให้ Move 1 มาก่อน
            var historyStack = historyComponent.GetMoveHistory();
            var historyList = new List<HistoryMove.HistoryMoveData>(historyStack);
            historyList.Reverse(); // กลับด้านให้เป็น Move 1, 2, 3...

            int moveNum = 1;
            // 1. เตรียมชื่อ Algorithm ของแต่ละฝั่งไว้ก่อน (Optimization)
            string whiteAlgo = "None";
            string blackAlgo = "None";

            // กำหนด Algo ฝั่งขาว
            if (WhitePlayer == PlayerType.AI)
            {
                whiteAlgo = (aiDifficultyWhite == AIDifficulty.Easy) ? "minimax" : "alpha_beta";
            }

            // กำหนด Algo ฝั่งดำ
            if (BlackPlayer == PlayerType.AI)
            {
                blackAlgo = (aiDifficultyBlack == AIDifficulty.Easy) ? "minimax" : "alpha_beta";
            }

            // ดึงค่า Algorithm Type จาก Tracker (ถ้ามี) เพื่อให้ข้อมูลไม่หายไปทั้งหมด
            foreach (var moveData in historyList)
            {
                string currentAlgo = "None";

                if (moveData.team == ChessPiece.Team.White)
                {
                    currentAlgo = whiteAlgo;
                }
                else if (moveData.team == ChessPiece.Team.Black)
                {
                    currentAlgo = blackAlgo;
                }
                // เพราะ HistoryMoveData ไม่ได้เก็บค่าเหล่านั้นไว้
                AiPerformanceData dummyAi = new AiPerformanceData
                {
                    Score = moveData.score,
                    Depth = moveData.depth,
                    Nodes = moveData.nodes,
                    MoveTimeMs = moveData.moveTimeMs,
                    AlgorithmType = currentAlgo
                };

                // ใช้ MoveMapper แปลงข้อมูล
                var moveDto = MoveMapper.ToDto(moveData, currentGameId, moveNum, dummyAi);
                finalMovesToSend.Add(moveDto);
                moveNum++;
            }

            // อัปเดตตัวแปร recordedMoves ของ GameManager ให้ตรงกันเผื่อต้องใช้ที่อื่น
            recordedMoves = finalMovesToSend;
            Debug.Log($"finalMovesToSend {finalMovesToSend}");
        }
        else
        {
            Debug.LogError("❌ ไม่เจอ HistoryMove Component! ข้อมูลการเดินจะว่างเปล่า");
        }
        //  2. ส่งข้อมูล Moves Batch
        if (movesApi != null && finalMovesToSend.Count > 0)
        {
            bool uploadDone = false;
            yield return movesApi.SendMovesBatch(finalMovesToSend, (success) =>
            {
                if (success) Debug.Log("✅ Uploaded Moves Successfully");
                else Debug.LogError("❌ Failed to upload moves!");
                uploadDone = true;
            });
        }
        else
        {
            Debug.LogWarning("⚠️ ไม่มีข้อมูลการเดินที่จะส่ง (Moves list is empty)");
        }

        //  3. ส่งข้อมูล AI Performance (ถ้าไม่ใช่ Online Multiplayer)
        string modeStr = PlayerPrefs.GetString("Mode", "SinglePlayer");
        bool hasAI = (WhitePlayer == PlayerType.AI || BlackPlayer == PlayerType.AI);
        if (hasAI && aiPerformanceApi != null && PerformanceTracker.Instance != null)
        {
            Debug.Log("🔄 Uploading AI Performance...");
            var perfData = PerformanceTracker.Instance.Export();
            Debug.Log($"perfData: {perfData.AiLevel}, {perfData.AverageDepth},{perfData.AverageMoveTimeMs}, {perfData.AverageNodesEvaluated},{perfData.AlgorithmType}");

            // เช็คป้องกันอีกชั้น เผื่อ Export ส่งค่า null
            if (perfData == null || !perfData.HasAnyData())
            {
                Debug.LogWarning("⚠️ AI Performance data is null or empty, skip upload");
            }
            else
            {
                perfData.GameId = currentGameId;
                // มั่นใจได้ว่าไม่พังเพราะเช็ค aiPerformanceApi ไว้ที่ด้านบนแล้ว
                yield return aiPerformanceApi.SendPerformance(perfData);
            }
        }
        else
        {
            // ถ้ามันเข้าตรงนี้ แสดงว่าคุณลืมเตรียม Object ไว้ใน Unity
            Debug.LogError("❌ Cannot upload performance: aiPerformanceApi or PerformanceTracker is MISSING!");
        }
        //  4. ส่งข้อมูลจบเกม (Game Result)
        yield return StartCoroutine(ProcessGameFinished(winner, endReason, finalMovesToSend.Count));
        syncCoroutine = null;
    }

    private IEnumerator ReturnMainSequence()
    {
        // 0. ถ้ามีการส่งข้อมูลจบเกมค้างอยู่ ให้รอจนเสร็จก่อน (สำคัญมาก!)
        if (syncCoroutine != null)
        {
            Debug.Log("⏳ Waiting for SyncAndEndGame to complete...");
            yield return syncCoroutine;
        }

        // 1. ถ้าเป็น Offline (Local) ไป Reset เลย
        if (currentGameId == -1)
        {
            DoResetAndLeave();
            yield break;
        }

        // 2. ล็อกหน้าจอ หรือขึ้น Loading (ถ้ามี) ระหว่างรอส่งข้อมูล
        // if (loadingPanel != null) loadingPanel.SetActive(true);
        string modeStr = PlayerPrefs.GetString("Mode", "SinglePlayer");
        bool isAbandoned = !gameIsOver;

        // ✅ แก้ไข: ส่งข้อมูลตราบใดที่มี GameId (ไม่ว่าจะเป็นโหมดไหน หรือกดออกกลางคัน)

        bool shouldSendData = (currentGameId != -1);

        // --- A. ส่ง Moves และรอจนเสร็จ ---
        if (shouldSendData && movesApi != null && recordedMoves.Count > 0)
        {
            bool uploadDone = false;
            // ใช้ท่า yield return StartCoroutine เพื่อรอให้จบจริง
            yield return movesApi.SendMovesBatch(recordedMoves, (success) =>
            {
                if (success) Debug.Log("📤 Moves sent successfully.");
                uploadDone = true;
            });
            yield return null;
        }

        // --- B. ส่ง AI Performance (ไม่ต้องรอมาก เพราะส่งแยกกันได้ แต่รอหน่อยก็ดี) ---
        if (shouldSendData && modeStr != "OnlineMultiplayer" && aiPerformanceApi != null && PerformanceTracker.Instance != null)
        {
            var perfData = PerformanceTracker.Instance.Export();
            perfData.GameId = currentGameId;
            yield return aiPerformanceApi.SendPerformance(perfData);
        }

        // --- C. แจ้ง Resign (สำคัญมาก ต้องรอให้ Server รับรู้ก่อนปิด) ---
        if (isAbandoned && gameAPI != null)
        {
            int resigningPlayerId = -1;
            if (PerformanceTracker.Instance != null)
            {
                resigningPlayerId = PerformanceTracker.Instance.UserId;
            }

            // รอ Server ตอบกลับว่า Resign สำเร็จ
            bool resignDone = false;
            yield return gameAPI.ResignGame(currentGameId, resigningPlayerId, "resignation", (success) =>
            {
                Debug.Log("🏁 Resign acknowledged by server.");
                resignDone = true;
            });
        }

        // 3. ทุกอย่างเสร็จสิ้น -> เปลี่ยน Scene ได้อย่างปลอดภัย
        DoResetAndLeave();
    }

    private IEnumerator ProcessGameFinished(ChessPiece.Team winningTeam, string endReason, int totalMoves)
    {
        Debug.Log("⏳ Finalizing Game Result...");

        if (gameAPI != null)
        {
            // แปลงผลแพ้ชนะเป็น String ตามที่ Backend ต้องการ
            string resultStr = "draw";
            if (winningTeam == Team.White) resultStr = "white_wins";
            else if (winningTeam == Team.Black) resultStr = "black_wins";

            GameResultDto resultDto = new GameResultDto
            {
                GameId = currentGameId,
                Result = resultStr,
                ResultReason = endReason, // เช่น "checkmate", "stalemate", "draw_repetition"
                MoveCount = totalMoves    // จำนวนตาเดินรวม
            };

            // เรียก API จบเกม
            yield return gameAPI.FinalizeGame(resultDto, (success) =>
            {
                Debug.Log(success ? "🏁 Game Finalized Successfully" : "❌ Failed to finalize game");
            });
        }
        else
        {
            Debug.LogWarning("⚠️ GameAPI is null, cannot finalize game.");
        }
    }
    //set method
    public void SetPlayerNamesAndTypes()
    {
        string whiteDisplayName = "White";
        string blackDisplayName = "Black";
        string localPlayerName = PlayerPrefs.GetString("PlayerName");
        if (string.IsNullOrEmpty(localPlayerName)) localPlayerName = "Human";
        Debug.Log($"aiDifficultyWhite: {aiDifficultyWhite}, aiDifficultyBlack: {aiDifficultyBlack}");
        switch (currentMode)
        {
            case GameModes.AIVsAI:
                whiteDisplayName = $"AI ({aiDifficultyWhite}) (White)";
                blackDisplayName = $"AI ({aiDifficultyBlack}) (Black)";
                break;
            case GameModes.SinglePlayer:
                whiteDisplayName = $"White {localPlayerName}";
                blackDisplayName = $"AI ({aiDifficultyBlack}) (Black)";
                break;
            case GameModes.LocalMultiplayer:
                whiteDisplayName = "Human (White)";
                blackDisplayName = "Human (Black)";
                break;
            case GameModes.Online:
                whiteDisplayName = !string.IsNullOrEmpty(whitePlayerName) ? whitePlayerName : "Online Player";
                blackDisplayName = !string.IsNullOrEmpty(blackPlayerName) ? blackPlayerName : "Online Player";
                break;
            default:
                whiteDisplayName = "White";
                blackDisplayName = "Black";
                break;
        }

        Debug.Log($"[GameManager] SetNames ({currentMode}): White='{whiteDisplayName}', Black='{blackDisplayName}'");

        if (txtNameWhite != null)
            txtNameWhite.text = whiteDisplayName;

        if (txtNameBlack != null)
            txtNameBlack.text = blackDisplayName;
    }

    public void SetGameStarted(bool value)
    {
        isGameStarted = value;
    }

    public void SetPlayerNames(string whiteName, string blackName)
    {
        whitePlayerName = whiteName;
        blackPlayerName = blackName;
        UpdatePlayerTurnUI(); // อัปเดต UI ทันที
    }

    public void SetAIDifficulty(string whiteDiff, string blackDiff)
    {
        aiDifficultyWhite = ParseDifficulty(whiteDiff);
        aiDifficultyBlack = ParseDifficulty(blackDiff);
    }
    // ตั้งค่าประเภทผู้เล่น
    public void SetPlayerTypes(PlayerType whiteType, PlayerType blackType)
    {
        WhitePlayer = whiteType;
        BlackPlayer = blackType;
        SetPlayerNamesAndTypes();
    }
    //Get method
    // ตรวจสอบว่าตอนนี้เป็นตาของทีมไหน
    public ChessPiece.Team GetCurrentTurn()
    {
        return currentTurn;
    }
    // ✅ เช็คว่าเป็นตาของเราหรือไม่ (ใช้สำหรับล็อคการกด)
    public bool IsMyTurn()
    {
        if (!isOnlineMode) return true; // Offline เล่นได้ตลอด (หรือตาม Logic เดิม)
        return currentTurn == myLocalTeam;
    }
    // ✅ ฟังก์ชันส่ง API แยก (เรียกเฉพาะตอนผู้เล่นกดเดินเอง)
    public void OnLocalPlayerMoved(Vector2Int from, Vector2Int to, ChessPiece.PieceType pieceType)
    {
        // 1. เช็คว่าเป็นโหมด Online หรือไม่?
        if (currentMode != GameModes.Online) return;

        // 2. เช็คว่าเป็นตาเราจริงหรือไม่
        if (!IsMyTurn())

        {
            Debug.LogWarning("⚠️ Attempted to send move but it's not my turn.");
            return;
        }

        Debug.Log("📡 Sending Move to Server...");

        // 3. ดึงข้อมูลจาก HistoryMove แทนการสร้างใหม่เอง เพื่อให้ได้ Flag ครบถ้วน (Castling, Check, etc.)
        MoveCreateDto moveDto = null;
        var history = FindFirstObjectByType<HistoryMove>();


        if (history != null && history.GetMoveHistory().Count > 0)
        {
            var lastMoveData = history.GetMoveHistory().Peek();
            // เช็คว่า Move ที่ดึงมา ตรงกับที่เรากำลังจะส่งหรือไม่ (ป้องกัน Race Condition)
            if (lastMoveData.endPosition == to && lastMoveData.startPosition == from)
            {
                AiPerformanceData dummyAi = new AiPerformanceData();
                moveDto = MoveMapper.ToDto(lastMoveData, currentGameId, moveCount + 1, dummyAi);
                // Override PlayerId ให้มั่นใจ
                int myUserId = (PerformanceTracker.Instance != null) ? PerformanceTracker.Instance.UserId : 0;
            }
            else
            {
                Debug.LogError($"❌ History Mismatch! History: {lastMoveData.endPosition}, Sending: {to}");
            }
        }

        if (moveDto == null)
        {
            Debug.LogError("❌ Failed to get move from HistoryMove! Fallback to manual creation (Flags will be missing).");
            // Fallback (ถ้าจำเป็น แต่จริงๆ ควร Error เพื่อให้รู้ตัว)
            moveDto = new MoveCreateDto
            {
                GameId = currentGameId,
                MoveNumber = moveCount + 1,
                StartX = from.x,
                StartY = from.y,
                EndX = to.x,
                EndY = to.y,
                PieceType = (int)pieceType,
                PlayerTurn = (currentTurn == Team.White) ? 0 : 1
            };
        }
        // 4. ยิง API
        StartCoroutine(movesApi.SendMove(moveDto, (success) =>
        {
            if (success)
            {
                Debug.Log("✅ Send Move Success!");

                // ✅ อัปเดต State ฝั่งตัวเองทันทีเมื่อ Server รับรู้แล้ว
                lastAppliedMoveNumber = moveDto.MoveNumber;
                moveCount = moveDto.MoveNumber;

                // ✅ สลับเทิร์นไปเป็นของคู่แข่ง
                SwitchTurn();

                // ✅ เริ่ม Polling รอหมากจากคู่แข่ง
                StartPolling();
            }
            else
            {
                Debug.LogError("⚠️ Send Move Failed!");
                // TODO: อาจจะต้องมี Rollback หรือแจ้งเตือนผู้เล่น
            }
        }));
    }
    // ✅ ฟังก์ชันสลับเทิร์น (อันเดียวจบ)
    public void SwitchTurn()
    {
        if (IsGameOver() || gameIsOver)
        {
            Debug.Log("🏁 เกมจบแล้ว ไม่สามารถสลับเทิร์นได้");
            return;
        }
        // สลับสี
        currentTurn = (currentTurn == Team.White) ? Team.Black : Team.White;
        UpdatePlayerTurnUI();

        // 🟢 ถ้าเป็น Online Mode
        // 2. จัดการโหมด Online
        if (currentMode == GameModes.Online)
        {
            if (IsMyTurn())
            {
                // 👉 ถึงตาเรา: หยุดรอ Server แล้วยอมให้เราจับหมาก
                Debug.Log("🟢 ถึงตาคุณแล้ว! (Stop Polling)");
                isWaitingForOpponent = false;
                if (onlinePollingCoroutine != null) StopCoroutine(onlinePollingCoroutine);

            }
            else
            {
                // 👉 ตาคนอื่น: เริ่มรอ Server (Polling)
                Debug.Log("⏳ จบตาคุณ -> รอคู่แข่งเดิน (Start Polling)");
                isWaitingForOpponent = true;
                if (onlinePollingCoroutine != null) StopCoroutine(onlinePollingCoroutine);
                onlinePollingCoroutine = StartCoroutine(PollOpponentMove());
            }
        }
    }

    public string GetCurrentPlayerName()
    {
        return (currentTurn == ChessPiece.Team.White) ? whitePlayerName : blackPlayerName;
    }

    public GameModes GetCurrentMode()
    {
        return currentMode;
    }
    /*Set*/
    public void SetCurrentTurn(Team team)
    {
        currentTurn = team;
    }
    // อัปเดต UI เมื่อเปลี่ยนตาเดิน
    public void UpdatePlayerTurnUI()
    {
        if (HistoryMoveUI.Instance != null)
        {
            HistoryMoveUI.Instance.UpdatePlayerTurn();
            HistoryMoveUI.Instance.UpdateMoveHistoryList();
        }
        else
        {
            Debug.LogWarning("HistoryMoveUI not found!");
        }
    }

    // ฟังก์ชันตรวจสอบ Checkmate หรือ Stalemate
    public void CheckGameState()
    {

        ChessPiece.Team opponentTeam = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
        if (ChessBoard.Instance.IsKingInCheckmate(currentTurn))
        {
            ChessPiece.Team winningTeam = GetOpponentTeam(currentTurn);
            Debug.Log($"♟️ Checkmate! {winningTeam} win!");
            GameOver(winningTeam, "checkmate");

            return;
        }
        else if (ChessBoard.Instance.IsStalemate(currentTurn))
        {
            Debug.Log("⚖️ Stalemate! draw!");
            GameOver(ChessPiece.Team.None, "stalemate");
            return;
        }
    }

    public void GameOver(ChessPiece.Team winningTeam, string endReason)
    {
        if (gameIsOver) return;
        gameIsOver = true;

        // ปิดทุก UI ก่อน
        winGamePanel.SetActive(false);
        loseGamePanel.SetActive(false);
        drawGamePanel.SetActive(false);
        drawInfoPanel.SetActive(false); // แสดง counter ก็ปิด

        Debug.Log($"🎉 เกมจบแล้ว! {(winningTeam == Team.None ? "เสมอ" : $"{winningTeam} ชนะ")}!");


        Debug.Log("gameIsOver: " + gameIsOver + "\n" + "currentGameId: " + currentGameId +

         "\n" + "currentMode: " + currentMode + "\n" + "currentTurn: " + currentTurn
         + "\n" + "moveCount: " + moveCount + "\n" + "endReason: " + endReason);

        if (winningTeam == Team.None)
        {
            drawGamePanel.SetActive(true);
            drawTxt.text = "game draw!";
        }
        else
        {
            bool isLocalPlayerWinner =
                (winningTeam == Team.White && WhitePlayer == PlayerType.Human) ||
                (winningTeam == Team.Black && BlackPlayer == PlayerType.Human);
            if (isLocalPlayerWinner)
            {
                winGamePanel.SetActive(true);
                winTxt.text = $"{GetCurrentPlayerName()} ชนะ!";

            }
            else
            {
                loseGamePanel.SetActive(true);
                loseTxt.text = $"{GetCurrentPlayerName()} แพ้!";
            }
        }

        if (currentMode == GameModes.LocalMultiplayer || currentGameId <= 0)
        {
            Debug.Log("Game Over (Local/No ID): No data sync required.");
            return;
        }


        syncCoroutine = StartCoroutine(SyncAndEndGame(winningTeam, endReason));
    }

    public void ResetGame()
    {
        // --- 1. รีเซ็ตสถานะเกมพื้นฐาน ---
        gameIsOver = false;
        isGameStarted = false;
        isAITurnActive = false;
        currentTurn = Team.White;

        // --- 2. 🚨 รีเซ็ตตัวแปรนับรอบและข้อมูล Backend (สำคัญมาก) ---
        moveCount = 0;              // ต้องรีเซ็ตเพื่อให้ MoveNumber เริ่มที่ 1 ใหม่
        currentGameId = -1;         // ตัด Game ID เก่าทิ้ง

        if (recordedMoves != null)
        {
            recordedMoves.Clear();  // ล้าง List ที่จะส่ง API
        }

        // --- 3. หยุด Coroutine ถ้ามี (ป้องกัน AI เดินซ้อน) ---
        if (aiLoop != null) StopCoroutine(aiLoop);
        if (fiftyMoveCoroutine != null) StopCoroutine(fiftyMoveCoroutine);
        if (syncCoroutine != null) StopCoroutine(syncCoroutine);

        // --- 4. จัดการ UI ---
        if (winGamePanel) winGamePanel.SetActive(false);
        if (loseGamePanel) loseGamePanel.SetActive(false);
        if (drawGamePanel) drawGamePanel.SetActive(false);
        if (drawInfoPanel) drawInfoPanel.SetActive(false);

        Debug.Log(historyMoveUI);

        // --- 5. รีเซ็ตกระดานและตัวหมาก ---
        if (chessBoard != null)
        {
            chessBoard.ResetBoard();
        }

        if (historyMoveUI != null)
        {
            historyMoveUI.ClearHistory();
        }

        Debug.Log("♟️ เกมถูกรีเซ็ตสมบูรณ์!");

        // --- 6. อัปเดต UI ให้กลับเป็นค่าเริ่มต้น ---
        UpdatePlayerTurnUI();
        ResetFiftyMoveUI();
    }

    public void ReturnMain()
    {
        StartCoroutine(ReturnMainSequence());
    }

    public void UpdateFiftyMoveCounter(int count)
    {
        if (drawInfoPanel == null || fiftyMoveText == null) return;
        if (count >= 30)
        {
            drawInfoPanel.SetActive(true);
            fiftyMoveText.text = $"📏 กฎ 50 เดิน: {count}/50";
            if (count >= 48)
                fiftyMoveText.color = Color.red;
            else if (count >= 45)
                fiftyMoveText.color = new Color(1f, 0.5f, 0f);
            else
                fiftyMoveText.color = Color.white;
            if (fiftyMoveCoroutine != null) StopCoroutine(fiftyMoveCoroutine);
            fiftyMoveCoroutine = StartCoroutine(AutoHideFiftyMovePanel(0.5f));
        }
        else
        {
            drawInfoPanel.SetActive(false);
        }
    }

    public void RecordMove(HistoryMove.HistoryMoveData moveData, AiPerformanceData aiStats = null)
    {
        moveCount++;

        // ถ้ามี GameID (ออนไลน์อยู่) ให้เก็บเข้า List รอส่ง
        if (currentGameId != -1)
        {
            MoveCreateDto dto = MoveMapper.ToDto(moveData, currentGameId, moveCount, aiStats);
            recordedMoves.Add(dto);

            recordedMoves.Add(dto);
            Debug.Log($"📝 Recorded Move Locally: {dto.MoveNumber}");
        }
        // 2. (🚨 ส่วนที่ขาดหายไป) ส่งข้อมูลเข้า PerformanceTracker เพื่อหาค่าเฉลี่ย
        if (aiStats != null && PerformanceTracker.Instance != null)
        {
            // ต้องมั่นใจว่าใน PerformanceTracker มีฟังก์ชันชื่อประมาณนี้
            PerformanceTracker.Instance.AddMove(aiStats.Depth, aiStats.Nodes, aiStats.MoveTimeMs, aiStats.Score);
            Debug.Log($"[PerformanceTracker] Added Data -> Depth: {aiStats.Depth}, Nodes: {aiStats.Nodes}");
        }
    }

    public void ResetFiftyMoveUI()
    {
        if (fiftyMoveCoroutine != null)
        {
            StopCoroutine(fiftyMoveCoroutine);
            fiftyMoveCoroutine = null;
        }
        if (drawInfoPanel != null)
        {
            drawInfoPanel.SetActive(false);
        }
    }

    public void ModeSelect()
    {
        ResetGameData();

        string modeStr = PlayerPrefs.GetString("Mode", "SinglePlayer");

        GameCreateDto createDto = new GameCreateDto();
        createDto.GameType = modeStr.ToLower();

        switch (modeStr)
        {
            case "AIVsAI":
                SetupAIVsAIMode();
                break;

            case "LocalMultiplayer":
                SetupLocalMultiplayerMode();
                break;

            case "SinglePlayer":
                SetupSinglePlayerMode();
                break;

            case "OnlineMultiplayer":
                SetupOnlineMultiplayerMode();
                break;
            default:
                SetupSinglePlayerMode();
                break;
        }

        if (modeStr == "LocalMultiplayer")
        {
            currentGameId = -1; // Flag บอกว่าเป็น Offline
            isGameStarted = true;
            currentTurn = Team.White;
            UpdatePlayerTurnUI();
            Debug.Log("🎮 Local Multiplayer Started (Offline Mode)");
            return; //  ออกจากฟังก์ชันทันที ไม่ไปเรียก API ด้านล่าง
        }


        if (modeStr == "OnlineMultiplayer")
        {
            // SetupOnlineMultiplayerMode() ถูกเรียกไปแล้วใน Switch Case ข้างบน
            // ตรงนี้แค่รับประกัน state และ return ไม่ให้สร้างเกมซ้ำ
            isGameStarted = true;
            currentTurn = Team.White;

            UpdatePlayerTurnUI();
            return;

        }

        int myUserId = -1;
        if (PerformanceTracker.Instance != null)
        {
            myUserId = PerformanceTracker.Instance.UserId;
        }

        createDto.GameType = modeStr switch
        {
            "SinglePlayer" => "single_player",
            "AIVsAI" => "ai_vs_ai",
            "LocalMultiplayer" => "local_multiplayer",
            _ => "single_player"
        };

        createDto.WhitePlayerId = (WhitePlayer == PlayerType.Human) ? myUserId : -1;
        createDto.BlackPlayerId = (BlackPlayer == PlayerType.Human) ? myUserId : -1;
        createDto.WhitePlayerType = GetPlayerTypeString(WhitePlayer, aiDifficultyWhite);
        createDto.BlackPlayerType = GetPlayerTypeString(BlackPlayer, aiDifficultyBlack);

        // 3. ส่ง API
        if (gameAPI != null)
        {
            StartCoroutine(gameAPI.CreateGame(createDto, (id) =>
            {
                currentGameId = id;
                if (PerformanceTracker.Instance != null)
                {
                    PerformanceTracker.Instance.GameId = id;
                }
                Debug.Log($"✅ Game Created! ID: {currentGameId}");
            }));
        }

        isGameStarted = true;
        currentTurn = Team.White;
        UpdatePlayerTurnUI();
    }

    public void DoResetAndLeave()
    {
        Debug.Log("👋 Loading MainMenu Scene...");
        if (gameIsOver) ResetGame(); // Reset กระดาน

        // ปิด UI
        if (winGamePanel) winGamePanel.SetActive(false);
        if (loseGamePanel) loseGamePanel.SetActive(false);
        if (drawGamePanel) drawGamePanel.SetActive(false);
        if (drawInfoPanel) drawInfoPanel.SetActive(false);

        // รีเซ็ตตัวแปร
        gameIsOver = false;
        isGameStarted = false;
        isAITurnActive = false;
        currentTurn = Team.White;
        currentGameId = -1;

        // กลับหน้าหลัก
        // ✅ อัปเดตสถานะกลับเป็น "online"
        int userId = PlayerPrefs.GetInt("UserId", 0);
        if (userApi != null)
        {
            StartCoroutine(userApi.UpdateStatus(userId, "online", (success, message) =>
            {
                Debug.Log(success ? "✅ Status updated to online" : $"⚠️ Failed to update status to online: {message}");
            }));
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    // ✅ เพิ่มฟังก์ชันนี้ใน GameManager.cs
    public void OnExitGameClicked()
    {
        if (gameIsOver)
        {
            DoResetAndLeave();
            return;
        }

        Debug.Log("⏳ Sending Abandon request...");
        int playerId = (PerformanceTracker.Instance != null) ? PerformanceTracker.Instance.UserId : -1;

        if (gameAPI != null && currentGameId > 0)
        {
            // ✅ เปลี่ยนเป็น "abandoned" เพื่อให้ Backend รู้ว่ากดออกเกม
            StartCoroutine(gameAPI.ResignGame(currentGameId, playerId, "abandoned", (success) =>
            {
                Debug.Log(success ? "✅ Abandon Success" : "❌ Request Failed");
                DoResetAndLeave();
            }));
        }
        else
        {
            DoResetAndLeave();
        }
    }

    public void OnGiveUpClicked()
    {
        if (gameIsOver)
        {
            DoResetAndLeave();
            return;
        }

        Debug.Log("🏳️ Player clicked Give Up (Resign)...");
        int playerId = (PerformanceTracker.Instance != null) ? PerformanceTracker.Instance.UserId : -1;

        if (gameAPI != null && currentGameId > 0)
        {
            // ส่งเหตุผลเป็น "resignation" อย่างเป็นทางการ
            StartCoroutine(gameAPI.ResignGame(currentGameId, playerId, "resignation", (success) =>
            {
                Debug.Log(success ? "✅ Resign Success" : "❌ Resign Failed");
                DoResetAndLeave();
            }));
        }
        else
        {
            // Offline Mode -> แค่จบเกม
            GameOver(GetOpponentTeam(currentTurn), "resignation");
        }
    }

    public void OnRestartClick()
    {
        ResetGame(); // ล้างของเก่า
        StartGameSession();
    }

    public bool IsGameOver() => gameIsOver;
}