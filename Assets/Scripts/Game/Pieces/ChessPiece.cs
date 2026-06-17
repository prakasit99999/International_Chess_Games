using System.Collections.Generic;
using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    // public
    private Vector2 originalPosition; // ตำแหน่งเริ่มต้นของตัวหมาก
    private SpriteRenderer spriteRenderer;
    private ChessBoard boardManager;  // อ้างอิงถึง ChessBoard
    private List<ChessPiece> allPieces = new List<ChessPiece>();  // ลิสต์ที่เก็บชิ้นส่วนทั้งหมด

    public enum PieceType { Pawn = 0, Rook = 1, Knight = 2, Bishop = 3, Queen = 4, King = 5, None = -1 }
    public enum Team { White = 1, Black = 2, None = 0 }

    public PieceType pieceType;
    public Team team;

    public int currentX;
    public int currentY;
    public bool isSelected = false; // เช็คว่าหมากถูกเลือกหรือไม่
    public Vector2Int boardPosition;
    public ChessPiece.Team CurrentPlayerTeam { get; set; }
    public bool HasMoved { get; set; } = false; // ตรวจสอบว่าหมากขยับหรือยัง


    // Start is called before the first frame update
    void Start()
    {
        if (boardManager == null)
        {
            boardManager = ChessBoard.Instance ?? FindObjectOfType<ChessBoard>();
            if (boardManager == null)
            {
                Debug.LogWarning("⚠️ ไม่พบ ChessBoard ในฉาก (ใช้ SetBoardManager() แทนใน Test)");
            }
        }
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    //Set
    public void SetBoardManager(ChessBoard manager)
    {
        boardManager = manager;
    }

    public void SetPosition(int x, int y)
    {
        currentX = x;
        currentY = y;
        boardPosition = new Vector2Int(x, y);
        transform.position = new Vector2(x, y);
    }

    private void OnMouseDown()
    {
        if (ChessBoard.Instance.IsPromoting()) return;

        // ✅ ให้ GameManager ตรวจสอบสิทธิ์ก่อน (Online/Local/Turn)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TrySelectPiece(this);
        }
    }

    private bool ValidatePawnMove(Vector2Int newPosition)
    {
        int direction = (team == Team.White) ? 1 : -1; // ทิศทางเดิน (ขาวขึ้น, ดำลง)
        int startRow = (team == Team.White) ? 1 : 6; // แถวเริ่มต้นของเบี้ย

        // เดินหน้า 1 ช่อง
        if (newPosition.x == boardPosition.x && newPosition.y == boardPosition.y + direction)
        {
            return boardManager.IsTileEmpty(newPosition);
        }

        // เดินสองช่องในตาแรก
        if (!HasMoved && boardPosition.y == startRow &&
            newPosition.x == boardPosition.x && newPosition.y == boardPosition.y + (direction * 2))
        {
            Vector2Int middlePosition = new Vector2Int(boardPosition.x, boardPosition.y + direction);
            return boardManager.IsTileEmpty(middlePosition) && boardManager.IsTileEmpty(newPosition);
        }

        // กินหมากแนวทแยง
        if (Mathf.Abs(newPosition.x - boardPosition.x) == 1 && newPosition.y == boardPosition.y + direction)
        {
            // ครวจสอบการกิน En Passant
            if (boardManager.IsEnPassantTarget(newPosition))
            {
                return true;
            }

            return boardManager.IsEnemyAtPosition(newPosition, team);
        }


        return false;
    }

    private bool ValidateRookMove(Vector2Int newPosition)
    {
        return newPosition.x == boardPosition.x || newPosition.y == boardPosition.y;
    }

    private bool ValidateKnightMove(Vector2Int newPosition)
    {
        int dx = Mathf.Abs(newPosition.x - boardPosition.x);
        int dy = Mathf.Abs(newPosition.y - boardPosition.y);
        return (dx == 2 && dy == 1) || (dx == 1 && dy == 2);
    }

    private bool ValidateBishopMove(Vector2Int newPosition)
    {
        int dx = Mathf.Abs(newPosition.x - boardPosition.x);
        int dy = Mathf.Abs(newPosition.y - boardPosition.y);
        return dx == dy;
    }

    private bool ValidateQueenMove(Vector2Int newPosition)
    {
        return ValidateRookMove(newPosition) || ValidateBishopMove(newPosition);
    }

    private bool ValidateKingMove(Vector2Int newPosition)
    {
        int dx = Mathf.Abs(newPosition.x - boardPosition.x);
        int dy = Mathf.Abs(newPosition.y - boardPosition.y);
        return dx <= 1 && dy <= 1;
    }

    public void UpdateSprite()
    {
        if (team == Team.White)
        {
            spriteRenderer.sprite = ChessBoard.Instance.whiteSprites[(int)pieceType];
        }
        else
        {
            spriteRenderer.sprite = ChessBoard.Instance.blackSprites[(int)pieceType];
        }
    }

    public bool CanAttack(Vector2Int targetPosition)
    {
        // ตรวจสอบว่าตำแหน่งเป้าหมายอยู่บนกระดานหรือไม่
        if (!boardManager.IsPositionOnBoard(targetPosition))
        {
            return false;
        }

        // หมายเหตุ: การโจมตี (Attack) ไม่สนใจว่าช่องเป้าหมายจะเป็นอะไร (ว่าง หรือ มีเพื่อน หรือ มีศัตรู)
        // ยกเว้นกรณี Pawn ที่การกินกับการเดินปกติไม่เหมือนกัน

        // ตรวจสอบกฎการเดินพื้นฐานของหมากแต่ละประเภท
        bool isValidPattern = false;
        switch (pieceType)
        {
            case PieceType.Pawn:
                // Pawn special case: Attack is diagonal only
                int direction = (team == Team.White) ? 1 : -1;
                if (Mathf.Abs(targetPosition.x - boardPosition.x) == 1 && targetPosition.y == boardPosition.y + direction)
                {
                    isValidPattern = true; // Diagonal move for attack
                }
                // Note: En Passant logic is usually handled in Move validation, but technically it's a capture.
                // For "IsPositionUnderAttack" (King safety), simple diagonal check is enough.
                break;
            case PieceType.Rook:
                isValidPattern = ValidateRookMove(targetPosition);
                break;
            case PieceType.Knight:
                isValidPattern = ValidateKnightMove(targetPosition);
                break;
            case PieceType.Bishop:
                isValidPattern = ValidateBishopMove(targetPosition);
                break;
            case PieceType.Queen:
                isValidPattern = ValidateQueenMove(targetPosition);
                break;
            case PieceType.King:
                isValidPattern = ValidateKingMove(targetPosition);
                break;
        }

        if (!isValidPattern)
            return false;

        // ตรวจสอบสิ่งกีดขวาง (ยกเว้น Knight)
        if (pieceType == PieceType.Rook || pieceType == PieceType.Bishop || pieceType == PieceType.Queen)
        {
            if (!boardManager.IsPathClear(boardPosition, targetPosition, pieceType))
                return false;
        }

        return true;
    }

    public bool IsValidMove(Vector2Int newPosition)
    {
        // 1. ตรวจสอบว่าตำแหน่งใหม่อยู่บนกระดานหรือไม่
        if (!boardManager.IsPositionOnBoard(newPosition))
        {
            return false;
        }

        // 2. ตรวจสอบว่าตำแหน่งใหม่มีหมากทีมเดียวกันหรือไม่ (ห้ามกินพวกเดียวกัน)
        if (boardManager.IsOccupiedByTeam(newPosition, team))
        {
            return false;
        }

        // 3. ตรวจสอบ Pattern การเดิน (แยก Pawn ออกมาเพราะมี logic เดินปกติ vs กิน)
        bool isValidPattern = false;
        if (pieceType == PieceType.Pawn)
        {
            isValidPattern = ValidatePawnMove(newPosition);
        }
        else
        {
            isValidPattern = CanAttack(newPosition);
        }

        if (!isValidPattern)
            return false;

        // 4. เช็คว่าหลังจากเดินแล้ว King ของทีมนี้ยังปลอดภัยอยู่ไหม (Pinned Check)
        if (boardManager.DoesMoveExposeKing(this, newPosition))
        {
            return false;
        }

        return true;
    }

    // ในไฟล์ ChessPiece.cs
    public void MoveTo(Vector2Int newPosition)
    {
        currentX = newPosition.x; // อัปเดตตำแหน่ง X
        currentY = newPosition.y; // อัปเดตตำแหน่ง Y
        boardPosition = newPosition;
        transform.position = new Vector3(newPosition.x, newPosition.y, 0);
        HasMoved = true; // ตั้งค่าให้รู้ว่าหมากถูกเคลื่อนแล้ว
    }

    public void Promote(PieceType newType)
    {
        if (this == null)
        {
            Debug.LogError("❌ ChessPiece เป็น null! ตรวจสอบว่าถูกเรียกใช้ถูกต้องหรือไม่");
            return;
        }

        Debug.Log($"🔼 กำลังเลื่อนขั้น {team} เบี้ยเป็น {newType}");

        // ✅ บันทึกข้อมูลก่อนเลื่อนขั้น
        ChessBoard.Instance.SetPromotionData(this.boardPosition, PieceType.Pawn);

        pieceType = newType; // อัปเดตประเภทหมาก
        UpdateSprite();

        ChessBoard.Instance.SetPromoting(false);
    }

    public void PromotePawn(PieceType promotedTo = PieceType.None)
    {
        // ✅ ถ้ามีการกำหนดค่าเลื่อนขั้นมาแล้ว (จาก Network) ให้ทำเลย
        if (promotedTo != PieceType.None)
        {
            if (pieceType == PieceType.Pawn && (boardPosition.y == 0 || boardPosition.y == 7))
            {
                Debug.Log($"🌍 Network Promotion: {promotedTo}");
                Promote(promotedTo);
            }
            return;
        }

        if (pieceType == PieceType.Pawn && (boardPosition.y == 0 || boardPosition.y == 7))
        {
            bool isAI =
                (team == Team.White && GameManager.Instance.WhitePlayer == GameManager.PlayerType.AI) ||
                (team == Team.Black && GameManager.Instance.BlackPlayer == GameManager.PlayerType.AI);

            if (isAI)
            {
                Debug.Log($"🤖 AI ({team}) Direct Auto-Promote -> Queen");
                Promote(PieceType.Queen);
                return;
            }

            Debug.Log("เบี้ยเดินถึงแถวสุดท้าย เริ่มกระบวนการเลื่อนขั้น");
            ChessBoard.Instance.SetPromoting(true);
            // ✅ บันทึกประเภทเดิมก่อนเลื่อนขั้น
            ChessBoard.Instance.SetPromotionData(this.boardPosition, PieceType.Pawn);
            PromotionManager.Instance.ShowPromotionMenu(this);
        }
    }

    public List<Vector2Int> GetValidMoves()
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector2Int targetPosition = new Vector2Int(x, y);
                if (IsValidMove(targetPosition))
                {
                    validMoves.Add(targetPosition);
                }

            }
        }

        // Include castling as legal king moves for game-state checks (stalemate/checkmate).
        if (pieceType == PieceType.King && boardManager != null)
        {
            int row = (team == Team.White) ? 0 : 7;
            Vector2Int kingSide = new Vector2Int(6, row);
            Vector2Int queenSide = new Vector2Int(2, row);

            if (boardManager.CanCastle(true, team) && !validMoves.Contains(kingSide))
                validMoves.Add(kingSide);

            if (boardManager.CanCastle(false, team) && !validMoves.Contains(queenSide))
                validMoves.Add(queenSide);
        }

        return validMoves;
    }
}

