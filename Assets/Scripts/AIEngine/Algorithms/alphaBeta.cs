using System;
using System.Diagnostics;
using System.Linq;
using AIEngine.Utilities;
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

        private const int TimeLimitMs = 10000;

        // 2. จองพื้นที่ใน Constructor (ทำครั้งเดียว)
        public AlphaBeta()
        {
            _killerMoves = new MoveModel[MaxSearchDepth, 2];
            _historyMoves = new int[8, 8, 8, 8];
        }

        private int _nodesEvaluated = 0;
        private int _actualDepth = 0;

        public override MoveModel FindBestMove(ChessBoardModel board, int maxDepth)
        {
            var result = FindBestMoveWithMetrics(board, maxDepth);
            return result.Move;
        }

        public override SearchResult FindBestMoveWithMetrics(ChessBoardModel board, int maxDepth)
        {
            _nodesEvaluated = 0;
            _actualDepth = 0;
            var stopwatch = Stopwatch.StartNew();
            MoveModel bestMove = null;
            int bestScore = 0;
            int currentDepth = 1;

            while (currentDepth <= maxDepth && stopwatch.Elapsed.TotalMilliseconds < TimeLimitMs)
            {
                var (iterativeBest, iterativeBestScore) = AlphaBetaSearch(board, currentDepth, stopwatch, bestMove);
                if (iterativeBest != null)
                {
                    bestMove = iterativeBest;
                    bestScore = iterativeBestScore;
                    _actualDepth = currentDepth;
                }
                currentDepth++;
            }

            stopwatch.Stop();
            var elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
            var move = bestMove ?? MoveGenerator.GenerateMoves(board).FirstOrDefault()
                    ?? throw new InvalidOperationException("No valid moves found.");
            return new SearchResult
            {
                Move = move,
                Depth = _actualDepth,
                NodesEvaluated = _nodesEvaluated,
                TimeMs = elapsedMs,
                Score = bestScore,

            };
        }

        private (MoveModel, int) AlphaBetaSearch(ChessBoardModel board, int depth, Stopwatch stopwatch, MoveModel previousBest)
        {
            // [แก้ไข 1] ส่ง _killerMoves และ _historyMoves เข้าไปให้ MoveOrderer
            // สังเกตว่าผมส่ง null แทน ttMove ในพารามิเตอร์ที่ 3 เพราะเราใช้ previousBest เป็นตัวนำทางใน Root แล้ว
            var moves = MoveGenerator.GenerateMoves(board);
            MoveOrderer.OrderMoves(moves, board, previousBest, GetKillers(0), _historyMoves);
            int bestScore = int.MinValue;
            MoveModel bestMove = null;

            int alpha = int.MinValue;
            int beta = int.MaxValue;

            foreach (var move in moves)
            {
                if (stopwatch.Elapsed.TotalMilliseconds > TimeLimitMs) break;

                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int score;
                if (newBoard.RepetitionCount >= 3)
                    score = 0; // draw
                else
                    // [แก้ไข 2] เพิ่มพารามิเตอร์ ply = 0 (เลข 0 หลัง false)
                    score = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, !board.IsWhiteTurn, 1, stopwatch);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, bestScore);

                if (alpha >= beta)
                    break;
            }
            return (bestMove, bestScore); ;
        }

        // Helper: ดึงท่า Killer เฉพาะของ Ply ปัจจุบันออกมาเป็น Array 1 มิติ
        private MoveModel[] GetKillers(int ply)
        {
            // ป้องกัน Array Index Out of Bounds
            if (ply >= MaxSearchDepth) return new MoveModel[2] { null, null };

            // สร้าง Array ใหม่ที่มีแค่ 2 ท่าของชั้นนี้
            return new MoveModel[] { _killerMoves[ply, 0], _killerMoves[ply, 1] };
        }

        private int AlphaBetaRecursive(ChessBoardModel board, int depth, int alpha, int beta, bool maximizingPlayer, int ply, Stopwatch stopwatch)
        {
            _nodesEvaluated++; // นับ node ที่ evaluate

            // ✅ timeout -> return alpha (ไม่ใช่ 0)
            if (stopwatch.Elapsed.TotalMilliseconds > TimeLimitMs) return alpha;

            if (board.IsGameOver())
            {
                // ✅ Evaluation() ของคุณ normalize อยู่แล้ว ไม่ต้องคูณ maximizingPlayer
                return AIEngine.Evaluation.Evaluation.Evaluate(board);
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
                        return entry.Score;

                    // ถ้าเป็น LowerBound (ค่าจริงอาจสูงกว่านี้) และค่าที่เก็บไว้มันสูงกว่า Beta -> ตัดจบได้
                    if (entry.Flag == TTEntry.TTFlag.LowerBound)
                        alpha = Math.Max(alpha, entry.Score);

                    // ถ้าเป็น UpperBound (ค่าจริงอาจต่ำกว่านี้) และค่าที่เก็บไว้มันต่ำกว่า Alpha -> ตัดจบได้
                    else if (entry.Flag == TTEntry.TTFlag.UpperBound)
                        beta = Math.Min(beta, entry.Score);

                    // ถ้า Alpha >= Beta แสดงว่าเราตัดจบได้
                    if (alpha >= beta)
                        return entry.Score;
                }
            }

            // [ต้องเติมกลับมาครับ!] ถ้าความลึกหมดแล้ว ให้ไปค้นหาใน Quiescence Search ต่อ
            if (depth <= 0)
            {
                // ส่ง null ไปในตัวสุดท้าย เพราะ Quiescence ไม่ได้คืนค่า BestMove
                var score = QuiescenceSearch(board, alpha, beta, maximizingPlayer, stopwatch);
                _tt.Store(board.ZobristKey, depth, score, TTEntry.TTFlag.Exact, null);
                return score;
            }

            var moves = MoveGenerator.GenerateMoves(board);
            MoveOrderer.OrderMoves(moves, board, entry?.BestMove, GetKillers(ply), _historyMoves);
            var bestValue = maximizingPlayer ? int.MinValue : int.MaxValue;

            MoveModel bestMove = null;

            int originalAlpha = alpha;
            int originalBeta = beta;

            foreach (var move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move);

                int value = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, !maximizingPlayer, ply + 1, stopwatch);

                if (maximizingPlayer)
                {
                    if (value > bestValue) // เช็คเงื่อนไข update
                    {
                        bestValue = value;
                        bestMove = move; // <--- [แก้จุดที่ 1.2] จำท่าเดิน
                    }
                    alpha = Math.Max(alpha, bestValue);
                }
                else
                {
                    if (value < bestValue) // เช็คเงื่อนไข update
                    {
                        bestValue = value;
                        bestMove = move; // <--- [แก้จุดที่ 1.2] จำท่าเดิน
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
                        _historyMoves[move.FromX, move.FromY, move.ToX, move.ToY] += depth * depth;
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

        private int QuiescenceSearch(ChessBoardModel board, int alpha, int beta, bool maximizingPlayer, Stopwatch stopwatch)
        {
            _nodesEvaluated++; // นับ node ใน quiescence search ด้วย

            if (board.RepetitionCount >= 3)
            {
                return 0;
            }
            var standPat = AIEngine.Evaluation.Evaluation.Evaluate(board);

            if (maximizingPlayer)
            {
                if (standPat >= beta) return beta;
                if (standPat > alpha) alpha = standPat;
            }
            else
            {
                // ฝั่ง Min ต้องพยายามลดค่า Beta และเช็ค Alpha Cutoff
                if (standPat <= alpha) return alpha;
                if (standPat < beta) beta = standPat;
            }

            var captures = MoveGenerator.GenerateMoves(board)
                .Where(m => board.Board[m.ToX, m.ToY] != 0)
                .OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY]));


            foreach (var move in captures)
            {
                if (stopwatch.Elapsed.TotalMilliseconds > TimeLimitMs) return alpha;

                var newBoard = board.Clone();
                newBoard.MakeMove(move);
                int score = QuiescenceSearch(newBoard, alpha, beta, !maximizingPlayer, stopwatch);

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

            return maximizingPlayer ? alpha : beta;
        }
    }
}
