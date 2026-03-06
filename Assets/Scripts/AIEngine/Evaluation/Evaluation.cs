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

        // คะแนนพื้นฐานของตัวหมาก (Material) - ใช้จาก Settings แทน
        // private const int PawnValue = 100;
        // private const int KnightValue = 320;
        //
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
                            // เพิ่ม Pawn Structure Evaluation ตรงนี้
                            // เพิ่ม Pawn Structure Evaluation ตรงนี้
                            float structScore = EvaluatePawnStructure(board, x, y, isWhite, passedPawnBonus, isolatedPawnPenalty);
                            if (isWhite) { mgScore += structScore; egScore += structScore; }
                            else { mgScore -= structScore; egScore -= structScore; }
                            break;

                        case 2: // Knight
                            mgPst = MgKnightTable[tableIdx];
                            egPst = MgKnightTable[tableIdx]; // Knight ไม่ค่อยต่างมาก
                            break;

                        case 6: // King
                            mgPst = MgKingTable[tableIdx];
                            egPst = EgKingTable[tableIdx];
                            break;

                        // TODO: ใส่ตาราง Bishop, Rook, Queen
                        default:
                            break;
                    }

                    if (isWhite) { mgScore += mgPst * positionalFactor; egScore += egPst * positionalFactor; }
                    else { mgScore -= mgPst * positionalFactor; egScore -= egPst * positionalFactor; }
                }
            }

            // 3. Tapered Evaluation Calculation
            // Phase ยิ่งมาก = ยิ่งใกล้ต้นเกม, Phase น้อย = ท้ายเกม
            // Formula: (MG * phase + EG * (24 - phase)) / 24
            phase = Math.Min(phase, PhaseTotal); // Clamp value

            // Apply Positional Factor (Style) only to Positional part if separated, OR applies to total evaluation difference?
            // Usually applies to the strategic components, but here simple multiplication for now as requested.
            // But wait, user said "PositionalFactor" implies scaling the PST/Structure scores.
            // Current code mixes Material + PST into mgScore/egScore.
            // For now, let's keep it simple as the original code didn't separate them clearly.
            // Or better, apply to the PST lookup result?
            // Re-reading user request: "EvaluationSettings defined parameters like PositionalFactor... but implementation uses internal constants".
            // Implementation: I will apply PositionalFactor to the PST values looked up.

            // NOTE: I accidentally didn't apply PositionalFactor in the loop above.
            // Let's modify the PST lookup above in a separate step or just assume the user wants the infrastructure first.
            // Actually, I can't easily modify the PST lookup block in this chunk without making it huge.
            // I will leave PositionalFactor unused for this specific chunk and focus on Material/PawnStructure first to minimize risk?
            // NO, I should do it right. I will edit the PST lookup block.

            float finalScore = ((mgScore * phase) + (egScore * (PhaseTotal - phase))) / (float)PhaseTotal;

            // 4. Side to Move Bonus (Tempo)
            // การได้เดินก่อนมีค่าเล็กน้อย (เช่น 10-20 คะแนน)
            // 4. Side to Move Bonus (Tempo) + Attack Bonus
            // การได้เดินก่อนมีค่าเล็กน้อย (เช่น 10-20 คะแนน) + Settings AttackBonus
            finalScore += board.IsWhiteTurn ? (10f + settings.AttackBonus) : (-10f - settings.AttackBonus);

            // Return absolute score (White-centric)
            return finalScore;
        }



        // ==========================================
        // PAWN STRUCTURE & PASSED PAWNS
        // ==========================================
        private static float EvaluatePawnStructure(ChessBoardModel board, int x, int y, bool isWhite, int passedBonus, int isolatedPenalty)
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

            return score;
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
            int startRow = isWhite ? 0 : r + 1;
            int endRow = isWhite ? r - 1 : 7;

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
    }
}