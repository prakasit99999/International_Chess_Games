using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileClick : MonoBehaviour
{
    private ChessBoard boardManager;
    private Vector2Int tilePosition;
    private SpriteRenderer spriteRenderer;
    private Color32 originalColor;
    private bool isFlashing = false;
    //public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f); // สีเมื่อนำเมาส์ชี้
    public Color32 validMoveColor = new Color32(228, 243, 216, 204);   // สีเขียวความทึบ 80%
    public Color32 attackColor = new Color32(255, 128, 128, 204);    // สีแดงความทึบ 80%
    public Color32 checkColor = new Color32(255, 0, 0, 204);         // สีแดงสดความทึบ 80%
    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("❌ ไม่พบ SpriteRenderer ใน Tile: " + gameObject.name);
            enabled = false; // หยุดการทำงานของสคริปต์
            return;
        }
        originalColor = spriteRenderer.color; // เก็บสีเริ่มต้น
    }
    // Update is called once per frame
    void Update()
    {
        if (isFlashing) return; // หยุดการอัปเดตสีถ้ากำลังกระพริบ

        Color32 targetColor = originalColor;

        // ตรวจสอบคิงถูกเช็ค (ความสำคัญสูงสุด)
        if (boardManager != null)
        {
            if (boardManager.IsKingInCheck(ChessPiece.Team.White))
            {
                Vector2Int whiteKingPos = boardManager.FindKingPosition(ChessPiece.Team.White);
                if (whiteKingPos == tilePosition) targetColor = checkColor;
            }
            if (boardManager.IsKingInCheck(ChessPiece.Team.Black))
            {
                Vector2Int blackKingPos = boardManager.FindKingPosition(ChessPiece.Team.Black);
                if (blackKingPos == tilePosition) targetColor = checkColor;
            }
        }
        // ตรวจสอบทางเดินที่ถูกต้อง (ความสำคัญรอง)
        if (boardManager != null && boardManager.SelectedPiece != null)
        {
            if (boardManager.SelectedPiece.GetValidMoves().Contains(tilePosition))
            {
                targetColor = validMoveColor;
            }
        }
        // ตรวจสอบทางเดินและศัตรูที่โจมตีได้
        if (boardManager != null && boardManager.SelectedPiece != null)
        {
            if (boardManager.SelectedPiece.GetValidMoves().Contains(tilePosition))
            {
                // ตรวจสอบว่าตำแหน่งนี้มีศัตรูหรือไม่
                if (boardManager.IsEnemyAtPosition(tilePosition, boardManager.SelectedPiece.team))
                {
                    targetColor = attackColor; // สีแดงเมื่อเป็นศัตรู
                }
                else
                {
                    targetColor = validMoveColor; // สีเขียวสำหรับทางเดินปกติ
                }
            }
        }
        // อัปเดตสี
        if (!spriteRenderer.color.Equals(targetColor))
        {
            spriteRenderer.color = targetColor;
        }
    }

    public void SetTilePosition(Vector2Int position, ChessBoard manager)
    {
        tilePosition = position;
        boardManager = manager;
    }

    private void OnMouseDown()
    {
        if (PauseManager.isPaused) return;
        if (ChessBoard.Instance.IsPromoting()) return;

        // ✅ Online Mode Check
        if (GameManager.Instance.isOnlineMode && !GameManager.Instance.IsMyTurn())
        {
            return;
        }

        if (boardManager != null)
        {
            boardManager.OnTileClicked(tilePosition);
        }
    }

    private void OnMouseExit()
    {
        if (spriteRenderer != null &&
            !spriteRenderer.color.Equals(validMoveColor) &&
            !spriteRenderer.color.Equals(checkColor))
        {
            spriteRenderer.color = originalColor;
        }
    }

}
