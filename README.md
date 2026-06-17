# International_Chess_Games

<<<<<<< ours
> อ้างอิงจากโฟลเดอร์จริง `Assets/Scripts` ในโปรเจกต์

```
scripts/
├── GameAssembly.asmdef                          # Assembly definition ของเกมหลัก
├── AIEngine/                                    # ระบบ AI สำหรับหมากรุก
│   ├── Adapters/                                # ตัวกลางเชื่อมต่อ (Adapters)
│   │   └── UnityAIBoardAdapter.cs
│   ├── Algorithms/                              # อัลกอริทึมการค้นหา
│   │   ├── alphaBeta.cs
│   │   ├── MinMax.cs
│   │   └── SearchAlgorithm.cs
│   ├── Core/                                    # ระบบหลักของ AI
│   │   ├── AICore.cs
│   │   └── PerformanceTracker.cs
│   ├── Evaluation/                              # ระบบประเมินค่าตำแหน่ง
│   │   ├── Evaluation.cs
│   │   └── EvaluationSettings.cs
│   ├── Interfaces/                              # อินเทอร์เฟซของ AI
│   │   └── IChessAI.cs
│   ├── Models/                                  # โมเดลข้อมูลภายใน AI
│   │   ├── ChessBoardModel.cs
│   │   ├── MoveModel.cs
│   │   ├── MoveRecord.cs
│   │   └── MoveState.cs
│   └── Utilities/                               # เครื่องมือและยูทิลิตี้ของ AI
│       ├── AiPerformanceData.cs
│       ├── BoardConverter.cs
│       ├── MoveGenerator.cs
│       ├── MoveOrderer.cs
│       ├── SearchResult.cs
│       ├── TranspositionTable.cs
│       ├── TTEntry.cs
│       └── Zobrist.cs
├── DTO/                                         # Data Transfer Objects (ใช้สื่อสารกับ API/ระบบอื่น)
│   ├── AuthDTOs.cs
│   ├── GameDTOs.cs
│   ├── InviteDTOs.cs
│   ├── LeaderboardDTOs.cs
│   ├── MatchDTOs.cs
│   ├── MoveDtos.cs
│   └── UserDTOs.cs
├── Game/                                        # ระบบเกมหลัก
│   ├── Core/                                    # ระบบหลักของเกม
│   │   ├── ChessBoard.cs
│   │   ├── GameManager.cs
│   │   ├── MoveResult.cs
│   │   ├── PauseManager.cs
│   │   └── SettingManager.cs
│   ├── Mechanics/                               # กลไกการเล่นและการเดินหมาก
│   │   ├── HistoryMove.cs
│   │   ├── TileClick.cs
│   │   └── UndoMove.cs
│   ├── Onlie/                                   # ระบบออนไลน์ของเกม
│   │   ├── Managers/                            
│   │   │   ├── LeaderboardManager.cs
│   │   │   ├── MatchmakingManager.cs
│   │   │   └── PlayerSearchManager.cs
│   │   └── UI/                                  # UI สำหรับโหมดออนไลน์
│   │       ├── GameResultUI.cs
│   │       ├── LeaderboardRowUI.cs
│   │       ├── LoginUi.cs
│   │       ├── LogutUi.cs
│   │       ├── MatchmakingUi.cs
│   │       ├── PlayerRowUI.cs
│   │       └── RankingUI.cs
│   ├── Pieces/                                  # ตัวหมากรุก
│   │   └── ChessPiece.cs
│   └── UI/                                      # UI หลักของเกมออฟไลน์/ทั่วไป
│       ├── HistoryMoveUI.cs
│       └── PromotionManager.cs
├── Menu/                                        # เมนูหลักของเกม
│   └── MainMenuController.cs
├── Networking/                                  # ระบบเครือข่ายของเกม
│   └── GameNetworkHandler.cs
├── Service/                                     # เลเยอร์บริการเรียก API ฝั่งไคลเอนต์
│   └── api/
│       ├── AiPerformanceAPI.cs
│       ├── authApi.cs
│       ├── GameAPI.cs
│       ├── LeaderboardApi.cs
│       ├── MatchmakingApi.cs
│       ├── MovesAPI.cs
│       ├── profileApi.cs
│       ├── rankingAPI.cs
│       └── UserAPI.cs
└── Utils/                                       # ยูทิลิตี้ทั่วไปของเกม
    ├── CameraDebug.cs
    └── MoveMapper.cs
```
=======
เอกสารเริ่มต้นสำหรับผู้มาใหม่อยู่ที่ `docs/NEWCOMER_GUIDE_TH.md`
>>>>>>> theirs
