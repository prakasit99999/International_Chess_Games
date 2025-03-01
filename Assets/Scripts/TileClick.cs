using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileClick : MonoBehaviour
{
    private ChessBoard boardManager;
    private Vector2Int tilePosition;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f); // สีเมื่อนำเมาส์ชี้
    public Color validMoveColor = new Color(0.5f, 1f, 0.5f, 1f); // สีเขียวอ่อนสำหรับทางเดินที่ถูกต้อง
    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color; // เก็บสีเดิมของแผ่น
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetTilePosition(Vector2Int position, ChessBoard manager)
    {
        tilePosition = position;
        boardManager = manager;
    }

    private void OnMouseDown()
    {
        if (boardManager != null)
        {
            boardManager.OnTileClicked(tilePosition);  // เรียกไปที่ `ChessBoard`
        }
    }
    private void OnMouseEnter()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
        spriteRenderer.color = hoverColor;
    }

    private void OnMouseEnter()
    {
        if (spriteRenderer.color != validMoveColor) // ไม่อัปเดตสีถ้าเป็นทางเดินถูกต้องอยู่แล้ว
        {
            spriteRenderer.color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (spriteRenderer.color != validMoveColor) // ไม่อัปเดตสีถ้าเป็นทางเดินถูกต้องอยู่แล้ว
        {
            spriteRenderer.color = originalColor;
        }
    }



}
