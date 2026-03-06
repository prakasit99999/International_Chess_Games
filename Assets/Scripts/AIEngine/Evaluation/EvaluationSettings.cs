using System;

namespace AIEngine.Evaluation
{
    public class EvaluationSettings
    {
        public string Name { get; set; } = "Standard";

        // --- Material Values (ค่าหมาก) ---
        public int PawnValue { get; set; } = 100;
        public int KnightValue { get; set; } = 320;
        public int BishopValue { get; set; } = 330;
        public int RookValue { get; set; } = 500;
        public int QueenValue { get; set; } = 900;

        // --- Style Multipliers (ปรับสไตล์) ---
        public float PositionalFactor { get; set; } = 1.0f; // 1.0 = ปกติ, 1.2 = เน้นยืนตำแหน่งสวย
        public int AttackBonus { get; set; } = 0;           // คะแนนพิเศษถ้าเป็นฝ่ายบุก

        // --- Pawn Weights ---
        public int PassedPawnBonus { get; set; } = 20;      // โบนัสเบี้ยผ่าน
        public int IsolatedPawnPenalty { get; set; } = -15; // โทษเบี้ยโดดเดี่ยว
    }
}