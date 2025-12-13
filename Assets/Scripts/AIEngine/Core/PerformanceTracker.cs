using System;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    public static PerformanceTracker Instance;
    private List<int> depths = new List<int>();
    private List<int> nodes = new List<int>();
    private List<float> times = new List<float>();
    public string AiLevel;           // easy / medium / hard
    public string AlgorithmType;     // minimax / alpha_beta
    public int GameId;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    public void ResetData()
    {
        depths.Clear();
        nodes.Clear();
        times.Clear();
    }

    public void AddMove(int depth, int nodeCount, float timeMs)
    {
        depths.Add(depth);
        nodes.Add(nodeCount);
        times.Add(timeMs);
    }

    public AiPerformanceData Export()
    {
        return new AiPerformanceData
        {
            GameId = GameId,
            AiLevel = AiLevel,
            AlgorithmType = AlgorithmType,
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
