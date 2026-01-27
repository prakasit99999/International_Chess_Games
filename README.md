# International_Chess_Games

## โครงสร้างโฟลเดอร์และไฟล์

```
scripts/
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
│   ├── Interfaces/                              # อินเทอร์เฟซ
│   │   └── IChessAI.cs
│   ├── Models/                                  # โมเดลข้อมูล
│   │   ├── ChessBoardModel.cs
│   │   ├── MoveModel.cs
│   │   ├── MoveRecord.cs
│   │   └── MoveState.cs
│   └── Utilities/                               # เครื่องมือช่วยเหลือ
│       ├── AiPerformanceData.cs
│       ├── BoardConverter.cs
│       ├── MoveGenerator.cs
│       ├── MoveOrderer.cs
│       ├── SearchResult.cs
│       ├── TranspositionTable.cs
│       ├── TTEntry.cs
│       └── Zobrist.cs
├── DTO/                                         # Data Transfer Objects
│   ├── AuthDTOs.cs
│   ├── GameDTOs.cs
│   ├── MatchDTOs.cs
│   └── MoveDtos.cs
├── Game/                                        # ระบบเกมหลัก
│   ├── Core/                                    # ระบบหลักของเกม
│   │   ├── ChessBoard.cs
│   │   ├── GameManager.cs
│   │   ├── PauseManager.cs
│   │   └── SettingManager.cs
│   ├── Mechanics/                               # กลไกการเล่น
│   │   ├── HistoryMove.cs
│   │   ├── TileClick.cs
│   │   └── UndoMove.cs
│   ├── OnlieUI/                                 # UI สำหรับเกมออนไลน์
│   │   └── UI/
│   │       ├── GameResultUI.cs
│   │       ├── LoginUi.cs
│   │       ├── LogutUi.cs
│   │       ├── MatchmakingManager.cs
│   │       ├── MatchmakingUi.cs
│   │       └── RankingUI.cs
│   ├── Pieces/                                  # ตัวหมากรุก
│   │   └── ChessPiece.cs
│   └── UI/                                      # UI หลัก
│       ├── HistoryMoveUI.cs
│       └── PromotionManager.cs
├── Menu/                                        # เมนูหลัก
│   └── MainMenuController.cs
├── Networking/                                  # ระบบเครือข่าย
│   └── GameNetworkHandler.cs
├── Service/                                     # บริการ API
│   └── api/
│       ├── AiPerformanceAPI.cs
│       ├── authApi.cs
│       ├── GameAPI.cs
│       ├── MatchmakingApi.cs
│       ├── MovesAPI.cs
│       └── profileApi.cs
└── Utils/                                       # เครื่องมือช่วยเหลือทั่วไป
    ├── CameraDebug.cs
    └── MoveMapper.cs
```
