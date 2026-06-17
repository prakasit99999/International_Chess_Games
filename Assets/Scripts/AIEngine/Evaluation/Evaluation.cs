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
        // Total Phase = 24 (Pawn=0, Knight=1, Bishop=1, Rook=2, Queen=4, King=0) * จำนวนหมากแต่ละประเภทบนกระดานเริ่มต้น
        // ตัวอย่าง: 0 (Pawn), 1 (N), 2 (B), 3 (R), 4 (Q), 5 (K)  =
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
            int phase = PhaseTotal;   // นับถอยจากเต็มกระดานไปท้ายเกม

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
                        phase -= PhaseWeights[pieceTypeIndex];
                    }

                    // C. Positional Score (PST)
                    // ต้อง Flip index สำหรับสีดำ (Mirror)
                    // x (rank): 0-7. สีขาวเริ่มแถว 7, สีดำเริ่มแถว 0 (ใน Model ของคุณ ดูเหมือน 0 คือแถวบน)
                    // สมมติ: board[0,0] คือ a8 (มุมดำ), board[7,0] คือ a1 (มุมขาว) 
                    // ดังนั้น White Index = table[x * 8 + y], Black Index = table[(7-x) * 8 + y]

                    int tableIdx = isWhite ? ((7 - x) * 8 + y) : (x * 8 + y);

                    // เลือกใช้ตารางคะแนนตามประเภทหมาก
                    // (ตัวอย่างใช้ตาราง Pawn/King เพื่อความกระชับ คุณควรเพิ่มตาราง Knight/Bishop/Rook/Queen ให้ครบ)
                    float mgPst = 0f, egPst = 0f;

                    switch (absPiece)
                    {
                        case 1: // Pawn
                            mgPst = MgPawnTable[tableIdx];
                            egPst = EgPawnTable[tableIdx];
                            float structScore = EvaluatePawnStructure(board, x, y, isWhite, passedPawnBonus, isolatedPawnPenalty, settings.DoubledPawnPenalty, phase);
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
            // Heavy move generation is needed for mobility and king-safety attack maps.
            // Threats (hanging pieces) use square-attack checks instead.
            bool needWhiteMoves = settings.UseMobility;
            bool needBlackMoves = settings.UseMobility;
            if (needWhiteMoves) whiteMoves = GetMovesForSide(board, true);
            if (needBlackMoves) blackMoves = GetMovesForSide(board, false);

            if (settings.UseMobility)
            {
                mgScore += EvaluateMobility(board, whiteMoves, true, phase, settings) * settings.MobilityWeight;
                mgScore -= EvaluateMobility(board, blackMoves, false, phase, settings) * settings.MobilityWeight;
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
                mgScore += EvaluateHangingPieces(board, true, settings);
                mgScore -= EvaluateHangingPieces(board, false, settings);
            }

            if (settings.UseBackwardPawn || settings.UsePawnChain || settings.UsePawnStorm)
            {
                mgScore += EvaluatePawnExtras(board, true, settings);
                mgScore -= EvaluatePawnExtras(board, false, settings);
            }

            if (settings.UseRookOnSeventh)
            {
                mgScore += EvaluateRookOnSeventh(board, true, settings);
                mgScore -= EvaluateRookOnSeventh(board, false, settings);
            }

            if (settings.UseQueenEarlyPenalty)
            {
                mgScore += EvaluateQueenEarlyPenalty(board, true, settings);
                mgScore -= EvaluateQueenEarlyPenalty(board, false, settings);
            }

            if (settings.UseKingDistanceEndgame)
            {
                egScore += EvaluateKingDistanceEndgame(board, phase, settings);
            }


            // 3. Tapered Evaluation Calculation
            // Phase ยิ่งมาก = ยิ่งใกล้ต้นเกม, Phase น้อย = ท้ายเกม
            // phase ณ จุดนี้ = endgame progress (24 ต้นเกม, 0ท้ายเกม)
            phase = Math.Max(0, Math.Min(phase, PhaseTotal)); // Clamp value
            int mgPhase = PhaseTotal - phase;
            int egPhase = phase;
            float finalScore = ((mgScore * mgPhase) + (egScore * egPhase)) / (float)PhaseTotal;

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
        private static float EvaluatePawnStructure(ChessBoardModel board, int x, int y, bool isWhite, int passedBonus, int isolatedPenalty, int doubledPenalty, int currentPhase)
        {
            float score = 0f;
            // connected passed pawn
            if (HasPawnOnFile(board, y - 1, isWhite) || HasPawnOnFile(board, y + 1, isWhite))
            {
                score += 20;
            }

            // 1. Isolated Pawn
            bool leftFileHasPawn = HasPawnOnFile(board, y - 1, isWhite);
            bool rightFileHasPawn = HasPawnOnFile(board, y + 1, isWhite);
            if (!leftFileHasPawn && !rightFileHasPawn)
            {
                score -= Math.Abs(isolatedPenalty);
            }

            // 2. Passed Pawn (มีความสำคัญพุ่งสูงปรี๊ดตอน End Game)
            if (IsPassedPawn(board, x, y, isWhite))
            {
                float rankBonus = isWhite ? (7 - x) * 15f : x * 15f;
                float endgameWeight = currentPhase / (float)PhaseTotal; // 0.0 = ต้นเกม -> 1.0 = ปลายเกม

                score += passedBonus + rankBonus + (50f * endgameWeight);

                // โบนัสถ้ารุกข์คุมหลังเบี้ยผ่าน
                int rookVal = isWhite ? 4 : -4;
                int behindRow = isWhite ? x + 1 : x - 1;
                if (behindRow >= 0 && behindRow <= 7 && board.Board[behindRow, y] == rookVal)
                {
                    score += 25f;
                }
            }

            // 3. Doubled Pawn
            int pawnCountOnFile = CountPawnsOnFile(board, y, isWhite);
            if (pawnCountOnFile > 1)
            {
                score -= (pawnCountOnFile - 1) * Math.Abs(doubledPenalty);
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
        private static List<MoveModel> GetMovesForSide(ChessBoardModel board, bool forWhite)
        {
            var clone = board.CloneForSearch();
            clone.IsWhiteTurn = forWhite;
            return MoveGenerator.GenerateMoves(clone);
        }

        private static int EvaluateMobility(ChessBoardModel board, List<MoveModel> moves, bool forWhite, int phase, EvaluationSettings settings)
        {
            if (moves == null || moves.Count == 0) return 0;

            int score = 0;
            float mgFactor = 1f - (phase / (float)PhaseTotal);
            foreach (var move in moves)
            {
                int fromPiece = board.Board[move.FromX, move.FromY];
                if (fromPiece == 0) continue;
                if (forWhite && fromPiece < 0) continue;
                if (!forWhite && fromPiece > 0) continue;

                int absPiece = Math.Abs(fromPiece);
                int pieceWeight = absPiece switch
                {
                    2 => 2,
                    3 => 2,
                    4 => 2,
                    5 => 1,
                    1 => 0,
                    _ => 0
                };

                if (pieceWeight == 0) continue;

                int moveScore = pieceWeight;
                if (board.Board[move.ToX, move.ToY] != 0)
                    moveScore += 1;

                if (move.ToX >= 2 && move.ToX <= 5 && move.ToY >= 2 && move.ToY <= 5)
                    moveScore += 1;

                bool targetAttacked = board.IsSquareUnderAttack(new Square(move.ToX, move.ToY), !forWhite);
                if (!targetAttacked)
                {
                    moveScore += settings.SafeMobilityBonus;
                }
                else if (board.Board[move.ToX, move.ToY] != 0 && EvaluateCaptureSEE(board, move, settings) >= 0)
                {
                    moveScore += Math.Max(1, settings.SafeMobilityBonus / 2);
                }

                if (absPiece == 3)
                {
                    moveScore += settings.BishopMobilityBonus;
                }

                // Non-linear scaling rewards truly active pieces more than raw move counting.
                score += moveScore * moveScore;
            }

            return (int)(score * (0.2f + 0.3f * mgFactor));
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
                    else score -= settings.BrokenPawnShieldPenalty;
                }
            }
            else
            {
                score -= settings.BrokenPawnShieldPenalty * 2;
            }
            int enemyQueen = forWhite ? -5 : 5;
            int tropismPenalty = 0;
            int attackCount = CountAttackedKingRingSquares(board, king, !forWhite);
            score -= attackCount * settings.KingAttackPenalty;
            score -= EvaluateOpenFilesNearKing(board, king, forWhite, settings);

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
        private static int EvaluateHangingPieces(ChessBoardModel board, bool forWhite, EvaluationSettings settings)
        {
            int score = 0;
            bool enemyIsWhite = !forWhite;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int piece = board.Board[x, y];
                    if (piece == 0 || Math.Abs(piece) == 1 || Math.Abs(piece) == 6) continue;
                    if (forWhite && piece < 0) continue;
                    if (!forWhite && piece > 0) continue;

                    var square = new Square(x, y);
                    bool attacked = board.IsSquareUnderAttack(square, enemyIsWhite);
                    if (!attacked) continue;
                    bool defended = board.IsSquareUnderAttack(square, forWhite);
                    int exchangeScore = EvaluateStaticExchangeOnSquare(board, x, y, forWhite);
                    if (attacked && !defended)
                        score += settings.HangingPiecePenalty;
                    else if (exchangeScore < 0)
                        score += exchangeScore * settings.ThreatExchangeWeight / 100;
                }
            }

            return score;
        }

        private static float EvaluateKingDistanceEndgame(ChessBoardModel board, int phase, EvaluationSettings settings)
        {
            var whiteKing = FindKing(board, true);
            var blackKing = FindKing(board, false);
            if (whiteKing.X < 0 || blackKing.X < 0) return 0f;

            int whiteMaterial = GetMaterialWithoutKing(board, settings, true);
            int blackMaterial = GetMaterialWithoutKing(board, settings, false);
            int materialDiff = whiteMaterial - blackMaterial;

            float endgameProgress = phase / (float)PhaseTotal;
            int absMaterialDiff = Math.Abs(materialDiff);

            // ลด noise: ใช้เฉพาะช่วงท้ายเกมจริง + ต้องนำชัดเจน
            if (endgameProgress < 0.40f) return 0f;
            if (absMaterialDiff < settings.PawnValue * 2) return 0f;

            int totalMaterial = whiteMaterial + blackMaterial;
            if (totalMaterial > (settings.QueenValue * 2 + settings.RookValue * 2)) return 0f;

            int kingDistance = Math.Abs(whiteKing.X - blackKing.X) + Math.Abs(whiteKing.Y - blackKing.Y);
            int closeness = 14 - kingDistance;
            float advantageScale = Math.Min(1f, absMaterialDiff / (float)(settings.RookValue * 2));
            float score = closeness * settings.KingDistanceEndgameWeight * endgameProgress * advantageScale;

            return materialDiff > 0 ? score : -score;
        }

        private static int CountAttackedKingRingSquares(ChessBoardModel board, Square king, bool byWhite)
        {
            int attacked = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = king.X + dx;
                    int y = king.Y + dy;
                    if (x < 0 || x > 7 || y < 0 || y > 7) continue;

                    if (board.IsSquareUnderAttack(new Square(x, y), byWhite))
                    {
                        attacked++;
                    }
                }
            }
            return attacked;
        }

        private static int EvaluateOpenFilesNearKing(ChessBoardModel board, Square king, bool forWhite, EvaluationSettings settings)
        {
            int ownPawn = forWhite ? 1 : -1;
            int penalty = 0;

            for (int file = Math.Max(0, king.Y - 1); file <= Math.Min(7, king.Y + 1); file++)
            {
                bool ownPawnFound = false;
                for (int rank = 0; rank < 8; rank++)
                {
                    if (board.Board[rank, file] == ownPawn)
                    {
                        ownPawnFound = true;
                        break;
                    }
                }

                if (!ownPawnFound)
                {
                    penalty += settings.OpenFileNearKingPenalty;
                }
            }

            return penalty;
        }

        private static int EvaluatePawnExtras(ChessBoardModel board, bool forWhite, EvaluationSettings settings)
        {
            int pawn = forWhite ? 1 : -1;
            int score = 0;
            var enemyKing = FindKing(board, !forWhite);

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.Board[x, y] != pawn) continue;

                    if (settings.UsePawnChain && IsPawnChained(board, x, y, forWhite))
                    {
                        score += settings.PawnChainBonus;
                    }

                    if (settings.UseBackwardPawn && IsBackwardPawn(board, x, y, forWhite))
                    {
                        score += settings.BackwardPawnPenalty;
                    }

                    if (settings.UsePawnStorm && enemyKing.X >= 0 && IsPawnStormPawn(x, y, enemyKing, forWhite))
                    {
                        score += settings.PawnStormBonus;
                    }
                }
            }

            return score;
        }

        private static bool IsPawnChained(ChessBoardModel board, int x, int y, bool forWhite)
        {
            int pawn = forWhite ? 1 : -1;
            int supportRow = forWhite ? x + 1 : x - 1;
            if (supportRow < 0 || supportRow > 7) return false;

            return (y > 0 && board.Board[supportRow, y - 1] == pawn) ||
                   (y < 7 && board.Board[supportRow, y + 1] == pawn);
        }

        private static bool IsBackwardPawn(ChessBoardModel board, int x, int y, bool forWhite)
        {
            int direction = forWhite ? -1 : 1;
            int enemyPawn = forWhite ? -1 : 1;

            if (HasPawnAheadOnAdjacentFiles(board, x, y, forWhite))
            {
                return false;
            }

            int frontX = x + direction;
            if (frontX < 0 || frontX > 7) return false;

            bool controlledByEnemyPawn =
                (y > 0 && board.Board[frontX, y - 1] == enemyPawn) ||
                (y < 7 && board.Board[frontX, y + 1] == enemyPawn);

            return controlledByEnemyPawn;
        }

        private static bool HasPawnAheadOnAdjacentFiles(ChessBoardModel board, int x, int y, bool forWhite)
        {
            int pawn = forWhite ? 1 : -1;
            if (y > 0)
            {
                for (int row = x + (forWhite ? -1 : 1); row >= 0 && row <= 7; row += (forWhite ? -1 : 1))
                {
                    if (board.Board[row, y - 1] == pawn) return true;
                }
            }

            if (y < 7)
            {
                for (int row = x + (forWhite ? -1 : 1); row >= 0 && row <= 7; row += (forWhite ? -1 : 1))
                {
                    if (board.Board[row, y + 1] == pawn) return true;
                }
            }

            return false;
        }

        private static bool IsPawnStormPawn(int x, int y, Square enemyKing, bool forWhite)
        {
            int forwardDistance = forWhite ? x : (7 - x);
            bool nearKingFile = Math.Abs(y - enemyKing.Y) <= 1;
            bool advancedEnough = forwardDistance <= 4;
            bool onEnemySide = forWhite ? x <= 4 : x >= 3;
            return nearKingFile && advancedEnough && onEnemySide;
        }

        private static int EvaluateRookOnSeventh(ChessBoardModel board, bool forWhite, EvaluationSettings settings)
        {
            int rook = forWhite ? 4 : -4;
            int targetRank = forWhite ? 1 : 6;
            int bonus = 0;

            for (int y = 0; y < 8; y++)
            {
                if (board.Board[targetRank, y] == rook)
                {
                    bonus += settings.RookOnSeventhBonus;
                }
            }

            return bonus;
        }

        private static int EvaluateQueenEarlyPenalty(ChessBoardModel board, bool forWhite, EvaluationSettings settings)
        {
            int queen = forWhite ? 5 : -5;
            int homeX = forWhite ? 7 : 0;
            int homeY = 3;
            int minor1 = forWhite ? 2 : -2;
            int minor2 = forWhite ? 3 : -3;

            int queenX = -1;
            int queenY = -1;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    if (board.Board[x, y] == queen)
                    {
                        queenX = x;
                        queenY = y;
                        break;
                    }
                }
            }

            if (queenX < 0 || (queenX == homeX && queenY == homeY))
            {
                return 0;
            }

            int undevelopedMinors = 0;
            int minorHomeRow = forWhite ? 7 : 0;
            int[] minorFiles = { 1, 2, 5, 6 };
            foreach (int file in minorFiles)
            {
                int piece = board.Board[minorHomeRow, file];
                if (piece == minor1 || piece == minor2)
                {
                    undevelopedMinors++;
                }
            }

            return undevelopedMinors >= 2 ? settings.QueenEarlyPenalty : 0;
        }

        private static int EvaluateStaticExchangeOnSquare(ChessBoardModel board, int x, int y, bool pieceIsWhite)
        {
            int piece = board.Board[x, y];
            if (piece == 0) return 0;

            int pieceValue = GetSimplePieceValue(Math.Abs(piece));
            bool attacked = board.IsSquareUnderAttack(new Square(x, y), !pieceIsWhite);
            if (!attacked) return 0;

            bool defended = board.IsSquareUnderAttack(new Square(x, y), pieceIsWhite);
            if (!defended) return -pieceValue;

            return -Math.Max(1, pieceValue / 4);
        }

        private static int EvaluateCaptureSEE(ChessBoardModel board, MoveModel move, EvaluationSettings settings)
        {
            int movingPiece = board.Board[move.FromX, move.FromY];
            if (movingPiece == 0) return 0;

            int capturedPiece = board.Board[move.ToX, move.ToY];
            if (capturedPiece == 0 && board.EnPassantTarget.HasValue &&
                move.ToX == board.EnPassantTarget.Value.X &&
                move.ToY == board.EnPassantTarget.Value.Y &&
                Math.Abs(movingPiece) == 1)
            {
                capturedPiece = movingPiece > 0 ? -1 : 1;
            }

            if (capturedPiece == 0) return 0;

            int gain = GetPieceValue(Math.Abs(capturedPiece), settings) - GetPieceValue(Math.Abs(movingPiece), settings);
            return gain;
        }

        private static int GetPieceValue(int absPiece, EvaluationSettings settings)
        {
            switch (absPiece)
            {
                case 1: return settings.PawnValue;
                case 2: return settings.KnightValue;
                case 3: return settings.BishopValue;
                case 4: return settings.RookValue;
                case 5: return settings.QueenValue;
                case 6: return KingValue;
                default: return 0;
            }
        }

        private static int GetSimplePieceValue(int absPiece)
        {
            switch (absPiece)
            {
                case 1: return 100;
                case 2: return 320;
                case 3: return 330;
                case 4: return 500;
                case 5: return 900;
                case 6: return KingValue;
                default: return 0;
            }
        }

        private static int GetMaterialWithoutKing(ChessBoardModel board, EvaluationSettings settings, bool forWhite)
        {
            int sign = forWhite ? 1 : -1;
            int material = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int piece = board.Board[x, y];
                    if (piece * sign <= 0) continue;

                    switch (Math.Abs(piece))
                    {
                        case 1: material += settings.PawnValue; break;
                        case 2: material += settings.KnightValue; break;
                        case 3: material += settings.BishopValue; break;
                        case 4: material += settings.RookValue; break;
                        case 5: material += settings.QueenValue; break;
                    }
                }
            }

            return material;
        }
    }
}
