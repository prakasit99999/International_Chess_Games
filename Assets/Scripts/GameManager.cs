using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChessPiece;

public class GameManager : MonoBehaviour
{
    private bool gameIsOver = false;
    private ChessBoard chessBoard;
    private PromotionManager promotionManager; // เชื่อมกับ PromotionManager ใน Inspector
    private ChessPiece.Team currentTurn = ChessPiece.Team.White;

    public static GameManager Instance;

    public GameObject whitePlayer;
    public GameObject blackPlayer;
    public bool isGameStarted;
    public bool isWhiteTurn;
    public bool isBlackTurn;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        chessBoard = FindObjectOfType<ChessBoard>();
        if (chessBoard == null)
        {
            Debug.LogError("❌ ChessBoard ไม่ถูกพบ! ตรวจสอบว่า ChessBoard อยู่ในฉาก");
        }
        else
        {
            chessBoard.SetGameManager(this); // Set the gameManager instance in ChessBoard
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    //set method
    public void SetGameStarted(bool value)
    {
        isGameStarted = value;
    }

    //Get method
    // ตรวจสอบว่าตอนนี้เป็นตาของทีมไหน
    public ChessPiece.Team GetCurrentTurn()
    {
        return currentTurn;
    }

    // ฟังก์ชันสลับเทิร์น
    public void SwitchTurn()
    {
        if (IsGameOver())
        {
            Debug.Log("เกมจบแล้ว ไม่สามารถสลับเทิร์นได้");
            return;
        }
        if (ChessBoard.Instance.IsPromoting())
        {
            Debug.Log("กำลังเลื่อนขั้น ไม่สามารถสลับเทิร์นได้");
            return;
        }

        currentTurn = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;
        Debug.Log($"🕒 ตอนนี้เป็นตาของ {currentTurn}");

        CheckGameState(); // ✅ ตรวจสอบสถานะเกมทุกครั้งที่เปลี่ยนตาเดิน
    }


    // ฟังก์ชันตรวจสอบ Checkmate หรือ Stalemate
    public void CheckGameState()
    {
        Debug.Log("🔍 ตรวจสอบสถานะเกม...");

        ChessPiece.Team opponentTeam = (currentTurn == ChessPiece.Team.White) ? ChessPiece.Team.Black : ChessPiece.Team.White;

        if (ChessBoard.Instance.IsKingInCheckmate(opponentTeam))
        {
            Debug.Log($"♟️ Checkmate! {currentTurn} ชนะเกม!");
            GameOver(currentTurn);
            return;
        }
        else if (ChessBoard.Instance.IsStalemate(opponentTeam))
        {
            Debug.Log("⚖️ Stalemate! เกมเสมอ!");
            GameOver(ChessPiece.Team.None); // ใช้ทีม "None" เพื่อบอกว่าเกมเสมอ
            return;
        }
    }

    public void GameOver(ChessPiece.Team winningTeam)
    {
        if (gameIsOver) return;

        gameIsOver = true;
        Debug.Log($"🎉 เกมจบแล้ว! {winningTeam} เป็นฝ่ายชนะ!");

        if (chessBoard == null)
        {
            Debug.LogError("❌ ChessBoard เป็น null! ตรวจสอบว่า ChessBoard อยู่ในฉาก");
            return;
        }

        foreach (var entry in chessBoard.GetPiecesOnBoard())
        {
            if (entry.Value != null)
            {
                entry.Value.enabled = false;
            }
        }
    }

    public bool IsGameOver()
    {
        return gameIsOver;
    }


}
