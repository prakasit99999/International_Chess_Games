
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
//using static AICore;
using static ChessPiece;

public class ChessBoard : MonoBehaviour
{
    private float tileSize = 1.0f;
    private const int boardSize = 8;
    private int _movesWithoutCaptureOrPawn = 0;
    private const int MAX_MOVES_WITHOUT_PROGRESS = 50;
    private GameManager gameManager;
    private HistoryMove historyMove;
    private ChessPiece[,] board = new ChessPiece[boardSize, boardSize];
    private ChessPiece selectedPiece = null; // ตัวแปรเก็บหมากที่ถูกเลือก
    private bool whiteCanCastleKingSide = true;
    private bool whiteCanCastleQueenSide = true;
    private bool blackCanCastleKingSide = true;
    private bool blackCanCastleQueenSide = true;
    private bool isPromoting = false; // ✅ ตัวแปรเช็คว่ากำลังเลื่อนขั้นหรือไม่
    private Vector2Int? enPassantTarget = null; // ตำแหน่งเบี้ยที่เดินสองช่องในตาแรก
    private Dictionary<Vector2Int, ChessPiece> piecesOnBoard = new Dictionary<Vector2Int, ChessPiece>();
    private Dictionary<Vector2Int, TileClick> tileClickMap = new Dictionary<Vector2Int, TileClick>();

    public IReadOnlyDictionary<Vector2Int, ChessPiece> PiecesOnBoard => piecesOnBoard;
    public int FiftyMoveCounter => _movesWithoutCaptureOrPawn;
    public GameManager GameManager => gameManager;

    public static ChessBoard Instance { get; private set; }
    public Transform pieceWhite;    // Empty GameObject สำหรับทีมขาว
    public Transform pieceBlack;    // Empty GameObject สำหรับทีมดำ
    public Transform boardLabels;    //Empty GameObject ส่วนรับตัวอักษรและตัวเลขบนกระดาน
    public GameObject piecePrefab;  // Prefab ของตัวหมากรุก
    public GameObject tilePrefab;  // Prefab ของช่องกระดาน
    public GameObject textPrefab;  // Prefab ของตัวอักษรและตัวเลขบนกระดาน

    public Sprite[] whiteSprites;  // Array เก็บ Sprite ทีมขาว
    public Sprite[] blackSprites;  // Array เก็บ Sprite ทีมดำ
    public ChessPiece selectedPawn; // เบี้ยที่รอเลื่อนขั้น
    public PromotionManager promotionManager; // เชื่อมกับ PromotionManager ใน Inspector
    public ChessPiece SelectedPiece => selectedPiece; // เพิ่ม Property เพื่อเข้าถึง selectedPiece

    public bool IsWhiteTurn { get; internal set; }

    public ChessPiece.PieceType promotionFrom;
    public Vector2Int promotionPosition;
    public Vector2Int position;  // ตัวแปรสำหรับเก็บตำแหน่งของหมาก
    public Color32 whitleColor = new Color32(255, 255, 255, 255);
    public Color32 blackColor = new Color32(0, 0, 0, 255);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            historyMove = FindObjectOfType<HistoryMove>();

        }
        else
        {
            Destroy(gameObject); // ป้องกัน ChessBoard ซ้ำกัน
        }
    }

    // Start is called before the first frame updateฟ
    void Start()
    {
        GenerateBoard();
        GenerateBoardLabels();
        SpawnPieces();
    }
    // Update is called once per frame
    void Update()
    {
    }
    //create a chess board with tiles methon viod GenerateBoard()
    void GenerateBoard()
    {
        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                float posX = x * tileSize;  // กำหนดตำแหน่งแนวนอน
                float posY = y * tileSize;  // กำหนดตำแหน่งแนวตั้ง

                // สร้างช่องกระดานใหม่ที่ตำแหน่ง (posX, posY)
                GameObject tile = Instantiate(tilePrefab, new Vector2(posX, posY), Quaternion.identity);
                tile.transform.parent = transform;  // ตั้งค่าให้เป็นลูกของ BoardManager

                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                renderer.color = (x + y) % 2 == 0 ? whitleColor : blackColor;

                string column = ((char)('A' + x)).ToString();
                string row = (y + 1).ToString();
                tile.name = column + row;

                // เพิ่ม BoxCollider2D เพื่อให้สามารถคลิกได้
                BoxCollider2D boxCollider2D = tile.GetComponent<BoxCollider2D>();
                if (boxCollider2D == null)
                {
                    boxCollider2D = tile.AddComponent<BoxCollider2D>();
                }
                boxCollider2D.enabled = true;

                // เพิ่มคอมโพเนนต์ TileClick และกำหนดค่าตำแหน่ง
                TileClick tileClick = tile.AddComponent<TileClick>();
                tileClick.SetTilePosition(new Vector2Int(x, y), this);
                tileClickMap[new Vector2Int(x, y)] = tileClick;



            }
        }
        
    }

    void GenerateBoardLabels()
    {
        float centerOffset = tileSize / 2f;

        // 🔤 A–H (แนวนอนล่าง, ซ้ายสุด)
        for (int x = 0; x < boardSize; x++)
        {
            string label = ((char)('A' + x)).ToString();
            Vector3 pos = new Vector3(
                x * tileSize + centerOffset,
                -centerOffset,
                0f
            );

            GameObject labelObj = Instantiate(textPrefab, pos, Quaternion.identity, boardLabels);
            labelObj.name = "Label_" + label; // ตั้งชื่อให้ชัดเจน
            TMP_Text text = labelObj.GetComponent<TMP_Text>();
            text.text = label;
            text.alignment = TextAlignmentOptions.BaselineLeft;  // ✅ ซ้าย-ฐาน
        }

        // 🔢 1–8 (แนวตั้งซ้าย, ขวาสุด)
        for (int y = 0; y < boardSize; y++)
        {
            string label = (y + 1).ToString();
            Vector3 pos = new Vector3(
                -centerOffset,
                y * tileSize + centerOffset,
                0f
            );

            GameObject labelObj = Instantiate(textPrefab, pos, Quaternion.identity, boardLabels);
            TMP_Text text = labelObj.GetComponent<TMP_Text>();
            labelObj.name = "Label_" + label; 
            text.text = label;
            text.alignment = TextAlignmentOptions.CaplineRight;
        }
    }

    // 🏁 สร้างตัวหมากรุกในตำแหน่งเริ่มต้น
    void SpawnPieces()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("❌ tilePrefab ยังไม่ได้เซ็ตใน ChessBoard!");
            return;
        }
        // 🏇 วางเบี้ย (Pawn) ที่แถว 1 และ 6
        for (int i = 0; i < boardSize; i++)
        {
            SpawnPiece(ChessPiece.PieceType.Pawn, ChessPiece.Team.White, new Vector2Int(i, 1));
            SpawnPiece(ChessPiece.PieceType.Pawn, ChessPiece.Team.Black, new Vector2Int(i, 6));
        }

        // 🏰 วางเรือ (Rook)
        SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.White, new Vector2Int(0, 0));
        SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.White, new Vector2Int(7, 0));
        SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.Black, new Vector2Int(0, 7));
        SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.Black, new Vector2Int(7, 7));

        //// 🏇 วางม้า (Knight)
        SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.White, new Vector2Int(1, 0));
        SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.White, new Vector2Int(6, 0));
        SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.Black, new Vector2Int(1, 7));
        SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.Black, new Vector2Int(6, 7));

        //// 🏹 วางบิชอป (Bishop)
        SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.White, new Vector2Int(2, 0));
        SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.White, new Vector2Int(5, 0));
        SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.Black, new Vector2Int(2, 7));
        SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.Black, new Vector2Int(5, 7));

        // 👑 วางควีน (Queen)
        SpawnPiece(ChessPiece.PieceType.Queen, ChessPiece.Team.White, new Vector2Int(3, 0));
        SpawnPiece(ChessPiece.PieceType.Queen, ChessPiece.Team.Black, new Vector2Int(3, 7));

        // 🤴 วางคิง (King)
        SpawnPiece(ChessPiece.PieceType.King, ChessPiece.Team.White, new Vector2Int(4, 0));
        SpawnPiece(ChessPiece.PieceType.King, ChessPiece.Team.Black, new Vector2Int(4, 7));
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

                SaveMoveToHistory(oldPosition, newPosition, null);

                enPassantTarget = null;

                selectedPiece = null;
                GameManager.Instance.SwitchTurn();

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
        if (captured != null)
        {
            Debug.Log($"⚔️ {selectedPiece.team} {selectedPiece.pieceType} กิน {captured.team} {captured.pieceType}!");
            Destroy(captured.gameObject);
        }

        selectedPiece.MoveTo(newPos);
        piecesOnBoard[newPos] = selectedPiece;

        if (!selectedPiece.HasMoved)
            selectedPiece.HasMoved = true;

        SetEnPassantTarget(originalPos, newPos);
    }

    private void SetEnPassantTarget(Vector2Int from, Vector2Int to)
    {
        if (selectedPiece.pieceType == ChessPiece.PieceType.Pawn &&
            Mathf.Abs(to.y - from.y) == 2)
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
        var opponent = (gameManager.GetCurrentTurn() == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
        if (IsKingInCheckmate(opponent))
        {
            Debug.Log($"♟️ Checkmate! {gameManager.GetCurrentTurn()} ชนะเกม!");
            gameManager.GameOver(gameManager.GetCurrentTurn());
        }
        else if (IsKingInCheck(opponent))
        {
            Debug.Log($"⚠️ Check! {opponent} กำลังถูกโจมตี!");
        }
    }

    private void SaveMoveToHistory(Vector2Int from, Vector2Int to, ChessPiece captured)
    {
        if (historyMove == null) return;

        bool isCastling = selectedPiece.pieceType == ChessPiece.PieceType.King && Mathf.Abs(to.x - from.x) == 2;
        bool isEnPassant = selectedPiece.pieceType == ChessPiece.PieceType.Pawn && IsEnPassantTarget(to);
        bool isPawnTwoStep = selectedPiece.pieceType == ChessPiece.PieceType.Pawn && Mathf.Abs(to.y - from.y) == 2;
        bool pieceHasMovedBefore = selectedPiece.HasMoved;
        bool isCheck = IsKingInCheck(selectedPiece.team == ChessPiece.Team.White ? ChessPiece.Team.Black : ChessPiece.Team.White);
        bool isPromotion = selectedPiece.pieceType == ChessPiece.PieceType.Pawn && (to.y == 0 || to.y == 7);

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
                capturedTeam = OpponentTeam(selectedPiece.team);
            }
            else if (captured != null)
            {
                capturedType = captured.pieceType;
                capturedTeam = captured.team;
            }
        }

        // ตั้งค่าการเลื่อนขั้น
        ChessPiece.PieceType promotedTo = isPromotion
            ? PromotionManager.Instance.GetSelectedPromotionType()
            : selectedPiece.pieceType;
        ChessPiece.PieceType promotedFrom = isPromotion
            ? ChessPiece.PieceType.Pawn
            : selectedPiece.pieceType;
        Vector2Int promotionPosition = isPromotion ? to : Vector2Int.zero;

        // ✅ บันทึกข้อมูลการเดิน
        historyMove.AddMove(
            from,
            to,
            selectedPiece.pieceType,
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
            selectedPiece.team,
            _movesWithoutCaptureOrPawn
        );

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
            GameManager.Instance.GameOver(ChessPiece.Team.None);
        }

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
        //Debug.Log($"🔄 สถานะเลื่อนขั้น: {isPromoting}");
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
    // 🎯 ฟังก์ชันสร้างหมากและวางลงบนกระดาน
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

        GameObject pieceObj = Instantiate(piecePrefab, new Vector2(position.x, position.y), Quaternion.identity);
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
        return piece; // ✅ คืนค่า ChessPiece

    }

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

    public Vector2Int FindKingPosition(ChessPiece.Team team)
    {
        foreach (var entry in piecesOnBoard)
        {
            ChessPiece piece = entry.Value;
            if (piece.pieceType == ChessPiece.PieceType.King && piece.team == team)
            {
                return piece.boardPosition;
            }
        }
        throw new System.Exception($"ไม่พบคิงของทีม {team}");
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
        return position.x >= 0 && position.x < boardSize && position.y >= 0 && position.y < boardSize;
    }

    public bool IsPositionUnderAttack(Vector2Int position, ChessPiece.Team team)
    {
        foreach (var entry in piecesOnBoard)
        {
            ChessPiece piece = entry.Value;

            // ถ้าเป็นหมากของศัตรู
            if (piece.team != team)
            {
                // ตรวจสอบประเภทของหมากและตำแหน่งที่สามารถโจมตีได้
                switch (piece.pieceType)
                {
                    case ChessPiece.PieceType.Pawn:
                        // เบี้ยโจมตีเฉพาะแนวทแยง
                        int direction = (piece.team == ChessPiece.Team.White) ? 1 : -1;
                        if (Mathf.Abs(position.x - piece.boardPosition.x) == 1 &&
                            position.y == piece.boardPosition.y + direction)
                        {
                            return true;
                        }
                        break;

                    case ChessPiece.PieceType.Knight:
                        // ม้าโจมตีแบบ L-Shape
                        int dx = Mathf.Abs(position.x - piece.boardPosition.x);
                        int dy = Mathf.Abs(position.y - piece.boardPosition.y);
                        if ((dx == 2 && dy == 1) || (dx == 1 && dy == 2))
                        {
                            return true;
                        }
                        break;

                    default:
                        // หมากอื่นๆ ใช้ IsValidMove
                        if (piece.IsValidMove(position))
                        {
                            return true;
                        }
                        break;
                }
            }
        }
        return false;
    }

    public bool IsKingInCheck(ChessPiece.Team team)
    {
        // หาตำแหน่งของคิง
        Vector2Int kingPosition = FindKingPosition(team);

        // ตรวจสอบว่าตำแหน่งคิงถูกโจมตีหรือไม่
        return IsPositionUnderAttack(kingPosition, team);
    }

    public bool IsKingInCheckmate(ChessPiece.Team team)
    {
        if (!IsKingInCheck(team))
            return false; // ถ้าไม่ได้ถูก Check ก็ไม่มีทาง Checkmate

        Stack<Action> changes = new Stack<Action>();

        // ลูปตรวจสอบหมากทุกตัวของทีม
        foreach (var position in piecesOnBoard.Keys.ToList())
        {
            if (!piecesOnBoard.TryGetValue(position, out ChessPiece piece) || piece.team != team)
                continue;

            foreach (var move in piece.GetValidMoves())
            {
                Vector2Int originalPosition = piece.boardPosition;
                ChessPiece capturedPiece = null;

                // จำลองการเดิน
                if (piecesOnBoard.TryGetValue(move, out capturedPiece))
                {
                    changes.Push(() => piecesOnBoard[move] = capturedPiece); // Undo การกิน
                    piecesOnBoard.Remove(move);
                }
                changes.Push(() => piecesOnBoard[originalPosition] = piece);
                piecesOnBoard.Remove(originalPosition);
                piecesOnBoard[move] = piece;
                piece.boardPosition = move;

                bool stillInCheck = IsKingInCheck(team);

                // Undo การเดิน
                while (changes.Count > 0)
                {
                    changes.Pop().Invoke();
                }

                if (!stillInCheck)
                    return false;
            }
        }

        return true; // ถ้าไม่มีหมากตัวไหนสามารถช่วยคิงได้ -> Checkmate
    }

    public bool IsStalemate(ChessPiece.Team team)
    {
        if (IsKingInCheck(team))
            return false; // ถ้ายังถูก Check ไม่ถือว่าเป็น Stalemate

        foreach (var position in piecesOnBoard.Keys.ToList())
        {
            if (!piecesOnBoard.TryGetValue(position, out ChessPiece piece) || piece.team != team)
                continue;

            foreach (var move in piece.GetValidMoves())
            {
                Vector2Int originalPosition = piece.boardPosition;
                ChessPiece capturedPiece = null;

                // จำลองการเดิน
                if (piecesOnBoard.TryGetValue(move, out capturedPiece))
                    piecesOnBoard.Remove(move);
                piecesOnBoard.Remove(originalPosition);
                piecesOnBoard[move] = piece;
                piece.boardPosition = move;

                bool stillInCheck = IsKingInCheck(team);

                // Undo การเดิน
                piecesOnBoard.Remove(move);
                piecesOnBoard[originalPosition] = piece;
                piece.boardPosition = originalPosition;
                if (capturedPiece != null)
                    piecesOnBoard[move] = capturedPiece;

                if (!stillInCheck)
                    return false;
            }
        }
        return true; // ถ้าไม่มีการเดินที่ถูกต้องเลย ถือว่าเป็น Stalemate
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
            return false;
        if (!piecesOnBoard.TryGetValue(rookPosition, out ChessPiece rook) || rook.HasMoved)
            return false;

        // ตรวจสอบว่า Castling ยังสามารถทำได้อยู่
        if (team == ChessPiece.Team.White)
        {
            if (isKingSide && !whiteCanCastleKingSide) return false;
            if (!isKingSide && !whiteCanCastleQueenSide) return false;
        }
        else if (team == ChessPiece.Team.Black)
        {
            if (isKingSide && !blackCanCastleKingSide) return false;
            if (!isKingSide && !blackCanCastleQueenSide) return false;
        }


        // ตรวจสอบเงื่อนไขอื่นๆ (เช่น ช่องว่าง, ไม่ถูกโจมตี)
        if (isKingSide)
        {
            return IsTileEmpty(new Vector2Int(5, row)) &&
                   IsTileEmpty(new Vector2Int(6, row)) &&
                   !IsPositionUnderAttack(new Vector2Int(4, row), team) && // คิงไม่ถูกเช็ค
                   !IsPositionUnderAttack(new Vector2Int(5, row), team) && // ช่องที่คิงเดินผ่านไม่ถูกโจมตี
                   !IsPositionUnderAttack(new Vector2Int(6, row), team);  // ช่องที่คิงไปอยู่ไม่ถูกโจมตี
        }
        else
        {
            return IsTileEmpty(new Vector2Int(2, row)) &&
                   IsTileEmpty(new Vector2Int(3, row)) &&
                   !IsPositionUnderAttack(new Vector2Int(4, row), team) && // คิงไม่ถูกเช็ค
                   !IsPositionUnderAttack(new Vector2Int(3, row), team) && // ช่องที่คิงเดินผ่านไม่ถูกโจมตี
                   !IsPositionUnderAttack(new Vector2Int(2, row), team);  // ช่องที่คิงไปอยู่ไม่ถูกโจมตี
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

        if (gameManager == null)
        {
            Debug.LogError("❌ gameManager ยังเป็น null!");
            return;
        }

        if (gameManager.IsGameOver())
        {
            Debug.Log("❌ เกมจบแล้ว ไม่สามารถเล่นต่อได้!");
            return;
        }

        // ถ้ายังไม่มีหมากที่ถูกเลือก
        if (selectedPiece == null)
        {
            // ตรวจสอบว่าเป็นเทิร์นของผู้เล่นหรือไม่
            if (piece.team != gameManager.GetCurrentTurn())
            {
                Debug.Log("❌ ไม่ใช่เทิร์นของคุณ!");
                return;
            }

            // เลือกหมาก
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

    public void MoveSelectedPiece(Vector2Int newPosition)
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
        HandleCheckState();

        bool wasCapture = capturedPiece != null;
        UpdateFiftyMoveRuleCounter(wasCapture);
        SaveMoveToHistory(originalPosition, newPosition, capturedPiece);

        if (!isPromoting)
        {
            GameManager.Instance.SwitchTurn();
        }

        selectedPiece = null;
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
        SaveMoveToHistory(new Vector2Int(4, row), kingNewPos, null);  // บันทึก Castling

        // สลับเทิร์น
        GameManager.Instance.SwitchTurn();
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
        GenerateBoard();
        SpawnPieces();
    }
}
