using System;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    public static PerformanceTracker Instance;
    private List<int> depths = new List<int>();
    private List<int> nodes = new List<int>();
    private List<float> times = new List<float>();
    private List<float> scores = new List<float>();
    public string AiLevel;           // easy / medium / hard
    public string AlgorithmType;     // minimax / alpha_beta
    public int GameId;
    public int UserId = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // UserId should be initialized from a SessionManager or similar in a real app
            Debug.Log($"[PerformanceTracker] Initialized with UserId: {UserId}");
        }
        else
            Destroy(gameObject);
    }

    public void ResetData()
    {
        depths.Clear();
        nodes.Clear();
        times.Clear();
    }

    public void AddMove(int depth, int nodeCount, float timeMs, float score)
    {
        depths.Add(depth);
        nodes.Add(nodeCount);
        times.Add(timeMs);
        scores.Add(score);
        Debug.Log($"[PerformanceTracker] Added Data -> Depth: {depth}, Nodes: {nodeCount}, Time: {timeMs}, Score: {score}");
    }

    public AiPerformanceData GetAiDataAt(int index)
    {
        if (index < 0 || index >= depths.Count) return null;

        return new AiPerformanceData
        {
            Depth = depths[index],
            Nodes = nodes[index],
            MoveTimeMs = (int)times[index],
            Score = scores[index],
            AlgorithmType = this.AlgorithmType
        };
    }

    public AiPerformanceData Export()
    {
        // 1. Sanitize AI Level (Database expects: easy, medium, hard)
        string safeLevel = (AiLevel ?? "easy").ToLower();
        if (safeLevel == "normal") safeLevel = "medium";

        // 2. Determine Algorithm based on Level (easy = minimax, others = alpha_beta)
        string safeAlgo = (safeLevel == "easy") ? "minimax" : "alpha_beta";

        return new AiPerformanceData
        {
            GameId = GameId,
            AiLevel = safeLevel,
            AlgorithmType = safeAlgo,
            AverageDepth = depths.Count > 0 ? (float)Math.Round(Average(depths), 2) : 0f,
            AverageNodesEvaluated = nodes.Count > 0 ? Mathf.RoundToInt(Average(nodes)) : 0,
            AverageMoveTimeMs = times.Count > 0 ? Mathf.RoundToInt(Average(times)) : 0,
            TotalMoves = depths.Count
        };
    }

    private float Average(List<int> list)
    {
        float sum = 0;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }

    private float Average(List<float> list)
    {
        float sum = 0;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }
}
