using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(ChessBoard))]
public class ChessBoardGenerate : MonoBehaviour
{
    [SerializeField] private ChessBoard board;

    private Dictionary<Vector2Int, TileClick> tileClickMap = new Dictionary<Vector2Int, TileClick>();
    private bool isBoardGenerated = false;

    public Transform boardLabels;
    public GameObject tilePrefab;
    public GameObject textPrefab;

    public Color32 whitleColor = new Color32(255, 255, 255, 255);
    public Color32 blackColor = new Color32(0, 0, 0, 255);

    private void Awake()
    {
        if (board == null)
        {
            board = GetComponent<ChessBoard>();
        }
    }

    private void Start()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        if (board == null)
        {
            Debug.LogError("ChessBoardGenerate requires a ChessBoard reference.");
            return;
        }

        if (isBoardGenerated)
        {
            Debug.LogWarning("Board already generated. Skipping.");
            return;
        }

        GenerateBoard();
        GenerateBoardLabels();
        SpawnPieces();

        isBoardGenerated = true;
        Debug.Log("Board initialized");
    }

    public void RebuildBoard()
    {
        isBoardGenerated = false;
        InitializeBoard();
    }

    private void GenerateBoard()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("tilePrefab is not assigned on ChessBoardGenerate.");
            return;
        }

        float tileSize = board.tileSize;

        for (int x = 0; x < ChessBoard.BoardSize; x++)
        {
            for (int y = 0; y < ChessBoard.BoardSize; y++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x * tileSize, y * tileSize, 0), Quaternion.identity);
                tile.transform.parent = transform;

                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                renderer.color = (x + y) % 2 == 0 ? whitleColor : blackColor;

                string column = ((char)('A' + x)).ToString();
                string row = (y + 1).ToString();
                tile.name = column + row;

                BoxCollider2D boxCollider2D = tile.GetComponent<BoxCollider2D>();
                if (boxCollider2D == null)
                {
                    boxCollider2D = tile.AddComponent<BoxCollider2D>();
                }
                boxCollider2D.enabled = true;

                TileClick tileClick = tile.AddComponent<TileClick>();
                tileClick.SetTilePosition(new Vector2Int(x, y), board);
                tileClickMap[new Vector2Int(x, y)] = tileClick;
            }
        }
    }

    private void GenerateBoardLabels()
    {
        if (textPrefab == null || boardLabels == null)
        {
            return;
        }

        float tileSize = board.tileSize;
        float centerOffset = tileSize / 2f;

        for (int x = 0; x < ChessBoard.BoardSize; x++)
        {
            string label = ((char)('A' + x)).ToString();
            Vector3 localPos = new Vector3(x * tileSize + centerOffset, -centerOffset, 0f);
            Vector3 pos = transform.TransformPoint(localPos);

            GameObject labelObj = Instantiate(textPrefab, pos, Quaternion.identity, boardLabels);
            labelObj.name = "Label_" + label;
            TMP_Text text = labelObj.GetComponent<TMP_Text>();
            text.text = label;
            text.alignment = TextAlignmentOptions.BaselineLeft;
        }

        for (int y = 0; y < ChessBoard.BoardSize; y++)
        {
            string label = (y + 1).ToString();
            Vector3 localPos = new Vector3(-centerOffset, y * tileSize + centerOffset, 0f);
            Vector3 pos = transform.TransformPoint(localPos);

            GameObject labelObj = Instantiate(textPrefab, pos, Quaternion.identity, boardLabels);
            TMP_Text text = labelObj.GetComponent<TMP_Text>();
            labelObj.name = "Label_" + label;
            text.text = label;
            text.alignment = TextAlignmentOptions.CaplineRight;
        }
    }

    private void SpawnPieces()
    {
        // if (tilePrefab == null)
        // {
        //     Debug.LogError("❌ tilePrefab ยังไม่ได้เซ็ตใน ChessBoard!");
        //     return;
        // }
        // // 🏇 วางเบี้ย (Pawn) ที่แถว 1 และ 6
        // for (int i = 0; i < ChessBoard.BoardSize; i++)
        // {
        //     board.SpawnPiece(ChessPiece.PieceType.Pawn, ChessPiece.Team.White, new Vector2Int(i, 1));
        //     board.SpawnPiece(ChessPiece.PieceType.Pawn, ChessPiece.Team.Black, new Vector2Int(i, 6));
        // }

        // // 🏰 วางเรือ (Rook)
        // board.SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.White, new Vector2Int(0, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.White, new Vector2Int(7, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.Black, new Vector2Int(0, 7));
        // board.SpawnPiece(ChessPiece.PieceType.Rook, ChessPiece.Team.Black, new Vector2Int(7, 7));

        // //// 🏇 วางม้า (Knight)
        // board.SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.White, new Vector2Int(1, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.White, new Vector2Int(6, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.Black, new Vector2Int(1, 7));
        // board.SpawnPiece(ChessPiece.PieceType.Knight, ChessPiece.Team.Black, new Vector2Int(6, 7));

        // //// 🏹 วางบิชอป (Bishop)
        // board.SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.White, new Vector2Int(2, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.White, new Vector2Int(5, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.Black, new Vector2Int(2, 7));
        // board.SpawnPiece(ChessPiece.PieceType.Bishop, ChessPiece.Team.Black, new Vector2Int(5, 7));

        // // 👑 วางควีน (Queen)
        // board.SpawnPiece(ChessPiece.PieceType.Queen, ChessPiece.Team.White, new Vector2Int(3, 0));
        // board.SpawnPiece(ChessPiece.PieceType.Queen, ChessPiece.Team.Black, new Vector2Int(3, 7));

        // // 🤴 วางคิง (King)
        board.SpawnPiece(ChessPiece.PieceType.King, ChessPiece.Team.White, new Vector2Int(4, 0));
        board.SpawnPiece(ChessPiece.PieceType.King, ChessPiece.Team.Black, new Vector2Int(4, 7));

        board.SpawnPiece(ChessPiece.PieceType.Pawn, ChessPiece.Team.White, new Vector2Int(1, 6));
        board.SpawnPiece(ChessPiece.PieceType.Pawn, ChessPiece.Team.Black, new Vector2Int(1, 1));
    }
}
