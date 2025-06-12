//using AI_MinMax_AlphaBeta.Models;

//namespace AI_MinMax_AlphaBeta.AlphaBeta
//{
//    public class AlphaBeta : SearchAlgorithm
//    {
//        private readonly Dictionary<string, int> _transpositionTable = new Dictionary<string, int>();
//        private const int TimeLimitMs = 10000;

//        public override Move FindBestMove(ChessBoard board, int maxDepth)
//        {
//            var startTime = DateTime.Now;
//            Move? bestMove = null;
//            var currentDepth = 1;

//            while (currentDepth <= maxDepth && (DateTime.Now - startTime).TotalMilliseconds < TimeLimitMs)
//            {
//                var iterativeBest = AlphaBetaSearch(board, currentDepth, startTime, bestMove);
//                if (iterativeBest != null)
//                {
//                    bestMove = iterativeBest;
//                    //Console.WriteLine($"Depth {currentDepth} Best: {bestMove}");
//                }
//                currentDepth++;
//            }
//            return bestMove ?? MoveGenerator.GenerateMoves(board).FirstOrDefault() ?? throw new InvalidOperationException("No valid moves found.");
//        }


//        private Move AlphaBetaSearch(ChessBoard board, int depth, DateTime startTime, Move previousBest)
//        {
//            var moves = OrderMoves(MoveGenerator.GenerateMoves(board), board, null);
//            var bestScore = int.MinValue;
//            Move? bestMove = null;

//            foreach (var move in moves)
//            {
//                if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) break;

//                var newBoard = board.Clone();
//                newBoard.MakeMove(move);
//                var score = -AlphaBetaRecursive(newBoard, depth - 1, int.MinValue, int.MaxValue, false, startTime);

//                if (score > bestScore)
//                {
//                    bestScore = score;
//                    bestMove = move;
//                }
//            }
//            return bestMove;
//        }

//        private int AlphaBetaRecursive(ChessBoard board, int depth, int alpha, int beta, bool maximizingPlayer, DateTime startTime)
//        {
//            if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) return 0;
//            if (board.IsGameOver()) return Evaluation.Evaluate(board) * (maximizingPlayer ? 1 : -1);

//            var boardKey = $"{board.SerializeBoard()}{maximizingPlayer}";
//            if (_transpositionTable.TryGetValue(boardKey, out var cached)) return cached;

//            if (depth <= 0)
//            {
//                var score = QuiescenceSearch(board, alpha, beta, startTime);
//                _transpositionTable[boardKey] = score;
//                return score;
//            }

//            var moves = OrderMoves(MoveGenerator.GenerateMoves(board), board, null);
//            var bestValue = maximizingPlayer ? int.MinValue : int.MaxValue;

//            foreach (var move in moves)
//            {
//                var newBoard = board.Clone();
//                newBoard.MakeMove(move);
//                var value = AlphaBetaRecursive(newBoard, depth - 1, alpha, beta, !maximizingPlayer, startTime);

//                bestValue = maximizingPlayer
//                    ? Math.Max(bestValue, value)
//                    : Math.Min(bestValue, value);

//                if (maximizingPlayer)
//                    alpha = Math.Max(alpha, bestValue);
//                else
//                    beta = Math.Min(beta, bestValue);

//                if (beta <= alpha) break;
//            }

//            _transpositionTable[boardKey] = bestValue;
//            return bestValue;
//        }

//        private int QuiescenceSearch(ChessBoard board, int alpha, int beta, DateTime startTime)
//        {
//            var standPat = Evaluation.Evaluate(board);
//            if (standPat >= beta) return beta;
//            alpha = Math.Max(alpha, standPat);

//            var captures = MoveGenerator.GenerateMoves(board)
//                .Where(m => board.Board[m.ToX, m.ToY] != 0)
//                .OrderByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY]));

//            foreach (var move in captures)
//            {
//                if ((DateTime.Now - startTime).TotalMilliseconds > TimeLimitMs) return alpha;

//                var newBoard = board.Clone();
//                newBoard.MakeMove(move);
//                var score = -QuiescenceSearch(newBoard, -beta, -alpha, startTime);

//                alpha = Math.Max(alpha, -score);
//                if (alpha >= beta) break;
//            }
//            return alpha;
//        }

//        private List<Move> OrderMoves(List<Move> moves, ChessBoard board, Move? previousBest)
//        {
//            return moves.OrderByDescending(m => GetMoveScore(m, board))
//                .ThenByDescending(m => Math.Abs(board.Board[m.ToX, m.ToY]))
//                .ToList();
//        }

//        private int GetMoveScore(Move move, ChessBoard board)
//        {
//            // MVV-LVA Scoring
//            var victimValue = Math.Abs(board.Board[move.ToX, move.ToY]);
//            var aggressorValue = Math.Abs(board.Board[move.FromX, move.FromY]);
//            return victimValue * 10 - aggressorValue;
//        }
//    }
//}