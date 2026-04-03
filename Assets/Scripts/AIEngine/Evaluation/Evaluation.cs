using System;
using System.Collections.Generic;
using AIEngine.Utilities;

namespace AIEngine.Evaluation
{
    public class Evaluation
    {
        // ==========================================
        // CONSTANTS & CONFIGURATION (PeSTO / Kaufman)
        // ==========================================
        private const int KingValue = 20000;

        // Phase Calculation: ใช้สำหรับ Tapered Evaluation
        // Total Phase = 16 (Pawn ไม่นับ) + 4 (Knights) + 4 (Bishops) + 4 (Rooks) + 2 (Queens) = 24? 
        // ปกติใช้ค่า: N=1, B=1, R=2, Q=4. Total = 4+4+4+4 = 16 (ไม่นับ King/Pawn)
        private const int PhaseTotal = 24;
        private static readonly int[] PhaseWeights = { 0, 1, 1, 2, 4, 0 }; // Pawn, N, B, R, Q, K

        // ตารางคะแนนตำแหน่ง (Middle Game vs End Game)
        // ค่าเหล่านี้อ้างอิง Simplified PeSTO tables เพื่อประสิทธิภาพ
        private static readonly int[] MgPawnTable = {
              0,   0,   0,   0,   0,   0,   0,   0,
             50,  50,  50,  50,  50,  50,  50,  50,
             10,  10,  20,  30,  30,  20,  10,  10,
              5,   5,  10,  25,  25,  10,   5,   5,
              0,   0,   0,  20,  20,   0,   0,   0,
              5,  -5, -10,   0,   0, -10,  -5,   5,
              5,  10,  10, -20, -20,  10,  10,   5,
              0,   0,   0,   0,   0,   0,   0,   0
        };

        private static readonly int[] EgPawnTable = {
              0,   0,   0,   0,   0,   0,   0,   0,
             80,  80,  80,  80,  80,  80,  80,  80,
             50,  50,  50,  50,  50,  50,  50,  50,
             30,  30,  30,  30,  30,  30,  30,  30,
             20,  20,  20,  20,  20,  20,  20,  20,
             10,  10,  10,  10,  10,  10,  10,  10,
             10,  10,  10,  10,  10,  10,  10,  10,
              0,   0,   0,   0,   0,   0,   0,   0
        };

        private static readonly int[] MgBishopTable = {
        -20, -10, -10, -10, -10, -10, -10, -20,
        -10,   5,   0,   0,   0,   0,   5, -10,
        -10,  10,  10,  10,  10,  10,  10, -10,
        -10,   0,  10,  10,  10,  10,   0, -10,
        -10,   5,   5,  10,  10,   5,   5, -10,
        -10,   0,   5,  10,  10,   5,   0, -10,
        -10,   0,   0,   0,   0,   0,   0, -10,
        -20, -10, -10, -10, -10, -10, -10, -20
        };

        private static readonly int[] EgBishopTable = {
        -10,  -5,  -5,  -5,  -5,  -5,  -5, -10,
        -5,  10,   5,   5,   5,   5,  10,  -5,
        -5,   5,  15,  15,  15,  15,   5,  -5,
        -5,   5,  15,  20,  20,  15,   5,  -5,
        -5,   5,  15,  20,  20,  15,   5,  -5,
        -5,   5,  10,  15,  15,  10,   5,  -5,
        -5,   5,   0,   0,   0,   0,   5,  -5,
        -10,  -5,  -5,  -5,  -5,  -5,  -5, -10
        };

        private static readonly int[] MgRookTable = {
        0,   0,   5,  10,  10,   5,   0,   0,
        -5,   0,   0,   0,   0,   0,   0,  -5,
        -5,   0,   0,   0,   0,   0,   0,  -5,
        -5,   0,   0,   0,   0,   0,   0,  -5,
        -5,   0,   0,   0,   0,   0,   0,  -5,
        -5,   0,   0,   0,   0,   0,   0,  -5,
        5,  10,  10,  10,  10,  10,  10,   5,
        0,   0,   0,   0,   0,   0,   0,   0
};

        private static readonly int[] EgRookTable = {
        0,   0,   5,  10,  10,   5,   0,   0,
        -5,   0,   0,   5,   5,   0,   0,  -5,
        -5,   0,   0,   5,   5,   0,   0,  -5,
        -5,   0,   0,   5,   5,   0,   0,  -5,
        -5,   0,   0,   5,   5,   0,   0,  -5,
        -5,   0,   0,   5,   5,   0,   0,  -5,
        5,  10,  10,  15,  15,  10,  10,   5,
        0,   0,   0,   5,   5,   0,   0,   0
        };

        private static readonly int[] MgQueenTable = {
        -20, -10, -10,  -5,  -5, -10, -10, -20,
        -10,   0,   5,   0,   0,   0,   0, -10,
        -10,   5,   5,   5,   5,   5,   0, -10,
        -5,   0,   5,   5,   5,   5,   0,  -5,
         0,   0,   5,   5,   5,   5,   0,  -5,
        -10,   5,   5,   5,   5,   5,   0, -10,
        -10,   0,   5,   0,   0,   0,   0, -10,
        -20, -10, -10,  -5,  -5, -10, -10, -20
    };

        private static readonly int[] EgQueenTable = {
        -10,  -5,  -5,  -5,  -5,  -5,  -5, -10,
        -5,   5,   5,   5,   5,   5,   5,  -5,
        -5,   5,  10,  10,  10,  10,   5,  -5,
        -5,   5,  10,  15,  15,  10,   5,  -5,
        -5,   5,  10,  15,  15,  10,   5,  -5,
        -5,   5,  10,  10,  10,  10,   5,  -5,
        -5,   5,   5,   5,   5,   5,   5,  -5,
        -10,  -5,  -5,  -5,  -5,  -5,  -5, -10
        };

        private static readonly int[] MgKnightTable = {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20,   0,   0,   0,   0, -20, -40,
            -30,   0,  10,  15,  15,  10,   0, -30,
            -30,   5,  15,  20,  20,  15,   5, -30,
            -30,   0,  15,  20,  20,  15,   0, -30,
            -30,   5,  10,  15,  15,  10,   5, -30,
            -40, -20,   0,   5,   5,   0, -20, -40,
            -50, -40, -30, -30, -30, -30, -40, -50
        };

        // King Safety Table (Middle Game): อยากให้ King อยู่มุม
        private static readonly int[] MgKingTable = {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
             20,  20,   0,   0,   0,   0,  20,  20,
             20,  30,  10,   0,   0,  10,  30,  20
        };

        // King Activity Table (End Game): King ต้องเดินเข้ากลางกระดาน
        private static readonly int[] EgKingTable = {
            -50, -40, -30, -20, -20, -30, -40, -50,
            -30, -20, -10,   0,   0, -10, -20, -30,
            -30, -10,  20,  30,  30,  20, -10, -30,
            -30, -10,  30,  40,  40,  30, -10, -30,
            -30, -10,  30,  40,  40,  30, -10, -30,
            -30, -10,  20,  30,  30,  20, -10, -30,
            -30, -30,   0,   0,   0,   0, -30, -30,
            -50, -30, -30, -30, -30, -30, -30, -50
        };


        // ==========================================
        // MAIN EVALUATION FUNCTION
        // ==========================================
        public static float Evaluate(ChessBoardModel board, EvaluationSettings settings = null)
        {
            // 1. Check for Draw conditions first (Optimization)
            if (board.FiftyMoveCounter >= 100) return 0f; // Draw by 50 move rule

            // 1. Setup Settings (Use default if null)
            if (settings == null) settings = new EvaluationSettings();

            // Cache values for performance
            int pawnVal = settings.PawnValue;
            int knightVal = settings.KnightValue;
            int bishopVal = settings.BishopValue;
            int rookVal = settings.RookValue;
            int queenVal = settings.QueenValue;
            int passedPawnBonus = settings.PassedPawnBonus;
            int isolatedPawnPenalty = settings.IsolatedPawnPenalty;
            float positionalFactor = settings.PositionalFactor;

            float mgScore = 0f; // คะแนนช่วง Middle Game
            float egScore = 0f; // คะแนนช่วง End Game
            int phase = 0;   // ตัวนับ Phase ของเกม

            // 2. Loop through board once (Single Pass Efficiency)
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int piece = board.Board[x, y];
                    if (piece == 0) continue;

                    int absPiece = Math.Abs(piece);
                    int pieceTypeIndex = absPiece - 1; // 0=Pawn, 1=Knight...
                    bool isWhite = piece > 0;

                    // A. Material Score
                    float materialValue = 0;
                    switch (absPiece)
                    {
                        case 1: materialValue = pawnVal; break;
                        case 2: materialValue = knightVal; break;
                        case 3: materialValue = bishopVal; break;
                        case 4: materialValue = rookVal; break;
                        case 5: materialValue = queenVal; break;
                        case 6: materialValue = KingValue; break;
                    }
                    if (isWhite) { mgScore += materialValue; egScore += materialValue; }
                    else { mgScore -= materialValue; egScore -= materialValue; }

                    // B. Phase Calculation (นับถอยหลัง Material)
                    // ถ้าไม่ใช่ Pawn หรือ King ให้บวกค่า Phase
                    if (absPiece != 1 && absPiece != 6)
                    {
                        phase += PhaseWeights[pieceTypeIndex];
                    }

                    // C. Positional Score (PST)
                    // ต้อง Flip index สำหรับสีดำ (Mirror)
                    // x (rank): 0-7. สีขาวเริ่มแถว 7, สีดำเริ่มแถว 0 (ใน Model ของคุณ ดูเหมือน 0 คือแถวบน)
                    // สมมติ: board[0,0] คือ a8 (มุมดำ), board[7,0] คือ a1 (มุมขาว) 
                    // ดังนั้น White Index = table[x * 8 + y], Black Index = table[(7-x) * 8 + y]

                    int tableIdx = isWhite ? (x * 8 + y) : ((7 - x) * 8 + y);

                    // เลือกใช้ตารางคะแนนตามประเภทหมาก
                    // (ตัวอย่างใช้ตาราง Pawn/King เพื่อความกระชับ คุณควรเพิ่มตาราง Knight/Bishop/Rook/Queen ให้ครบ)
                    float mgPst = 0f, egPst = 0f;

                    switch (absPiece)
                    {
                        case 1: // Pawn
                            mgPst = MgPawnTable[tableIdx];
                            egPst = EgPawnTable[tableIdx];
                            float structScore = EvaluatePawnStructure(board, x, y, isWhite, passedPawnBonus, isolatedPawnPenalty, settings.DoubledPawnPenalty);
                            if (isWhite) { mgScore += structScore; egScore += structScore; }
                            else { mgScore -= structScore; egScore -= structScore; }
                            break;
                        case 2: // Knight
                            mgPst = MgKnightTable[tableIdx];
                            egPst = MgKnightTable[tableIdx]; // Knight ไม่ค่อยต่างมาก
                            break;

                        case 3: // Bishop
                            mgPst = MgBishopTable[tableIdx];
                            egPst = EgBishopTable[tableIdx];
                            break;
                        case 4: // Rook
                            mgPst = MgRookTable[tableIdx];
                            egPst = EgRookTable[tableIdx];
                            break;
                        case 5: // Queen
                            mgPst = MgQueenTable[tableIdx];
                            egPst = EgQueenTable[tableIdx];
                            break;

                        case 6: // King
                            mgPst = MgKingTable[tableIdx];
                            egPst = EgKingTable[tableIdx];
                            break;
                        default:
                            break;
                    }

                    if (isWhite) { mgScore += mgPst * positionalFactor; egScore += egPst * positionalFactor; }
                    else { mgScore -= mgPst * positionalFactor; egScore -= egPst * positionalFactor; }
                }
            }

            // 2.5 Advanced Features (สามารถเปิด/ปิดได้ตาม Difficulty)
            // precompute legal moves per side once เพื่อลดภาระใน evaluation ระหว่าง search
            List<MoveModel> whiteMoves = null;
            List<MoveModel> blackMoves = null;
            bool needWhiteMoves = settings.UseMobility || settings.UseThreats;
            bool needBlackMoves = settings.UseMobility || settings.UseThreats;
            if (needWhiteMoves) whiteMoves = GetMovesForSide(board, true);
            if (needBlackMoves) blackMoves = GetMovesForSide(board, false);

            if (settings.UseMobility)
            {
                mgScore += EvaluateMobility(whiteMoves) * settings.MobilityWeight;
                mgScore -= EvaluateMobility(blackMoves) * settings.MobilityWeight;
            }

            if (settings.UseBishopPair)
            {
                mgScore += HasBishopPair(board, true) ? settings.BishopPairBonus : 0;
                mgScore -= HasBishopPair(board, false) ? settings.BishopPairBonus : 0;
            }

            if (settings.UseRookFiles)
            {
                mgScore += EvaluateRookFiles(board, true, settings);
                mgScore -= EvaluateRookFiles(board, false, settings);
            }

            if (settings.UseOutpost)
            {
                mgScore += EvaluateKnightOutposts(board, true, settings.KnightOutpostBonus);
                mgScore -= EvaluateKnightOutposts(board, false, settings.KnightOutpostBonus);
            }

            if (settings.UseSpace)
            {
                mgScore += EvaluateSpace(board, true) * settings.SpaceWeight;
                mgScore -= EvaluateSpace(board, false) * settings.SpaceWeight;
            }

            if (settings.UseKingSafety)
            {
                mgScore += EvaluateKingSafety(board, true, settings);
                mgScore -= EvaluateKingSafety(board, false, settings);
            }

            if (settings.UseThreats)
            {
                mgScore += EvaluateHangingPieces(board, true, settings.HangingPiecePenalty, whiteMoves, blackMoves);
                mgScore -= EvaluateHangingPieces(board, false, settings.HangingPiecePenalty, blackMoves, whiteMoves);
            }


            // 3. Tapered Evaluation Calculation
            // Phase ยิ่งมาก = ยิ่งใกล้ต้นเกม, Phase น้อย = ท้ายเกม
            // Formula: (MG * phase + EG * (24 - phase)) / 24
            phase = Math.Min(phase, PhaseTotal); // Clamp value

            float finalScore = ((mgScore * phase) + (egScore * (PhaseTotal - phase))) / (float)PhaseTotal;

            // 4.Insufficient material
            if (IsInsufficientMaterial(board))
            {
                finalScore = 0f; // เสมอแน่นอน
            }
            

            // 5. Side to Move Bonus (Tempo) + Attack Bonus
            // การได้เดินก่อนมีค่าเล็กน้อย (เช่น 10-20 คะแนน) + Settings TempoBonus
            if (settings.UseTempo)
            {
                finalScore += board.IsWhiteTurn
                    ? settings.TempoBonus
                    : -settings.TempoBonus;
            }

            // Return relative score (Perspective)
            return finalScore;
        }

        // ==========================================
        // PAWN STRUCTURE & PASSED PAWNS
        // ==========================================
        private static float EvaluatePawnStructure(ChessBoardModel board, int x, int y, bool isWhite, int passedBonus, int isolatedPenalty, int doubledPenalty)
        {
            float score = 0f;
            int forwardDir = isWhite ? -1 : 1; // สมมติขาวเดินขึ้น (Index ลดลง) หรือลง แล้วแต่ Model
            // หมายเหตุ: ต้องเช็คทิศทางของ Board Model ให้ชัวร์ (ปกติ 0=Top/Black, 7=Bottom/White)

            // 1. Isolated Pawn (เบี้ยโดดเดี่ยว ไม่มีเพื่อนในไฟล์ข้างๆ)
            bool leftFileHasPawn = HasPawnOnFile(board, y - 1, isWhite);
            bool rightFileHasPawn = HasPawnOnFile(board, y + 1, isWhite);

            if (!leftFileHasPawn && !rightFileHasPawn)
            {
                score += isolatedPenalty; // Penalty is usually negative in settings, so add it
            }

            // 2. Passed Pawn (เบี้ยผ่าน: ไม่มีเบี้ยศัตรูขวางหน้า ในไฟล์เดียวกันและไฟล์ข้างๆ)
            // นี่คือ Key สำคัญของ Endgame
            if (IsPassedPawn(board, x, y, isWhite))
            {
                // ยิ่งใกล้ฝั่งตรงข้าม ยิ่งได้คะแนนเยอะ
                float rankBonus = isWhite ? (7 - x) * 10f : x * 10f;
                score += (passedBonus + rankBonus);
            }
            // 3. Doubled Pawn (เบี้ยซ้อนกันในไฟล์เดียว)
            int pawnCountOnFile = CountPawnsOnFile(board, y, isWhite);
            if (pawnCountOnFile > 1)
            {
                score += (pawnCountOnFile - 1) * doubledPenalty; // โทษเบี้ยซ้อน (ปรับได้ใน settings)
            }
            return score;
        }

        private static bool IsInsufficientMaterial(ChessBoardModel board)
        {
            int nonKingPieces = 0;

            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    int p = Math.Abs(board.Board[x, y]);
                    if (p != 0 && p != 6) // 6 = King
                        nonKingPieces++;
                }

            return nonKingPieces == 0;
        }

        private static bool HasPawnOnFile(ChessBoardModel board, int fileY, bool isWhite)
        {
            if (fileY < 0 || fileY > 7) return false;
            int pawnVal = isWhite ? 1 : -1;

            for (int r = 0; r < 8; r++)
            {
                if (board.Board[r, fileY] == pawnVal) return true;
            }
            return false;
        }

        private static bool IsPassedPawn(ChessBoardModel board, int r, int c, bool isWhite)
        {
            int enemyPawn = isWhite ? -1 : 1;

            // เช็คช่องข้างหน้าทั้งหมดในไฟล์ตัวเอง (c) และไฟล์ข้างๆ (c-1, c+1)
            // ถ้า White เดินจาก 7 -> 0, ศัตรูจะอยู่ row < r
            // ถ้า Black เดินจาก 0 -> 7, ศัตรูจะอยู่ row > r
            int startRow = isWhite ? 0 : r + 1;
            int endRow = isWhite ? r - 1 : 7;

            // Loop เช็คแถวหน้าเบี้ย
            for (int i = isWhite ? r - 1 : r + 1;
                 isWhite ? i >= 0 : i <= 7;
                 i += (isWhite ? -1 : 1))
            {
                if (board.Board[i, c] == enemyPawn) return false; // Blocked
                if (c > 0 && board.Board[i, c - 1] == enemyPawn) return false; // Control Left
                if (c < 7 && board.Board[i, c + 1] == enemyPawn) return false; // Control Right
            }
            return true;
        }

        private static int CountPawnsOnFile(ChessBoardModel board, int fileY, bool isWhite)
        {
            int pawnVal = isWhite ? 1 : -1;
            int count = 0;
            for (int r = 0; r < 8; r++)
            {
                if (board.Board[r, fileY] == pawnVal) count++;
            }
            return count;
        }

        // Mobility Evaluation: นับจำนวนช่องว่างที่หมากสามารถเดินได้ (ไม่รวมที่ถูกบล็อก)
        private static int EvaluateMobility(List<MoveModel> moves)
        {
            int score = 0; // สามารถปรับแต่งได้ เช่น แยกตามประเภทหมาก หรือให้คะแนนพิเศษสำหรับการควบคุมศูนย์กลาง
            foreach (var move in moves)
            {
                // ตัวอย่างง่ายๆ: ให้คะแนน 1 คะแนนต่อการเดินที่ถูกต้อง
                score++;
            }
            return score;
        }

        //Has Bishop Pair Bonus (ถ้ามีบิชอปคู่) 
        private static bool HasBishopPair(ChessBoardModel board, bool forWhite)
        {
            int bishop = forWhite ? 3 : -3;
            int count = 0;
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                    if (board.Board[x, y] == bishop) count++;
            return count >= 2;
        }

        //Evaluate Knight Outposts (ถ้าอัศวินอยู่ในตำแหน่งที่ดี เช่น มีเบี้ยป้องกันและไม่มีเบี้ยศัตรูคุกคาม จะได้คะแนนพิเศษ)
        private static int EvaluateKnightOutposts(ChessBoardModel board, bool forWhite, int outpostBonus)
        {
            int knight = forWhite ? 2 : -2;
            int ownPawn = forWhite ? 1 : -1;
            int enemyPawn = -ownPawn;
            int dir = forWhite ? -1 : 1;
            int score = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.Board[x, y] != knight) continue;

                    bool defendedByPawn = false;
                    int backRow = x - dir;
                    if (backRow >= 0 && backRow <= 7)
                    {
                        if (y > 0 && board.Board[backRow, y - 1] == ownPawn) defendedByPawn = true;
                        if (y < 7 && board.Board[backRow, y + 1] == ownPawn) defendedByPawn = true;
                    }

                    bool attackedByEnemyPawn = false;
                    int enemyPawnRow = x + dir;
                    if (enemyPawnRow >= 0 && enemyPawnRow <= 7)
                    {
                        if (y > 0 && board.Board[enemyPawnRow, y - 1] == enemyPawn) attackedByEnemyPawn = true;
                        if (y < 7 && board.Board[enemyPawnRow, y + 1] == enemyPawn) attackedByEnemyPawn = true;
                    }

                    if (defendedByPawn && !attackedByEnemyPawn)
                        score += outpostBonus;
                }
            }
            return score;
        }


        //Rook Files (ถ้า รุก  อยู่บนไฟล์ที่ไม่มีเบี้ยขวางทาง จะได้คะแนนพิเศษ)
        private static int EvaluateRookFiles(ChessBoardModel board, bool forWhite, EvaluationSettings settings)
        {
            int rook = forWhite ? 4 : -4;
            int ownPawn = forWhite ? 1 : -1;
            int enemyPawn = -ownPawn;
            int bonus = 0;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.Board[x, y] != rook) continue;
                    bool ownPawnInFile = false;
                    bool enemyPawnInFile = false;
                    for (int r = 0; r < 8; r++)
                    {
                        if (board.Board[r, y] == ownPawn) ownPawnInFile = true;
                        if (board.Board[r, y] == enemyPawn) enemyPawnInFile = true;
                    }
                    // Rook on Open File  : ไฟล์ที่ไม่มีเบี้ยของทั้งสองฝ่ายอยู่เลย
                    if (!ownPawnInFile && !enemyPawnInFile)
                    {
                        bonus += settings.RookOpenFileBonus;
                    }
                    else if (!ownPawnInFile) // Rook on Semi-Open File : ไฟล์ที่ไม่มีเบี้ยของฝ่ายตัวเอง แต่มีเบี้ยศัตรูอยู่
                    {
                        bonus += settings.RookSemiOpenFileBonus;
                    }
                }
            }
            return bonus;
        }

        // Evaluate Space (ประเมินพื้นที่ที่ฝ่ายนั้นๆ ควบคุมอยู่ เช่น ถ้าขาวควบคุมแถว 4-5 จะได้คะแนนพิเศษ เพราะมีพื้นที่ให้เดินมากขึ้น)
        private static int EvaluateSpace(ChessBoardModel board, bool forWhite)
        {
            int score = 0;
            int start = forWhite ? 0 : 4;
            int end = forWhite ? 3 : 7;

            for (int x = start; x <= end; x++)
            {
                for (int y = 2; y <= 5; y++)
                {
                    int p = board.Board[x, y];
                    if (p == 0) continue;
                    if (forWhite && p > 0) score++;
                    if (!forWhite && p < 0) score++;
                }
            }
            return score;
        }
        //King Safety Evaluation: ประเมินความปลอดภัยของ King โดยดูจากตำแหน่งของ King และการป้องกันรอบๆ (เช่น Pawn Shield, Tropism, Hanging Pieces ใกล้ King)
        private static int EvaluateKingSafety(ChessBoardModel board, bool forWhite, EvaluationSettings settings)
        {
            var king = FindKing(board, forWhite);
            if (king.X < 0) return 0; // King not found
            int ownPawn = forWhite ? 1 : -1;
            int score = 0;
            int dir = forWhite ? -1 : 1;
            int shieldRow = king.X + dir;
            // Pawn Shield Bonus: ถ้ามีเบี้ยอยู่หน้าราชา (เช่น แถว 6 สำหรับขาว, แถว 1 สำหรับดำ) จะได้คะแนนพิเศษ
            if (shieldRow >= 0 && shieldRow <= 7)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int y = king.Y + dy;
                    if (y < 0 || y > 7) continue;
                    if (board.Board[shieldRow, y] == ownPawn) score += settings.PawnShieldBonus;
                }
            }
            int enemyQueen = forWhite ? -5 : 5;
            int tropismPenalty = 0;
            // Tropism: ถ้ามีราชาอยู่ใกล้กับราชินีศัตรู จะถูกลงโทษ (ยิ่งใกล้ ยิ่งโดนโทษมาก)
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.Board[x, y] == enemyQueen)
                    {
                        int dist = Math.Abs(king.X - x) + Math.Abs(king.Y - y);
                        tropismPenalty += Math.Max(0, (14 - dist)) * settings.TropismWeight;
                    }
                }

            }
            return score - tropismPenalty;
        }

        // Find King (ใช้สำหรับประเมินความปลอดภัยของ King และการคำนวณอื่นๆ ที่เกี่ยวข้องกับตำแหน่งของ King)
        private static Square FindKing(ChessBoardModel board, bool forWhite)
        {
            int king = forWhite ? 6 : -6;
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                    if (board.Board[x, y] == king)
                        return new Square(x, y);

            return new Square(-1, -1);
        }
        // Hanging Pieces Evaluation: ประเมินว่ามีหมากตัวไหนที่ถูกโจมตีโดยศัตรูแต่ไม่มีการป้องกัน (เช่น Knight ที่ถูกคุมแต่ไม่มีหมากป้องกันอยู่เลย จะโดนโทษ)
        private static int EvaluateHangingPieces(ChessBoardModel board, bool forWhite, int hangingPenalty, List<MoveModel> ownMoves, List<MoveModel> enemyMoves)
        {
            int score = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int piece = board.Board[x, y];
                    if (piece == 0 || Math.Abs(piece) == 1 || Math.Abs(piece) == 6) continue;
                    if (forWhite && piece < 0) continue;
                    if (!forWhite && piece > 0) continue;

                    bool attacked = enemyMoves.Exists(m => m.ToX == x && m.ToY == y);
                    bool defended = ownMoves.Exists(m => m.ToX == x && m.ToY == y);
                    if (attacked && !defended)
                        score += hangingPenalty;
                }
            }

            return score;
        }
        // Get Moves For Side (ใช้สำหรับประเมิน Mobility และ Threats โดยการสร้างรายการเดินที่ถูกต้องสำหรับฝ่ายนั้นๆ)
        private static List<MoveModel> GetMovesForSide(ChessBoardModel board, bool forWhite)
        {
            var clone = board.Clone();
            clone.IsWhiteTurn = forWhite;
            return MoveGenerator.GenerateMoves(clone);
        }

    }
}