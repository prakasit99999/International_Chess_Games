using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChessPiece;
using static HistoryMove;

public class PromotionManager : MonoBehaviour
{

    public static PromotionManager Instance;

    private ChessPiece selectedPawn; // ตัวเบี้ยที่กำลังจะถูกเลื่อนขั้น
    private HistoryMove historyMove;
    private ChessPiece.PieceType selectedPromotionType;

    public GameObject promotionWhitePanel; // Panel สำหรับทีมขาว
    public GameObject promotionBlackPanel; // Panel สำหรับทีมดำ

    private void Awake()
    {
        if (Instance == null)
        {
            
            Instance = this;
            historyMove = FindObjectOfType<HistoryMove>();

        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (promotionWhitePanel == null || promotionBlackPanel == null)
        {
            Debug.LogError("❌ โปรดกำหนดค่า promotionWhitePanel และ promotionBlackPanel ใน Unity Inspector");
        }

        HidePromotionMenu();
    }

    public ChessPiece.PieceType GetSelectedPromotionType()
    {
        return selectedPromotionType;
    }
    // ซ่อนเมนูเลื่อนขั้น
    public void HidePromotionMenu()
    {

        if (promotionWhitePanel != null)
            promotionWhitePanel.SetActive(false);
        else
            Debug.LogError("❌ promotionWhitePanel เป็น null! ตรวจสอบว่า Panel มีอยู่หรือไม่");

        if (promotionBlackPanel != null)
            promotionBlackPanel.SetActive(false);
        else
            Debug.LogError("❌ promotionBlackPanel เป็น null! ตรวจสอบว่า Panel มีอยู่หรือไม่");
    }

    // แสดงเมนูเลื่อนขั้น
    public void ShowPromotionMenu(ChessPiece pawn)
    {
        if (pawn == null)
        {
            Debug.LogError("❌ pawn เป็น null! ตรวจสอบว่ามีการส่งเบี้ยมา");
            return;
        }
        selectedPawn = pawn;
        //Debug.Log($"🔼 แสดงเมนูเลื่อนขั้นสำหรับ {pawn.team} Pawn");

        if (pawn.team == ChessPiece.Team.White)
        {
            promotionWhitePanel.SetActive(true);
            Debug.Log("เปิด promotionWhitePanel");

        }
        else
        {
            promotionBlackPanel.SetActive(true);
            Debug.Log("เปิด promotionBlackPanel");
        }
    }

    // เลือกหมากที่ต้องการเลื่อนขั้น (0 = Queen, 1 = Rook, 2 = Bishop, 3 = Knight)
    public void OnPromotionButtonClicked(int choice)
    {
        if (selectedPawn == null)
        {
            Debug.LogError("❌ selectedPawn เป็น null! ตรวจสอบว่ามีการเลือกเบี้ยก่อนกดปุ่มเลื่อนขั้น");
            return;
        }

        if (PauseManager.isPaused) return;

        ChessPiece.PieceType newType = ChessPiece.PieceType.Queen; // ค่าเริ่มต้น

        switch (choice)
        {
            case 1: newType = ChessPiece.PieceType.Rook; break;
            case 2: newType = ChessPiece.PieceType.Knight; break;
            case 3: newType = ChessPiece.PieceType.Bishop; break;
            case 4: newType = ChessPiece.PieceType.Queen; break;
        }
        if (historyMove == null || historyMove.GetMoveHistory().Count == 0)
        {
            Debug.LogError("❌ ไม่พบประวัติการเดินหรือ HistoryMove ไม่ถูกกำหนดค่า!");
            return;
        }

        // ✅ อัปเดตข้อมูลใน HistoryMove ด้วยประเภทใหม่
        HistoryMoveData lastMove = historyMove.GetMoveHistory().Pop();
        lastMove.promotedTo = newType;
        historyMove.GetMoveHistory().Push(lastMove);

        //Debug.Log($"🔼 เลื่อนขั้นเป็น {newType}");
        selectedPawn.Promote(newType);
        selectedPawn = null;
        HidePromotionMenu();
        ChessBoard.Instance.SetPromoting(false);
        ChessBoard.Instance.SetselectedPiece(null);
        GameManager.Instance.SwitchTurn();

        Debug.Log($"🔼 อัปเดตประเภทที่เลื่อนขั้นใน HistoryMove: {lastMove.promotedTo}");

    }

}
