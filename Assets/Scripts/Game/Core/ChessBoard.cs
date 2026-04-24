using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static ChessPiece;

public class ChessBoard : MonoBehaviour
{
    public const int BoardSize = 8;
    public float tileSize = 1.0f;
    public Transform pieceWhite;
    public Transform pieceBlack;
    public GameObject piecePrefab;
    public Sprite[] whiteSprites;
    public Sprite[] blackSprites;

    private int _movesWithoutCaptureOrPawn = 0;
    private const int MAX_MOVES_WITHOUT_PROGRESS = 50;
    private GameManager gameManager;
    private HistoryMove historyMove;
    private GameSyncService gameSyncService;
    private ChessPiece[,] board = new ChessPiece[BoardSize, BoardSize];
    private ChessPiece selectedPiece = null; // ตัวแปรเก็บหมากที่ถูกเลือก
    private bool whiteCanCastleKingSide = true;
    private bool whiteCanCastleQueenSide = true;
    private bool blackCanCastleKingSide = true;
    private bool blackCanCastleQueenSide = true;
    private bool isPromoting = false; // ✅ ตัวแปรเช็คว่ากำลังเลื่อนขั้นหรือไม่
    private Vector2Int? enPassantTarget = null; // ตำแหน่งเบี้ยที่เดินสองช่องในตาแรก
    private Dictionary<Vector2Int, ChessPiece> piecesOnBoard = new Dictionary<Vector2Int, ChessPiece>();
    private ChessBoardModel boardModel;

    public IReadOnlyDictionary<Vector2Int, ChessPiece> PiecesOnBoard => piecesOnBoard;
    public int FiftyMoveCounter => _movesWithoutCaptureOrPawn;
    public GameManager GameManager => gameManager;

    // ✅ Expose Castling Rights for AI/Network Sync
    public bool WhiteCanCastleKingSide => whiteCanCastleKingSide;
    public bool WhiteCanCastleQueenSide => whiteCanCastleQueenSide;
    public bool BlackCanCastleKingSide => blackCanCastleKingSide;
    public bool BlackCanCastleQueenSide => blackCanCastleQueenSide;

    public ChessBoardModel BoardModel { get; set; }
    public static ChessBoard Instance { get; private set; }

    public ChessPiece selectedPawn; // เบี้ยที่รอเลื่อนขั้น
    public PromotionManager promotionManager; // เชื่อมกับ PromotionManager ใน Inspector
    public ChessPiece SelectedPiece => selectedPiece; // เพิ่ม Property เพื่อเข้าถึง selectedPiece
    public bool IsWhiteTurn { get; internal set; }

    public ChessPiece.PieceType promotionFrom;
    public Vector2Int promotionPosition;
    public Vector2Int position;  // ตัวแปรสำหรับเก็บตำแหน่งของหมาก
    //  Event สำหรับแจ้งว่ามีการเดินหมากเกิดขึ้น (แยก Logic ออกจาก GameManager)
    public event Action<MoveResult> OnMoveCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            historyMove = FindObjectOfType<HistoryMove>();
            gameSyncService = FindFirstObjectByType<GameSyncService>();
            boardModel = new ChessBoardModel();
            BoardModel = boardModel;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void InitializeBoard()
    {
        ChessBoardGenerate generator = GetComponent<ChessBoardGenerate>();
        if (generator == null)
        {
            Debug.LogWarning("ChessBoardGenerate not found. Board will not be generated.");
            return;
        }

        generator.InitializeBoard();
    }
    /* class methone private*/
    private bool ValidatePreMoveConditions(Vector2Int newPosition)
    {
        if (selectedPiece == null)
        {
            Debug.Log("❌ ไม่มีหมากที่ถูกเลือก");
            return false;
        }

        if (isPromoting)
        {
            Debug.Log("⏳ กำลังอยู่ในช่วงเลือกเลื่อนขั้น ไม่สามารถเดินหมากอื่นได้!");
            return false;
        }

        if (newPosition == selectedPiece.boardPosition)
        {
            Debug.Log("❌ ไม่สามารถเดินซ้ำตำแหน่งเดิมได้");
            return false;
        }

        return true;
    }

    private bool HandleCastling(Vector2Int newPosition)
    {
        if (selectedPiece.pieceType != ChessPiece.PieceType.King) return false;

        int row = (selectedPiece.team == ChessPiece.Team.White) ? 0 : 7;

        if (newPosition == new Vector2Int(6, row) && CanCastle(true, selectedPiece.team))
        {
            PerformCastling(true, selectedPiece.team);
            return true;
        }
        else if (newPosition == new Vector2Int(2, row) && CanCastle(false, selectedPiece.team))
        {
            PerformCastling(false, selectedPiece.team);
            return true;
        }
        return false;
    }

    private bool HandleEnPassant(Vector2Int newPosition)
    {
        if (selectedPiece.pieceType != ChessPiece.PieceType.Pawn) return false;

        Vector2Int oldPosition = selectedPiece.boardPosition;
        bool isSideStep = Mathf.Abs(newPosition.x - oldPosition.x) == 1;

        if (isSideStep && IsEnPassantTarget(newPosition))
        {
            Vector2Int enemyPawnPos = new Vector2Int(newPosition.x, oldPosition.y);

            if (piecesOnBoard.TryGetValue(enemyPawnPos, out ChessPiece enemyPawn))
            {
                Debug.Log($"⚔️ กิน {enemyPawn.team} {enemyPawn.pieceType} แบบ En Passant!");

                UnityEngine.Object.Destroy(enemyPawn.gameObject);
                piecesOnBoard.Remove(enemyPawnPos);

                piecesOnBoard.Remove(oldPosition);
                selectedPiece.SetPosition(newPosition.x, newPosition.y);
                piecesOnBoard[newPosition] = selectedPiece;

                SaveMoveToHistory(selectedPiece, oldPosition, newPosition, null);

                enPassantTarget = null;

                selectedPiece = null;

                MoveResult result = new MoveResult(oldPosition, newPosition, ChessPiece.PieceType.Pawn);
                result.IsEnPassant = true;
                result.CapturedType = enemyPawn.pieceType;
                result.CapturedTeam = enemyPawn.team;

                OnMoveCompleted?.Invoke(result);

                return true;
            }
        }

        return false;
    }

    private void UndoSimulatedMove(Vector2Int originalPos, Vector2Int newPos, ChessPiece captured)
    {
        piecesOnBoard.Remove(newPos);
        selectedPiece.boardPosition = originalPos;
        piecesOnBoard[originalPos] = selectedPiece;

        if (captured != null)
            piecesOnBoard[newPos] = captured;
    }

    private void FinalizeMove(Vector2Int originalPos, Vector2Int newPos, ChessPiece captured)
    {
        if (selectedPiece == null)
        {
            Debug.LogError("❌ FinalizeMove Called but selectedPiece is NULL!");
            return;
        }

        ChessPiece movingPiece = selectedPiece; // ✅ Capture reference locally

        if (captured != null)
        {
            if (captured == movingPiece)
            {
                Debug.LogError("❌ Attempting to capture self! Aborting destroy.");
            }
            else
            {
                Debug.Log($"⚔️ {movingPiece.team} {movingPiece.pieceType} กิน {captured.team} {captured.pieceType}!");
                Destroy(captured.gameObject);
            }
        }

        movingPiece.MoveTo(newPos);
        piecesOnBoard[newPos] = movingPiece;

        if (movingPiece != null && !movingPiece.HasMoved) // ✅ Null check added
            movingPiece.HasMoved = true;

        SetEnPassantTarget(originalPos, newPos, movingPiece);
    }

    private void FinalizeMoveDirect(ChessPiece piece, Vector2Int from, Vector2Int to, ChessPiece captured)
    {
        piecesOnBoard.Remove(from);

        if (captured != null)
        {
            Destroy(captured.gameObject);
            piecesOnBoard.Remove(to);
        }

        piece.MoveTo(to);
        piecesOnBoard[to] = piece;

        if (!piece.HasMoved)
            piece.HasMoved = true;

        // En Passant target (ต้อง set จาก piece)
        if (piece.pieceType == PieceType.Pawn && Mathf.Abs(to.y - from.y) == 2)
        {
            enPassantTarget = new Vector2Int(to.x, (to.y + from.y) / 2);
        }
        else
        {
            enPassantTarget = null;
        }
    }

    private void SetEnPassantTarget(Vector2Int from, Vector2Int to, ChessPiece movingPiece)
    {
        if (movingPiece != null && movingPiece.pieceType == ChessPiece.PieceType.Pawn && Mathf.Abs(to.y - from.y) == 2)
        {
            Vector2Int middle = new Vector2Int(to.x, (to.y + from.y) / 2);
            if (!piecesOnBoard.ContainsKey(middle))
            {
                enPassantTarget = middle;
                Debug.Log($"🎯 ตั้งค่า En Passant Target: {enPassantTarget}");
            }
        }
        else
        {
            enPassantTarget = null;
        }
    }

    private void HandleCheckState()
    {
        var opponent = (gameManager.GetCurrentTurn() == ChessPiece.Team.White)
            ? ChessPiece.Team.Black
            : ChessPiece.Team.White;

        if (IsKingInCheck(opponent))
        {
            Debug.Log($"⚠️ Check! {opponent} กำลังถูกโจมตี!");
        }
    }


    private void SaveMoveToHistory(ChessPiece movingPiece, Vector2Int from, Vector2Int to, ChessPiece captured, ChessPiece.PieceType? promotedPieceType = null, AiPerformanceData aiStats = null, ChessPiece.PieceType? originalPieceType = null)
    {
        if (historyMove == null) return;
        if (gameSyncService == null)
            gameSyncService = FindFirstObjectByType<GameSyncService>();

        int score = (aiStats != null) ? (int)aiStats.Score : 0;
        int depth = (aiStats != null) ? aiStats.Depth : 0;
        int nodes = (aiStats != null) ? aiStats.Nodes : 0;
        int moveTimeMs = (aiStats != null) ? aiStats.MoveTimeMs : 0;
        string algorithmType = (aiStats != null) ? aiStats.AlgorithmType : null;
        ChessPiece.PieceType historyPieceType = originalPieceType ?? movingPiece.pieceType;

        // ใช้ movingPiece แทน selectedPiece
        bool isCastling = historyPieceType == ChessPiece.PieceType.King && Mathf.Abs(to.x - from.x) == 2;
        bool isEnPassant = historyPieceType == ChessPiece.PieceType.Pawn && IsEnPassantTarget(to);
        bool isPawnTwoStep = historyPieceType == ChessPiece.PieceType.Pawn && Mathf.Abs(to.y - from.y) == 2;
        bool pieceHasMovedBefore = movingPiece.HasMoved;
        bool isCheck = IsKingInCheck(movingPiece.team == ChessPiece.Team.White ? ChessPiece.Team.Black : ChessPiece.Team.White);

        // Promotion logic
        bool isPromotion = historyPieceType == ChessPiece.PieceType.Pawn && (to.y == 0 || to.y == 7);
        ChessPiece.PieceType promotedTo = ChessPiece.PieceType.None;
        ChessPiece.PieceType promotedFrom = ChessPiece.PieceType.None;
        Vector2Int promotionPosition = Vector2Int.zero;

        if (isPromotion)
        {
            // ถ้าส่ง promotedPieceType มา (Network/AI) ให้ใช้เลย
            // ถ้าไม่ส่งมา (Local Human) ให้ดึงจาก PromotionManager
            promotedTo = promotedPieceType
                ?? (movingPiece.pieceType != ChessPiece.PieceType.Pawn ? movingPiece.pieceType : PromotionManager.Instance.GetSelectedPromotionType());
            promotedFrom = ChessPiece.PieceType.Pawn;
            promotionPosition = to;
        }

        // ตรวจสอบการกินหมาก
        bool isCapture = captured != null || isEnPassant;
        Vector2Int capturedPos = isEnPassant ? new Vector2Int(to.x, from.y) : to;
        Vector2Int? prevEnPassant = enPassantTarget;
        ChessPiece.PieceType capturedType = ChessPiece.PieceType.None;
        ChessPiece.Team capturedTeam = ChessPiece.Team.None;

        if (isCapture)
        {
            if (isEnPassant)
            {
                capturedType = ChessPiece.PieceType.Pawn;
                capturedTeam = OpponentTeam(movingPiece.team);
            }
            else if (captured != null)
            {
                capturedType = captured.pieceType;
                capturedTeam = captured.team;
            }
        }

        // บันทึกข้อมูลการเดิน
        historyMove.AddMove(
            from,
            to,
            historyPieceType,
            capturedType,
            capturedTeam,
            isCastling,
            isEnPassant,
            isCheck,
            isPawnTwoStep,
            pieceHasMovedBefore,
            isCapture,
            promotedTo,
            capturedPos,
            promotedFrom,
            promotionPosition,
            previousEnPassantTarget: prevEnPassant,
            movingPiece.team,
            _movesWithoutCaptureOrPawn,
            score,
            depth,
            nodes,
            moveTimeMs,
            algorithmType
        );

        if (gameSyncService != null)
        {
            var historyStack = historyMove.GetMoveHistory();
            if (historyStack != null && historyStack.Count > 0)
            {
                var latestMove = historyStack.Peek();
                Debug.Log(
                    $"[ChessBoard] Forwarding move to GameSyncService -> Team: {latestMove.team}, " +
                    $"Move: {latestMove.startPosition} -> {latestMove.endPosition}, " +
                    $"HasAIStats: {aiStats != null}, Algo: {(aiStats != null ? aiStats.AlgorithmType : "none")}, " +
                    $"Color: {(aiStats != null ? aiStats.AiColor : "none")}"
                );
                gameSyncService.RecordMove(latestMove, aiStats);
            }
            else
            {
                Debug.LogWarning("[ChessBoard] SaveMoveToHistory could not forward move because history stack is empty.");
            }
        }
        else
        {
            Debug.LogWarning("[ChessBoard] SaveMoveToHistory could not find GameSyncService.");
        }

        HistoryMoveUI.Instance?.UpdateMoveHistoryList();
    }

    private ChessPiece.Team OpponentTeam(ChessPiece.Team team)
    {
        return team == ChessPiece.Team.White ? ChessPiece.Team.Black : ChessPiece.Team.White;
    }

    private void UpdateFiftyMoveRuleCounter(bool wasCapture)
    {
        if (wasCapture || selectedPiece.pieceType == PieceType.Pawn)
        {
            _movesWithoutCaptureOrPawn = 0;
            GameManager.Instance.ResetFiftyMoveUI();

        }
        else
        {
            _movesWithoutCaptureOrPawn++;
            GameManager.Instance.UpdateFiftyMoveCounter(_movesWithoutCaptureOrPawn);

        }
        if (_movesWithoutCaptureOrPawn >= MAX_MOVES_WITHOUT_PROGRESS)
        {
            Debug.Log("เสมอ! 50 การเดินโดยไม่มีการยึดหรือเดินเบี้ย");
            GameManager.Instance.GameOver(ChessPiece.Team.None, "fifty_move_rule");
        }

    }

    private Vector2Int AlgebraicToVector(string pos)
    {
        int x = pos[0] - 'a';
        int y = int.Parse(pos[1].ToString()) - 1;
        return new Vector2Int(x, y);
    }

    // Set method
    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    public void SetEnPassantTarget(Vector2Int? target)
    {
        enPassantTarget = target;
        Debug.Log($" คืนค่า En Passant Target: {target}");

    }

    public void SetPromotionData(Vector2Int position, ChessPiece.PieceType from)
    {
        promotionPosition = position;
        promotionFrom = from;
    }

    public void SetPromoting(bool value)
    {
        isPromoting = value;
    }

    public void SetselectedPiece(ChessPiece piece)
    {
        selectedPiece = piece;
    }

    public void SetCanCastleKingSide(ChessPiece.Team team, bool value)
    {
        if (team == ChessPiece.Team.White)
            whiteCanCastleKingSide = value;
        else if (team == ChessPiece.Team.Black)
            blackCanCastleKingSide = value;
    }

    public void SetCanCastleQueenSide(ChessPiece.Team team, bool value)
    {
        if (team == ChessPiece.Team.White)
            whiteCanCastleQueenSide = value;
        else if (team == ChessPiece.Team.Black)
            blackCanCastleQueenSide = value;
    }

    public void SetFiftyMoveCounter(int count)
    {
        _movesWithoutCaptureOrPawn = count;
    }

    //get method
    public Dictionary<Vector2Int, ChessPiece> GetPiecesOnBoard()
    {
        return piecesOnBoard;
    }

    public Vector2Int? GetEnPassantTarget()
    {
        return enPassantTarget;
    }

    public GameManager GetGameManager()
    {
        return gameManager;
    }
    /* class methone public*/

    public ChessPiece SimulateMove(Vector2Int newPosition)
    {
        Vector2Int originalPosition = selectedPiece.boardPosition;
        ChessPiece captured = null;

        piecesOnBoard.Remove(originalPosition);
        if (piecesOnBoard.TryGetValue(newPosition, out captured))
            piecesOnBoard.Remove(newPosition);

        selectedPiece.boardPosition = newPosition;
        piecesOnBoard[newPosition] = selectedPiece;


        return captured;
    }

    public bool IsTileEmpty(Vector2Int position)
    {
        return !piecesOnBoard.ContainsKey(position);
    }

    public bool IsEnemyAtPosition(Vector2Int position, ChessPiece.Team team)
    {
        if (piecesOnBoard.TryGetValue(position, out ChessPiece piece))
        {
            return piece.team != team;
        }
        return false;
    }

    public bool IsOccupiedByTeam(Vector2Int position, ChessPiece.Team team)
    {
        if (piecesOnBoard.TryGetValue(position, out ChessPiece piece))
            return piece.team == team;
        return false;
    }

    public bool IsPathClear(Vector2Int start, Vector2Int end, ChessPiece.PieceType pieceType)
    {
        int dx = end.x - start.x;
        int dy = end.y - start.y;

        int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        Vector2Int current = start + new Vector2Int(stepX, stepY);
        while (current != end)
        {
            if (piecesOnBoard.ContainsKey(current))
                return false;
            current += new Vector2Int(stepX, stepY);
        }

        return true;
    }

    public bool IsPositionOnBoard(Vector2Int position)
    {
        return position.x >= 0 && position.x < BoardSize && position.y >= 0 && position.y < BoardSize;
    }

    public bool IsPositionUnderAttack(Vector2Int position, ChessPiece.Team team)
    {
        foreach (var entry in piecesOnBoard)
        {
            ChessPiece piece = entry.Value;

            // ถ้าเป็นหมากของศัตรู
            if (piece.team != team)
            {
                // ใช้ CanAttack แทน IsValidMove เพื่อป้องกัน Infinite Recursion 
                // และเพื่อให้ Pinned Piece ยังสามารถ Check King ได้ตามกฎ
                if (piece.CanAttack(position))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsKingInCheck(ChessPiece.Team team)
    {
        Vector2Int kingPos = FindKingPosition(team);
        if (kingPos == -Vector2Int.one) return false; // ถ้าไม่เจอ King ถือว่าไม่ check

        // ใช้ IsPositionUnderAttack ก็ได้ แต่ต้องระวังทีม (IsPositionUnderAttack เช็คว่าทีม 'team' โดนโจมตีไหม)
        // IsPositionUnderAttack(kingPos, team) จะวน loop enemy และเรียก CanAttack(kingPos)
        return IsPositionUnderAttack(kingPos, team);
    }

    public Vector2Int FindKingPosition(ChessPiece.Team team)
    {
        foreach (var entry in piecesOnBoard.ToList()) // ใช้ ToList() ป้องกัน collection modified
        {
            ChessPiece piece = entry.Value;
            if (piece != null && piece.pieceType == ChessPiece.PieceType.King && piece.team == team)
            {
                return piece.boardPosition;
            }
        }

        Debug.LogWarning($"⚠️ ไม่พบคิงของทีม {team} ใน state ปัจจุบัน");
        return -Vector2Int.one; // return invalid position แทน throw
    }

    public bool IsKingInCheckmate(ChessPiece.Team team)
    {
        if (!IsKingInCheck(team))
            return false;

        foreach (var entry in piecesOnBoard.ToList())
        {
            ChessPiece piece = entry.Value;
            if (piece == null || piece.team != team) continue;

            if (piece.GetValidMoves().Count > 0)
                return false; // มี legal move อย่างน้อยหนึ่งตา
        }
        return true;
    }

    public ChessPiece SpawnPiece(ChessPiece.PieceType type, ChessPiece.Team team, Vector2Int position)
    {
        if (piecePrefab == null)
        {
            Debug.LogError(" piecePrefab is null! กรุณาเซ็ตใน Inspector หรือระหว่าง Unit Test");
            return null;
        }
        if (piecesOnBoard.TryGetValue(position, out ChessPiece oldPiece))
        {
            Debug.Log($" ลบหมากเดิมที่ {position} ก่อนสร้างใหม่");
            UnityEngine.Object.Destroy(oldPiece.gameObject);
            piecesOnBoard.Remove(position);
        }

        GameObject pieceObj = Instantiate(piecePrefab, new Vector3(position.x * tileSize, position.y * tileSize, 0), Quaternion.identity);
        ChessPiece piece = pieceObj.GetComponent<ChessPiece>();
        piece.pieceType = type;
        piece.team = team;
        piece.boardPosition = position;
        piece.SetBoardManager(this);
        pieceObj.name = $"{team}_{type}";

        // กำหนด Sprite ตามประเภทของหมาก
        SpriteRenderer renderer = pieceObj.GetComponent<SpriteRenderer>();
        renderer.sprite = team == ChessPiece.Team.White ? whiteSprites[(int)type] : blackSprites[(int)type];

        // เพิ่ม BoxCollider2D ให้กับตัวหมาก
        BoxCollider2D boxCollider = pieceObj.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;  // ทำให้ Collider เป็น Trigger

        // จัดกลุ่มหมากแต่ละทีม
        pieceObj.transform.SetParent(team == ChessPiece.Team.White ? pieceWhite : pieceBlack);

        boxCollider.isTrigger = true;
        piecesOnBoard[position] = piece; // เพิ่มลงใน Dictionary
        return piece; //  คืนค่า ChessPiece
    }

    public bool IsStalemate(ChessPiece.Team team)
    {
        if (IsKingInCheck(team))
            return false;

        foreach (var entry in piecesOnBoard.ToList())
        {
            ChessPiece piece = entry.Value;
            if (piece == null || piece.team != team) continue;

            if (piece.GetValidMoves().Count > 0)
                return false; // มี legal move อย่างน้อยหนึ่งตา
        }
        return true;
    }

    public bool DoesMoveExposeKing(ChessPiece piece, Vector2Int target)
    {
        var backup = new Dictionary<Vector2Int, ChessPiece>(piecesOnBoard);

        Vector2Int originalPosition = piece.boardPosition;
        ChessPiece capturedPiece = null;

        try
        {
            //  จำลอง move
            piecesOnBoard.Remove(originalPosition);
            if (piecesOnBoard.TryGetValue(target, out capturedPiece))
            {
                piecesOnBoard.Remove(target);
            }

            piecesOnBoard[target] = piece;
            piece.boardPosition = target;

            //  ตรวจ check
            return IsKingInCheck(piece.team);
        }
        finally
        {
            //  rollback state กลับคืน (ไม่ให้ King หาย)
            piecesOnBoard.Clear();
            foreach (var kv in backup)
            {
                piecesOnBoard[kv.Key] = kv.Value;
            }

            piece.boardPosition = originalPosition;
        }
    }

    public bool IsEnPassantTarget(Vector2Int position)
    {
        return enPassantTarget.HasValue && enPassantTarget.Value == position;
    }

    public bool IsPromoting()
    {
        return isPromoting;
    }
    // แก้เงื่อนไขของ CanCastle()
    public bool CanCastle(bool isKingSide, ChessPiece.Team team)
    {
        int row = (team == ChessPiece.Team.White) ? 0 : 7; // แถวของคิง

        // หาตำแหน่งคิงและเรือ
        Vector2Int kingPosition = new Vector2Int(4, row);
        Vector2Int rookPosition = isKingSide ? new Vector2Int(7, row) : new Vector2Int(0, row);

        // ตรวจสอบว่าคิงและเรือยังไม่เคยเคลื่อนที่
        if (!piecesOnBoard.TryGetValue(kingPosition, out ChessPiece king) || king.HasMoved)
        {
            Debug.Log($"❌ CanCastle Fail: King missing or moved. Pos:{kingPosition}, HasMoved:{king?.HasMoved}");
            return false;
        }
        if (!piecesOnBoard.TryGetValue(rookPosition, out ChessPiece rook) || rook.HasMoved)
        {
            Debug.Log($"❌ CanCastle Fail: Rook missing or moved. Pos:{rookPosition}, HasMoved:{rook?.HasMoved}");
            return false;
        }

        // ตรวจสอบว่า Castling ยังสามารถทำได้อยู่
        if (team == ChessPiece.Team.White)
        {
            if (isKingSide && !whiteCanCastleKingSide) { Debug.Log("❌ CanCastle Fail: White KingSide flag false"); return false; }
            if (!isKingSide && !whiteCanCastleQueenSide) { Debug.Log("❌ CanCastle Fail: White QueenSide flag false"); return false; }
        }
        else if (team == ChessPiece.Team.Black)
        {
            if (isKingSide && !blackCanCastleKingSide) { Debug.Log("❌ CanCastle Fail: Black KingSide flag false"); return false; }
            if (!isKingSide && !blackCanCastleQueenSide) { Debug.Log("❌ CanCastle Fail: Black QueenSide flag false"); return false; }
        }

        // ตรวจสอบเงื่อนไขอื่นๆ (เช่น ช่องว่าง, ไม่ถูกโจมตี)
        if (isKingSide)
        {
            if (!IsTileEmpty(new Vector2Int(5, row))) { Debug.Log("❌ CanCastle Fail: Tile (5,row) not empty"); return false; }
            if (!IsTileEmpty(new Vector2Int(6, row))) { Debug.Log("❌ CanCastle Fail: Tile (6,row) not empty"); return false; }
            if (IsPositionUnderAttack(new Vector2Int(4, row), team)) { Debug.Log("❌ CanCastle Fail: King is in check"); return false; }
            if (IsPositionUnderAttack(new Vector2Int(5, row), team)) { Debug.Log("❌ CanCastle Fail: Path (5,row) attacked"); return false; }
            if (IsPositionUnderAttack(new Vector2Int(6, row), team)) { Debug.Log("❌ CanCastle Fail: Path (6,row) attacked"); return false; }
            return true;
        }
        else
        {
            if (!IsTileEmpty(new Vector2Int(2, row))) { Debug.Log("❌ CanCastle Fail: Tile (2,row) not empty"); return false; }
            if (!IsTileEmpty(new Vector2Int(3, row))) { Debug.Log("❌ CanCastle Fail: Tile (3,row) not empty"); return false; }
            if (IsPositionUnderAttack(new Vector2Int(4, row), team)) { Debug.Log("❌ CanCastle Fail: King is in check"); return false; }
            if (IsPositionUnderAttack(new Vector2Int(3, row), team)) { Debug.Log("❌ CanCastle Fail: Path (3,row) attacked"); return false; }
            if (IsPositionUnderAttack(new Vector2Int(2, row), team)) { Debug.Log("❌ CanCastle Fail: Path (2,row) attacked"); return false; }
            return true;
        }
    }
    // ฟังก์ชันสำหรับการคลิกที่ช่องบนกระดาน
    public void OnTileClicked(Vector2Int position)
    {
        Debug.Log($"Tile clicked at: {position}");

        if (piecesOnBoard.TryGetValue(position, out ChessPiece clickedPiece))
        {
            // 🟢 ถ้าคลิกที่หมาก → เปลี่ยนตัวเลือก
            SelectPiece(clickedPiece);
        }
        else if (selectedPiece != null)
        {
            // 🟢 ถ้าคลิกที่ช่องว่าง → เดินหมาก
            MoveSelectedPiece(position);
        }

    }

    public void SelectPiece(ChessPiece piece)
    {
        if (PauseManager.isPaused) return;

        // ตรวจสอบว่าหมากที่เลือกเป็น null หรือไม่
        if (piece == null)
        {
            Debug.Log("❌ ไม่สามารถเลือกช่องว่างได้");
            return;
        }

        // ถ้ายังไม่มีหมากที่ถูกเลือก -> เลือกหมากใหม่
        if (selectedPiece == null)
        {
            selectedPiece = piece;
            Debug.Log($"✅ เลือก {piece.team} {piece.pieceType}");
        }
        else // ถ้ามีหมากที่ถูกเลือกอยู่แล้ว
        {
            if (piece.team == selectedPiece.team && selectedPiece == piece)
            {
                // ยกเลิกการเลือก
                selectedPiece = null;
                Debug.Log("🔄 ยกเลิกการเลือก");
            }
            // ถ้าเลือกหมากทีมเดียวกัน
            else if (piece.team == selectedPiece.team)
            {
                // เลือกหมากใหม่
                selectedPiece = piece;
                Debug.Log($"🔄 เปลี่ยนเป็น {piece.team} {piece.pieceType}");
            }
            else // ถ้าเลือกหมากศัตรู
            {
                // ตรวจสอบว่าสามารถโจมตีได้หรือไม่
                if (selectedPiece.IsValidMove(piece.boardPosition))
                {
                    Debug.Log($"⚔️ โจมตี {piece.team} {piece.pieceType}!");
                    MoveSelectedPiece(piece.boardPosition);
                }
                else
                {
                    Debug.Log("❌ ไม่สามารถโจมตีหมากนี้ได้");
                }
            }
        }
    }

    public void MoveSelectedPiece(Vector2Int newPosition, AiPerformanceData aiStats = null)
    {
        if (!ValidatePreMoveConditions(newPosition)) return;
        if (HandleCastling(newPosition)) return;
        if (HandleEnPassant(newPosition)) return;

        // ✅ ตรวจสอบว่าตำแหน่งที่เลือกเดินได้หรือไม่
        if (!selectedPiece.IsValidMove(newPosition))
        {
            Debug.Log("❌ เดินไม่ได้! กฎไม่อนุญาตให้เดินไปตำแหน่งนี้");
            return;
        }

        // ✅ ตรวจสอบว่าเดินไปแล้วคิงจะถูก Check หรือไม่
        Vector2Int originalPosition = selectedPiece.boardPosition;
        ChessPiece.PieceType originalPieceType = selectedPiece.pieceType;
        ChessPiece capturedPiece = SimulateMove(newPosition);

        if (IsKingInCheck(selectedPiece.team))
        {
            UndoSimulatedMove(originalPosition, newPosition, capturedPiece);
            Debug.Log("❌ ไม่สามารถเดินไปตำแหน่งนี้ได้ เพราะจะทำให้คิงของตัวเองถูก Check!");
            return;
        }

        FinalizeMove(originalPosition, newPosition, capturedPiece);

        if (selectedPiece == null) return;
        selectedPiece.PromotePawn();
        ChessPiece.PieceType? promotedPieceType =
            originalPieceType == ChessPiece.PieceType.Pawn && selectedPiece.pieceType != ChessPiece.PieceType.Pawn
                ? selectedPiece.pieceType
                : null;
        HandleCheckState();

        bool wasCapture = capturedPiece != null;
        UpdateFiftyMoveRuleCounter(wasCapture);
        SaveMoveToHistory(selectedPiece, originalPosition, newPosition, capturedPiece, promotedPieceType, aiStats, originalPieceType);

        // ❌ Remove old push
        boardModel.PushCurrentPosition();

        // ✅ Only update board model if NOT promoting (Promotion will trigger update later)
        if (!isPromoting)
        {
            UpdateBoardModel();
        }

        MoveResult result = new MoveResult(originalPosition, newPosition, selectedPiece.pieceType);
        if (capturedPiece != null)
        {
            result.CapturedType = capturedPiece.pieceType;
            result.CapturedTeam = capturedPiece.team;
        }
        OnMoveCompleted?.Invoke(result);

        if (OnMoveCompleted == null && gameManager != null)
        {
            var mode = gameManager.gameModeManager != null
                ? gameManager.gameModeManager.CurrentMode
                : GameModeManager.GameModes.SinglePlayer;

            if (mode != GameModeManager.GameModes.Online)
            {
                gameManager.SwitchTurn();
                gameManager.CheckGameState();
            }
        }


        selectedPiece = null;
    }

    // ✅ New Method to Sync Model from Unity Board
    public void UpdateBoardModel()
    {
        // 1. Convert current Unity board to new AI Model
        ChessBoardModel newModel = AI.Utilities.BoardConverter.Convert(this, gameManager.GetCurrentTurn(), gameManager.GetCurrentTurn());

        // 2. Preserve History from old model
        if (boardModel != null)
        {
            newModel.SetPositionHistory(boardModel.GetPositionHistory(), boardModel.GetPositionCounts());
        }

        // 3. Record new position (Push Zobrist)
        newModel.RecordPosition();

        // 4. Replace old model
        this.boardModel = newModel;

        // Debug.Log($"🔄 BoardModel updated. Turn: {newModel.IsWhiteTurn}, Key: {newModel.ZobristKey}");
    }

    public void PerformCastling(bool isKingSide, ChessPiece.Team team)
    {
        int row = (team == ChessPiece.Team.White) ? 0 : 7;

        // ตำแหน่งใหม่ของคิงและเรือ
        Vector2Int kingNewPos = isKingSide ? new Vector2Int(6, row) : new Vector2Int(2, row);
        Vector2Int rookOldPos = isKingSide ? new Vector2Int(7, row) : new Vector2Int(0, row);
        Vector2Int rookNewPos = isKingSide ? new Vector2Int(5, row) : new Vector2Int(3, row);

        // ย้ายคิง
        ChessPiece king = piecesOnBoard[new Vector2Int(4, row)];
        piecesOnBoard.Remove(king.boardPosition);
        king.MoveTo(kingNewPos);
        piecesOnBoard[kingNewPos] = king;
        king.HasMoved = true; // ป้องกันการ Castling อีกครั้ง

        // ย้ายเรือ
        ChessPiece rook = piecesOnBoard[rookOldPos];
        piecesOnBoard.Remove(rookOldPos);
        rook.MoveTo(rookNewPos);
        piecesOnBoard[rookNewPos] = rook;
        rook.HasMoved = true; // ป้องกันการ Castling อีกครั้งด

        // อัปเดตสถานะว่า Castling ไม่สามารถทำได้อีก
        if (isKingSide)
            SetCanCastleKingSide(team, false);
        else
            SetCanCastleQueenSide(team, false);

        Debug.Log($"🏰 {team} ทำ Castling {(isKingSide ? "King-side" : "Queen-side")}");
        SaveMoveToHistory(king, new Vector2Int(4, row), kingNewPos, null);  // บันทึก Castling


        if (OnMoveCompleted != null)
        {
            MoveResult result = new MoveResult(new Vector2Int(4, row), kingNewPos, king.pieceType);
            result.IsCastling = true;
            OnMoveCompleted.Invoke(result);
        }

        selectedPiece = null;
    }

    public void RecordMove(string move)
    {
        Debug.Log("Last move: " + move);
    }

    public void ResetFiftyMoveRuleCounter()
    {
        _movesWithoutCaptureOrPawn = 0;
    }

    public void ResetBoard()
    {
        // ลบหมากทั้งหมด
        foreach (var piece in piecesOnBoard.Values)
        {
            Destroy(piece.gameObject);
        }
        piecesOnBoard.Clear();
        selectedPiece = null;
        enPassantTarget = null;
        isPromoting = false;
        _movesWithoutCaptureOrPawn = 0;
        // รีเซ็ตสถานะ Castling
        whiteCanCastleKingSide = true;
        whiteCanCastleQueenSide = true;
        blackCanCastleKingSide = true;
        blackCanCastleQueenSide = true;
        // สร้างกระดานใหม่และวางหมากใหม่
        ChessBoardGenerate generator = GetComponent<ChessBoardGenerate>();
        if (generator != null)
        {
            generator.RebuildBoard();
        }
    }

    // ✅ ฟังก์ชันสำหรับรับค่าจาก Server แล้วสั่งเดินตาม
    public void ApplyNetworkMove(MoveDto moveData)
    {
        Vector2Int from = moveData.from_position;
        Vector2Int to = moveData.to_position;

        if (piecesOnBoard.TryGetValue(from, out ChessPiece piece))
        {
            Debug.Log($"Inbox Apply Network Move: {from} -> {to} ({piece.pieceType})");

            // 1. ตรวจสอบ Castling
            if (moveData.isCastling)
            {
                // Logic การย้าย King ถูกทำใน FinalizeMoveDirect อยู่แล้ว
                // แต่ต้องย้าย Rook ด้วย manually เพราะ FinalizeMoveDirect ย้ายแค่ตัวเดียว
                int row = (piece.team == ChessPiece.Team.White) ? 0 : 7;
                bool isKingSide = to.x == 6; // g1 or g8
                Vector2Int rookOldPos = isKingSide ? new Vector2Int(7, row) : new Vector2Int(0, row);
                Vector2Int rookNewPos = isKingSide ? new Vector2Int(5, row) : new Vector2Int(3, row);

                if (piecesOnBoard.TryGetValue(rookOldPos, out ChessPiece rook))
                {
                    FinalizeMoveDirect(rook, rookOldPos, rookNewPos, null);
                }
            }

            // 2. ตรวจสอบ Capture & En Passant
            ChessPiece captured = null;
            if (moveData.isEnPassant)
            {
                // เป้าหมาย En Passant อยู่คนละช่องกับ to
                Vector2Int capturedPos = new Vector2Int(to.x, from.y);
                if (piecesOnBoard.TryGetValue(capturedPos, out ChessPiece epPawn))
                {
                    captured = epPawn;
                    piecesOnBoard.Remove(capturedPos); // Remove logic from board dictionary
                    Destroy(captured.gameObject); // Destroy visual
                }
            }
            else if (piecesOnBoard.TryGetValue(to, out ChessPiece target))
            {
                captured = target;
            }

            // 3. ย้ายตัวหมากหลัก (King or Piece)
            FinalizeMoveDirect(piece, from, to, captured);

            // 4. Promotion
            ChessPiece.PieceType? promotedType = null;
            if (moveData.promotedTo > 0)
            {
                promotedType = (ChessPiece.PieceType)moveData.promotedTo;
                piece.Promote(promotedType.Value);
            }

            // 5. บันทึก History (สำคัญ! ส่ง promotedType ไปด้วย)
            SaveMoveToHistory(
                piece,
                from,
                to,
                captured,
                promotedType,
                null,
                promotedType.HasValue ? ChessPiece.PieceType.Pawn : null
            );

            // 6. แจ้งเตือนว่าเดินเสร็จแล้ว
            boardModel.PushCurrentPosition();

            MoveResult result = new MoveResult(from, to, piece.pieceType);
            if (captured != null)
            {
                result.CapturedType = captured.pieceType;
                result.CapturedTeam = captured.team;
            }
            if (moveData.isCastling) result.IsCastling = true;
            if (moveData.isEnPassant) result.IsEnPassant = true;
            if (promotedType.HasValue) result.PromotedTo = promotedType.Value;

            OnMoveCompleted?.Invoke(result);
        }
        else
        {
            Debug.LogError($"❌ Network Move Error: Not found piece at {from}");
        }
    }

    public void ApplyNetworkMove(string from, string to)
    {
        Vector2Int fromPos = AlgebraicToVector(from);
        Vector2Int toPos = AlgebraicToVector(to);
        if (piecesOnBoard.TryGetValue(fromPos, out ChessPiece piece))
        {
            SelectPiece(piece);
            MoveSelectedPiece(toPos);
        }
    }
}
