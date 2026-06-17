using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AIEngine.Evaluation;
using AIEngine.Utilities;
using UnityEngine;
namespace AIEngine.Algorithms
{
    public class AlphaBeta : SearchAlgorithm
    {
        // 1. ตาราง Transposition Table
        private readonly TranspositionTable _tt = new TranspositionTable();
        // 2. Killer Moves: เก็บ 2 ท่า (Column) ต่อระดับความลึก (Row)
        private MoveModel[,] _killerMoves;
        // 3. History Heuristic: เก็บข้อมูล จาก (x,y) ไป (x,y)
        private int[,,,] _historyMoves;
        // ค่าคงที่สำหรับความลึกสูงสุด (เพื่อกำหนดขนาด Array)

        private const int MaxSearchDepth = 20;
        private const int MaxHistoryScore = 1_000_000;
        private const int AspirationWindowBase = 60;
        private const int FutilityDepthLimit = 1;
        private const int FutilityMarginDepth1 = 120;
        private const int FutilityMarginDepth2 = 220;

        private const int NoTimeLimitMs = 0;
        private const int DefaultTimeLimitMs = NoTimeLimitMs;
        private static readonly System.Random rng = new System.Random();
        private readonly int _timeLimitMs;
        // 2. จองพื้นที่ใน Constructor (ทำครั้งเดียว)
        public AlphaBeta(int timeLimitMs = DefaultTimeLimitMs)
        {
            // 0 means search without a time limit.
            _timeLimitMs = timeLimitMs;
            _killerMoves = new MoveModel[MaxSearchDepth, 2];
            _historyMoves = new int[8, 8, 8, 8];
        }

        private bool HasTimeLimit => _timeLimitMs > NoTimeLimitMs;

        private int _nodesEvaluated = 0;
        private int _actualDepth = 0;
        private readonly MoveModel[] _pvTable = new MoveModel[MaxSearchDepth];

        public override MoveModel FindBestMove(ChessBoardModel board, int maxDepth, EvaluationSettings settings = null)
        {
            var result = FindBestMoveWithMetrics(board, maxDepth, settings);
            return result.Move;
        }

        public override SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int maxDepth, EvaluationSettings settings = null)
        {
            _nodesEvaluated = 0;
            _actualDepth = 0;
            var stopwatch = Stopwatch.StartNew();
            MoveModel bestMove = null;
            List<MoveModel> bestMoves = new List<MoveModel>();
            int bestScore = 0;
            int currentDepth = 1;

            while (currentDepth <= maxDepth && (!HasTimeLimit || stopwatch.Elapsed.TotalMilliseconds < _timeLimitMs))
            {
                var iterationStart = stopwatch.Elapsed.TotalMilliseconds;
                Array.Clear(_pvTable, 0, _pvTable.Length);

                int aspiration = AspirationWindowBase + (currentDepth * 8);
                int alpha = currentDepth > 1 ? bestScore - aspiration : int.MinValue;
                int beta = currentDepth > 1 ? bestScore + aspiration : int.MaxValue;

                var (iterativeBests, iterativeBestScore) = AlphaBetaSearch(board, currentDepth, stopwatch, bestMove, settings, alpha, beta);

                bool failLow = currentDepth > 1 && iterativeBestScore <= alpha;
                bool failHigh = currentDepth > 1 && iterativeBestScore >= beta;
                if (failLow || failHigh)
                {
                    // Aspiration miss: re-search with full window.
                    (iterativeBests, iterativeBestScore) = AlphaBetaSearch(board, currentDepth, stopwatch, bestMove, settings, int.MinValue, int.MaxValue);
                }

                if (iterativeBests != null && iterativeBests.Count > 0)
                {
                    bestMoves = iterativeBests;
                    bestMove = bestMoves.First();
                    bestScore = iterativeBestScore;
                    _actualDepth = currentDepth;

                    var iterationTime = stopwatch.Elapsed.TotalMilliseconds - iterationStart;
                    UnityEngine.Debug.Log($"[AlphaBeta] Depth {currentDepth} completed in {iterationTime:F0}ms (Total: {stopwatch.Elapsed.TotalMilliseconds:F0}ms, Nodes: {_nodesEvaluated})");
                }
                AgeHistoryHeuristic();
                currentDepth++;
            }

            string timeLimitLabel = HasTimeLimit ? $"{_timeLimitMs}ms" : "No limit";
            UnityEngine.Debug.Log($"[AlphaBeta] Search finished at Depth {_actualDepth} (Target: {maxDepth}, TimeLimit: {timeLimitLabel}, Elapsed: {stopwatch.Elapsed.TotalMilliseconds:F0}ms)");

            stopwatch.Stop();
            var elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            var moves = MoveGenerator.GenerateMoves(board);
            var finalMove = bestMoves.DefaultIfEmpty(moves.FirstOrDefault()).FirstOrDefault();
            return new SearchResult
            {
                Move = finalMove,
                Depth = _actualDepth,
                NodesEvaluated = _nodesEvaluated,
                TimeMs = elapsedMs,
                Score = bestScore,

            };
        }

        private bool TryNullMovePrune(ChessBoardModel board, int depth, int alpha, int beta, bool maximizingPlayer, int ply, Stopwatch stopwatch, EvaluationSettings settings, bool allowNullMove, out int prunedScore)
        {
            prunedScore = 0;

            if (!CanApplyNullMove(board, depth, maximizingPlayer, allowNullMove))
                return false;

            int R = 2; // reduction
            if (depth >= 6 && !IsEndgame(board))
            {
                R = 3;
            }
            var nullBoard = board.CloneForSearch();

            // Null move = pass turn
            nullBoard.IsWhiteTurn = !nullBoard.IsWhiteTurn;
            nullBoard.EnPassantTarget = null; // ep right หายหลังผ่านตา

            // โค้ดคเป็น max/min ชให้เรียกแบบช่วงแคบตามฝั่ง
            int score = AlphaBetaRecursive(
                nullBoard,
                depth - 1 - R,
                alpha,
                beta,
                !maximizingPlayer,
                ply + 1,
                stopwatch,
                settings,
                allowNullMove: false // กัน null ซ้อนทันที
            );

            // เงื่อนไข prune สำหรับ max/min style
            if (maximizingPlayer)
            {
                if (score >= beta)
                {
                    prunedScore = beta; // fail-high cutoff
                    return true;
                }
            }
            else
            {
                if (score <= alpha)
                {
                    prunedScore = alpha; // fail-low cutoff
                    return true;
                }
            }

            return false;
        }

        private bool CanApplyNullMove(
            ChessBoardModel board,
            int depth,
            bool maximizingPlayer,
            bool allowNullMove)
        {
            if (!allowNullMove) return false;
            if (depth <= 4) return false;

            // Use board state directly: side to move is in check?
            bool inCheck = board.IsInCheck(board.IsWhiteTurn);
            if (inCheck) return false;
            if (IsPawnOnlyEndgame(board)) return false;
            if (IsEndgame(board) && IsLikelyZugzwang(board)) return false;
            return true;
        }

        private bool IsPawnOnlyEndgame(ChessBoardModel board)
        {
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    int p = Math.Abs(board.Board[x, y]);
                    if (p != 0 && p != 1 && p != 6)
                        return false;
                }
            return true;
        }
        private bool IsEndgame(ChessBoardModel board)
        {
            // Simple, safe endgame gate for NMP:
            // disable when non-pawn material is low.
            int nonPawnMaterialPhase = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int p = Math.Abs(board.Board[x, y]);
                    switch (p)
                    {
                        case 2: // knight
                        case 3: // bishop
                            nonPawnMaterialPhase += 1;
                            break;
                        case 4: // rook
                            nonPawnMaterialPhase += 2;
                            break;
                        case 5: // queen
                            nonPawnMaterialPhase += 4;
                            break;
                    }
                }
            }

            return nonPawnMaterialPhase <= 6;
        }


        private bool IsLikelyZugzwang(ChessBoardModel board)
        {
            int nonPawnMaterial = 0;

            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    int p = Math.Abs(board.Board[x, y]);

                    if (p == 2 || p == 3 || p == 4 || p == 5)
                        nonPawnMaterial++;
                }

            return nonPawnMaterial == 0; // เหลือแต่ pawn + king
        }


        private (List<MoveModel>, int) AlphaBetaSearch(ChessBoardModel board, int depth, Stopwatch stopwatch, MoveModel previousBest, EvaluationSettings settings, int alpha, int beta)
        {
            // ส่ง _killerMoves และ _historyMoves เข้าไปให้ MoveOrderer
            // สังเกตว่าผมส่ง null แทน ttMove ในพารามิเตอร์ที่ 3 เพราะเราใช้ previousBest เป็นตัวนำทางใน Root แล้ว
            var moves = MoveGenerator.GenerateMoves(board);
            // Root: use previousBest as PV hint, keep TT hint null at root.
            MoveOrderer.OrderMoves(moves, board, null, GetKillers(0), _historyMoves, previousBest ?? _pvTable[0]);
            bool isWhiteRoot = board.IsWhiteTurn;
            int bestScore = isWhiteRoot ? int.MinValue : int.MaxValue;
            List<MoveModel> bestMoves = new List<MoveModel>();

            foreach (var move in moves)
            {
                if (HasTimeLimit && stopwatch.Elapsed.TotalMilliseconds > _timeLimitMs) break;

                var newBoard = board.CloneForSearch();
                newBoard.MakeMove(move);

                int score;
                if (newBoard.RepetitionCount >= 3)
                    score = 0; // draw
                else
                    // เพิ่มพารามิเตอร์ ply = 1 และ settings
                    score = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, !board.IsWhiteTurn, 1, stopwatch, settings);

                bool isBetter = isWhiteRoot
                    ? score > bestScore
                    : score < bestScore;

                if (isBetter)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    _pvTable[0] = move;
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }

                if (isWhiteRoot)
                    alpha = Math.Max(alpha, bestScore);
                else
                    beta = Math.Min(beta, bestScore);

                if (alpha >= beta)
                    break;
            }
            return (bestMoves, bestScore);
        }
        // Helper: ดึงท่า Killer เฉพาะของ Ply ปัจจุบันออกมาเป็น Array 1 มิติ
        private MoveModel[] GetKillers(int ply)
        {
            // ป้องกัน Array Index Out of Bounds
            if (ply >= MaxSearchDepth) return new MoveModel[2] { null, null };

            // สร้าง Array ใหม่ที่มีแค่ 2 ท่าของชั้นนี้
            return new MoveModel[] { _killerMoves[ply, 0], _killerMoves[ply, 1] };
        }

        private void AddHistoryScore(MoveModel move, int bonus)
        {
            int current = _historyMoves[move.FromX, move.FromY, move.ToX, move.ToY];
            int next = current + bonus;
            if (next > MaxHistoryScore) next = MaxHistoryScore;
            _historyMoves[move.FromX, move.FromY, move.ToX, move.ToY] = next;
        }

        private void AgeHistoryHeuristic()
        {
            for (int fx = 0; fx < 8; fx++)
            {
                for (int fy = 0; fy < 8; fy++)
                {
                    for (int tx = 0; tx < 8; tx++)
                    {
                        for (int ty = 0; ty < 8; ty++)
                        {
                            _historyMoves[fx, fy, tx, ty] >>= 1;
                        }
                    }
                }
            }
        }

        private static int GetFutilityMargin(int depth)
        {
            if (depth <= 1) return FutilityMarginDepth1;
            return FutilityMarginDepth2;
        }

        private int AlphaBetaRecursive(ChessBoardModel board, int depth, int alpha, int beta, bool maximizingPlayer, int ply, Stopwatch stopwatch, EvaluationSettings settings, bool allowNullMove = true, bool allowCheckExtension = true)
        {
            _nodesEvaluated++; // นับ node ที่ evaluate

            if (HasTimeLimit && (_nodesEvaluated & 2047) == 0 && stopwatch.Elapsed.TotalMilliseconds > _timeLimitMs)
                return (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            if (board.IsGameOver())
            {
                // ✅ Evaluation() ของคุณ normalize อยู่แล้ว ไม่ต้องคูณ maximizingPlayer
                return (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);
            }

            // ✅ threefold repetition -> draw
            if (board.RepetitionCount >= 3)
            {
                return 0;
            }
            ulong key = board.ZobristKey;
            if (_tt.TryGet(key, out TTEntry entry) && entry.Depth >= depth)
            {
                // 2. เช็คว่าข้อมูลเก่า "ลึก" พอไหม?
                if (entry.Depth >= depth)
                {
                    // TT hit - ยังคงนับเป็น node (เพราะเคย evaluate มาแล้ว)
                    if (entry.Flag == TTEntry.TTFlag.Exact)
                        return (int)entry.Score;

                    // ถ้าเป็น LowerBound (ค่าจริงอาจสูงกว่านี้) และค่าที่เก็บไว้มันสูงกว่า Beta -> ตัดจบได้
                    if (entry.Flag == TTEntry.TTFlag.LowerBound)
                        alpha = (int)Math.Max(alpha, entry.Score);

                    // ถ้าเป็น UpperBound (ค่าจริงอาจต่ำกว่านี้) และค่าที่เก็บไว้มันต่ำกว่า Alpha -> ตัดจบได้
                    else if (entry.Flag == TTEntry.TTFlag.UpperBound)
                        beta = (int)Math.Min(beta, entry.Score);

                    // ถ้า Alpha >= Beta แสดงว่าเราตัดจบได้
                    if (alpha >= beta)
                        return (int)entry.Score;
                }
            }
            //Null Move Pruning (NMP)
            if (TryNullMovePrune(board, depth, alpha, beta, maximizingPlayer, ply, stopwatch, settings, allowNullMove, out int prunedScore))
            {
                return prunedScore; // Return the cutoff score from null move pruning
            }

            bool inCheck = board.IsInCheck(board.IsWhiteTurn);
            int staticEval = (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);
            int checkExtension = (inCheck && depth <= 3) ? 1 : 0;
            bool childAllowCheckExtension = allowCheckExtension && checkExtension == 0;


            // [ต้องเติมกลับมาครับ!] ถ้าความลึกหมดแล้ว ให้ไปค้นหาใน Quiescence Search ต่อ
            if (depth <= 0)
            {
                // ส่ง null ไปในตัวสุดท้าย เพราะ Quiescence ไม่ได้คืนค่า BestMove
                var score = QuiescenceSearch(board, alpha, beta, maximizingPlayer, stopwatch, settings);
                _tt.Store(board.ZobristKey, depth, score, TTEntry.TTFlag.Exact, null);
                return score;
            }

            var moves = MoveGenerator.GenerateMoves(board);
            MoveOrderer.OrderMoves(moves, board, entry?.BestMove, GetKillers(ply), _historyMoves, _pvTable[ply]);
            var bestValue = maximizingPlayer ? int.MinValue : int.MaxValue;

            MoveModel bestMove = null;

            int originalAlpha = alpha;
            int originalBeta = beta;
            int moveCount = 0;

            foreach (var move in moves)
            {
                moveCount++;

                int value;
                int movingPiece = board.Board[move.FromX, move.FromY];
                bool isEnPassantCapture = Math.Abs(movingPiece) == 1 &&
                                          board.EnPassantTarget.HasValue &&
                                          move.ToX == board.EnPassantTarget.Value.X &&
                                          move.ToY == board.EnPassantTarget.Value.Y &&
                                          board.Board[move.ToX, move.ToY] == 0;
                bool isCaptureOrPromotion = board.Board[move.ToX, move.ToY] != 0 ||
                                            isEnPassantCapture ||
                                            move.PromotionPiece != 0;

                var newBoard = board.CloneForSearch();
                newBoard.MakeMove(move);
                bool isCheckMove = newBoard.IsInCheck(newBoard.IsWhiteTurn);
                bool isLateQuietMove =
                    depth >= 3 &&
                    moveCount > 3 &&
                    !isCaptureOrPromotion &&
                    !isCheckMove &&
                    !inCheck;
                if (!inCheck && depth <= FutilityDepthLimit && !isCaptureOrPromotion && !isCheckMove)
                {
                    int margin = GetFutilityMargin(depth);
                    if (maximizingPlayer && staticEval + margin <= alpha)
                        continue;
                    if (!maximizingPlayer && staticEval - margin >= beta)
                        continue;
                }

                //Late Move  Reduction (LMR) - ลดความลึกของท่าที่อยู่ลึกๆ ในการค้นหา
                if (isLateQuietMove)
                {
                    // ค้นหาแบบลดระดับ (Reduced Depth) ไปก่อน 1 ระดับ
                    value = AlphaBetaRecursive(newBoard, depth - 2 + checkExtension, alpha, beta, !maximizingPlayer, ply + 1, stopwatch, settings, allowNullMove: true, allowCheckExtension: childAllowCheckExtension);
                    // ถ้าค่าที่ได้ดันทะลุหน้าต่าง Alpha-Beta แปลว่าเรามองข้ามท่าดีๆ ไป! ให้ค้นหาเต็มสูบใหม่
                    bool needsFullSearch = maximizingPlayer ? value > alpha : value < beta;
                    if (needsFullSearch)
                    {
                        value = AlphaBetaRecursive(newBoard, depth - 1 + checkExtension, alpha, beta, !maximizingPlayer, ply + 1, stopwatch, settings, allowNullMove: true, allowCheckExtension: childAllowCheckExtension);
                    }
                }
                else
                {
                    value = AlphaBetaRecursive(newBoard, depth - 1 + checkExtension, alpha, beta, !maximizingPlayer, ply + 1, stopwatch, settings, allowNullMove: true, allowCheckExtension: childAllowCheckExtension);
                }

                if (maximizingPlayer)
                {
                    if (value > bestValue) // เช็คเงื่อนไข update
                    {
                        bestValue = value;
                        bestMove = move; // <--- [แก้จุดที่ 1.2] จำท่าเดิน
                        if (ply < MaxSearchDepth) _pvTable[ply] = move;
                    }
                    alpha = Math.Max(alpha, bestValue);
                }
                else
                {
                    if (value < bestValue) // เช็คเงื่อนไข update
                    {
                        bestValue = value;
                        bestMove = move; // <--- [แก้จุดที่ 1.2] จำท่าเดิน
                        if (ply < MaxSearchDepth) _pvTable[ply] = move;
                    }
                    beta = Math.Min(beta, bestValue);
                }

                if (beta <= alpha)
                {
                    if (board.Board[move.ToX, move.ToY] == 0)
                    {
                        if (ply < MaxSearchDepth)
                        {
                            // เช็คว่าท่านี้ซ้ำกับ Killer ตัวแรกไหม
                            var killer1 = _killerMoves[ply, 0];
                            if (killer1 == null || !MoveOrderer.IsSameMove(killer1, move))
                            {
                                _killerMoves[ply, 1] = _killerMoves[ply, 0]; // ดันตัวเก่าไปเป็นตัวรอง
                                _killerMoves[ply, 0] = move; // ใส่ตัวใหม่เป็นตัวหลัก
                            }
                        }
                        // อัปเดต History
                        AddHistoryScore(move, depth * depth);
                    }
                    break;// Cutoff จริงๆ ค่อย Break ตรงนี้
                }
            }

            // เก็บผลลัพธ์ลง Transposition Table
            TTEntry.TTFlag flag;
            if (bestValue <= originalAlpha)
                flag = TTEntry.TTFlag.UpperBound; // ไม่เจอท่าที่ดีกว่า Alpha เดิม
            else if (bestValue >= originalBeta)
                flag = TTEntry.TTFlag.LowerBound; // ตัดจบเพราะดีเกิน Beta 
            else
                flag = TTEntry.TTFlag.Exact;      // เจอค่าที่แท้จริง
            _tt.Store(board.ZobristKey, depth, bestValue, flag, bestMove);
            return bestValue;
        }

        private int QuiescenceSearch(ChessBoardModel board, int alpha, int beta, bool maximizingPlayer, Stopwatch stopwatch, EvaluationSettings settings, int ply = 0)
        {
            _nodesEvaluated++; // count qsearch nodes

            if (HasTimeLimit && (_nodesEvaluated & 2047) == 0 && stopwatch.Elapsed.TotalMilliseconds > _timeLimitMs)
                return (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            if (board.RepetitionCount >= 3)
                return 0;

            if (ply > 10)
                return (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            ulong key = board.ZobristKey;
            if (_tt.TryGet(key, out TTEntry entry))
            {
                if (entry.Flag == TTEntry.TTFlag.Exact)
                    return (int)entry.Score;

                if (entry.Flag == TTEntry.TTFlag.LowerBound)
                    alpha = Math.Max(alpha, (int)entry.Score);

                else if (entry.Flag == TTEntry.TTFlag.UpperBound)
                    beta = Math.Min(beta, (int)entry.Score);

                if (alpha >= beta)
                    return (int)entry.Score;
            }

            var standPat = (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

            standPat += maximizingPlayer ? 5 : -5;

            int originalAlpha = alpha;
            int originalBeta = beta;

            const int DELTA = 200;
            if (maximizingPlayer)
            {
                if (standPat >= beta)
                {
                    _tt.Store(key, 0, standPat, TTEntry.TTFlag.LowerBound, null);
                    return beta;
                }

                if (standPat + DELTA < alpha)
                {
                    _tt.Store(key, 0, standPat, TTEntry.TTFlag.UpperBound, null);
                    return alpha;
                }

                if (standPat > alpha)
                    alpha = standPat;
            }
            else
            {
                if (standPat <= alpha)
                {
                    _tt.Store(key, 0, standPat, TTEntry.TTFlag.UpperBound, null);
                    return alpha;
                }

                if (standPat - DELTA > beta)
                {
                    _tt.Store(key, 0, standPat, TTEntry.TTFlag.LowerBound, null);
                    return beta;
                }

                if (standPat < beta)
                    beta = standPat;
            }

            // Avoid LINQ allocations in quiescence: filter captures in-place.
            var captures = MoveGenerator.GenerateMoves(board);
            int writeIndex = 0;
            for (int readIndex = 0; readIndex < captures.Count; readIndex++)
            {
                var candidate = captures[readIndex];
                if (!IsQuiescenceTacticalMove(board, candidate))
                {
                    continue;
                }

                captures[writeIndex++] = candidate;
            }
            if (writeIndex < captures.Count)
                captures.RemoveRange(writeIndex, captures.Count - writeIndex);

            MoveOrderer.OrderCaptures(captures, board);

            foreach (var move in captures)
            {
                if (HasTimeLimit && (_nodesEvaluated & 2047) == 0 && stopwatch.Elapsed.TotalMilliseconds > _timeLimitMs)
                    return (int)AIEngine.Evaluation.Evaluation.Evaluate(board, settings);

                var newBoard = board.CloneForSearch();
                newBoard.MakeMove(move);

                int score = QuiescenceSearch(
                    newBoard,
                    alpha,
                    beta,
                    !maximizingPlayer,
                    stopwatch,
                    settings,
                    ply + 1
                );
                if (maximizingPlayer)
                {
                    if (score > alpha) alpha = score;
                    if (alpha >= beta) break;
                }
                else
                {
                    if (score < beta) beta = score;
                    if (beta <= alpha) break;
                }
            }
            int result = maximizingPlayer ? alpha : beta;
            TTEntry.TTFlag flag;

            if (result <= originalAlpha)
                flag = TTEntry.TTFlag.UpperBound;
            else if (result >= originalBeta)
                flag = TTEntry.TTFlag.LowerBound;
            else
                flag = TTEntry.TTFlag.Exact;

            _tt.Store(key, 0, result, flag, null);


            return result;
        }

        private static bool IsQuiescenceTacticalMove(ChessBoardModel board, MoveModel move)
        {
            if (move.PromotionPiece != 0)
            {
                return true; // Promotion is always tactical.
            }

            if (board.Board[move.ToX, move.ToY] != 0)
            {
                return true; // Normal capture.
            }

            int movingPiece = board.Board[move.FromX, move.FromY];
            if (Math.Abs(movingPiece) == 1 &&
                board.EnPassantTarget.HasValue &&
                move.ToX == board.EnPassantTarget.Value.X &&
                move.ToY == board.EnPassantTarget.Value.Y)
            {
                return true; // En passant capture.
            }

            return false; // Non-capture, non-promotion move is not tactical.
        }
    }
}

