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

        // --- Pawn Weights ---
        public int PassedPawnBonus { get; set; } = 20;      // โบนัสเบี้ยผ่าน
        public int IsolatedPawnPenalty { get; set; } = -15; // โทษเบี้ยโดดเดี่ยว
        public int DoubledPawnPenalty { get; set; } = -10;  // โทษเบี้ยซ้อน
        public int TempoBonus { get; set; } = 10; // ใช้โบนัสจังหวะเดิน (Tempo)

        // --- Advanced Evaluation Weights ---
        public int MobilityWeight { get; set; } = 2;
        public int BishopPairBonus { get; set; } = 25;
        public int RookOpenFileBonus { get; set; } = 20;
        public int RookSemiOpenFileBonus { get; set; } = 10;
        public int KnightOutpostBonus { get; set; } = 18;
        public int SpaceWeight { get; set; } = 1;
        public int HangingPiecePenalty { get; set; } = -20;
        public int KingDistanceEndgameWeight { get; set; } = 6;
        public int SafeMobilityBonus { get; set; } = 1;
        public int BishopMobilityBonus { get; set; } = 1;
        public int RookOnSeventhBonus { get; set; } = 20;
        public int QueenEarlyPenalty { get; set; } = -18;
        public int ThreatExchangeWeight { get; set; } = 12;
        public int BackwardPawnPenalty { get; set; } = -12;
        public int PawnChainBonus { get; set; } = 8;
        public int PawnStormBonus { get; set; } = 10;

        // --- King Safety ---
        public int PawnShieldBonus { get; set; } = 12;
        public int TropismWeight { get; set; } = 4;
        public int KingAttackPenalty { get; set; } = 8;
        public int OpenFileNearKingPenalty { get; set; } = 18;
        public int BrokenPawnShieldPenalty { get; set; } = 10;

        // --- Feature Toggles by Difficulty ---
        public bool UseMobility { get; set; } = true;
        public bool UseKingSafety { get; set; } = true;
        public bool UseBishopPair { get; set; } = true;
        public bool UseRookFiles { get; set; } = true;
        public bool UseOutpost { get; set; } = true;
        public bool UseSpace { get; set; } = true;
        public bool UseThreats { get; set; } = true;
        public bool UseKingDistanceEndgame { get; set; } = true;
        public bool UseBackwardPawn { get; set; } = true;
        public bool UsePawnChain { get; set; } = true;
        public bool UsePawnStorm { get; set; } = true;
        public bool UseRookOnSeventh { get; set; } = true;
        public bool UseQueenEarlyPenalty { get; set; } = true;
        public bool UseTempo { get; set; } = true; // เปิด/ปิดโบนัสคนได้เดินก่อน

    }
}
