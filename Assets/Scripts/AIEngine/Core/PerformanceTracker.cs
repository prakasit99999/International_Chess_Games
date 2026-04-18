using System;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    private class AiPerformanceBucket
    {
        public string AiColor;
        public string AiLevel;
        public string AlgorithmType;
        public readonly List<int> Depths = new List<int>();
        public readonly List<int> Nodes = new List<int>();
        public readonly List<float> Times = new List<float>();
        public readonly List<float> Scores = new List<float>();
    }

    public static PerformanceTracker Instance;
    public int GameId;
    public int UserId = -1;

    private readonly Dictionary<string, AiPerformanceBucket> buckets = new Dictionary<string, AiPerformanceBucket>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (UserId == -1)
            {
                if (UserId != -1)
                    Debug.Log($"[PerformanceTracker] UserId initialized from SessionManager: {UserId}");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetData()
    {
        buckets.Clear();
    }

    public void AddMove(string aiColor, string aiLevel, string algorithmType, int depth, int nodeCount, float timeMs, float score)
    {
        string normalizedColor = NormalizeColor(aiColor);
        if (string.IsNullOrEmpty(normalizedColor))
            return;

        if (!buckets.TryGetValue(normalizedColor, out AiPerformanceBucket bucket))
        {
            bucket = new AiPerformanceBucket
            {
                AiColor = normalizedColor
            };
            buckets[normalizedColor] = bucket;
        }

        bucket.AiLevel = NormalizeLevel(aiLevel);
        bucket.AlgorithmType = NormalizeAlgorithmType(algorithmType, bucket.AiLevel);
        bucket.Depths.Add(depth);
        bucket.Nodes.Add(nodeCount);
        bucket.Times.Add(timeMs);
        bucket.Scores.Add(score);

        Debug.Log($"[PerformanceTracker] Added Data -> Color: {bucket.AiColor}, Algo: {bucket.AlgorithmType}, Depth: {depth}, Nodes: {nodeCount}, Time: {timeMs}, Score: {score}");
    }

    public AiPerformanceData GetAiDataAt(string aiColor, int index)
    {
        string normalizedColor = NormalizeColor(aiColor);
        if (string.IsNullOrEmpty(normalizedColor))
            return null;

        if (!buckets.TryGetValue(normalizedColor, out AiPerformanceBucket bucket))
            return null;

        if (index < 0 || index >= bucket.Depths.Count)
            return null;

        return new AiPerformanceData
        {
            AiColor = bucket.AiColor,
            AiLevel = bucket.AiLevel,
            Depth = bucket.Depths[index],
            Nodes = bucket.Nodes[index],
            MoveTimeMs = Mathf.RoundToInt(bucket.Times[index]),
            Score = bucket.Scores[index],
            AlgorithmType = bucket.AlgorithmType
        };
    }

    public AiPerformanceData Export(string aiColor)
    {
        string normalizedColor = NormalizeColor(aiColor);
        if (string.IsNullOrEmpty(normalizedColor))
            return null;

        if (!buckets.TryGetValue(normalizedColor, out AiPerformanceBucket bucket))
            return null;

        return new AiPerformanceData
        {
            GameId = GameId,
            AiColor = bucket.AiColor,
            AiLevel = bucket.AiLevel,
            AlgorithmType = bucket.AlgorithmType,
            AverageDepth = bucket.Depths.Count > 0 ? (float)Math.Round(Average(bucket.Depths), 2) : 0f,
            AverageNodesEvaluated = bucket.Nodes.Count > 0 ? Mathf.RoundToInt(Average(bucket.Nodes)) : 0,
            AverageMoveTimeMs = bucket.Times.Count > 0 ? Mathf.RoundToInt(Average(bucket.Times)) : 0,
            TotalMoves = bucket.Depths.Count
        };
    }

    public List<AiPerformanceData> ExportAll()
    {
        var result = new List<AiPerformanceData>();

        foreach (var entry in buckets)
        {
            var data = Export(entry.Key);
            if (data != null && data.HasAnyData())
                result.Add(data);
        }

        return result;
    }

    private string NormalizeColor(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        string v = value.Trim().ToLowerInvariant();
        return v switch
        {
            "white" => "white",
            "black" => "black",
            _ => null
        };
    }

    private string NormalizeLevel(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "easy";

        string v = value.Trim().ToLowerInvariant();
        if (v == "normal")
            v = "medium";

        return v;
    }

    private string NormalizeAlgorithmType(string algorithmType, string aiLevel)
    {
        if (!string.IsNullOrEmpty(algorithmType))
            return algorithmType.Trim().ToLowerInvariant();

        return aiLevel == "easy" ? "minimax" : "alpha_beta";
    }

    private float Average(List<int> list)
    {
        float sum = 0f;
        foreach (var v in list)
            sum += v;
        return sum / list.Count;
    }

    private float Average(List<float> list)
    {
        float sum = 0f;
        foreach (var v in list)
            sum += v;
        return sum / list.Count;
    }
}
