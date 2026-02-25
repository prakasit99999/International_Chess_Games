using System;

[Serializable]
public class AiPerformanceData
{
    // --- ส่วนสรุปผลรวม (ตรงกับตาราง ai_performance) ---
    public int GameId;
    public string AiLevel;
    public string AlgorithmType;
    public float AverageDepth;
    public int AverageNodesEvaluated;
    public int AverageMoveTimeMs;
    public int TotalMoves;

    // --- ข้อมูลรายตาเดิน (ใช้ภายใน/MoveMapper) ---
    public float Score;
    public int Depth;
    public int Nodes;
    public int MoveTimeMs;

    public bool HasAnyData()
    {
        // ไม่มีเกม → ไม่ส่ง
        if (GameId <= 0)
            return false;

        // ไม่ใช่ AI → ไม่ส่ง
        if (string.IsNullOrEmpty(AlgorithmType) || AlgorithmType == "None")
            return false;

        // AI ไม่ได้คิดเลย
        if (TotalMoves <= 0)
            return false;

        return true;
    }

}