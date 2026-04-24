using System;
using System.Collections.Generic;
namespace AIEngine.Utilities
{
    public static class MoveGenerator
    {
        public static List<MoveModel> GenerateMoves(ChessBoardModel board)
        {
            List<MoveModel> moves = new List<MoveModel>();
            int[,] currentBoard = board.Board;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int piece = currentBoard[x, y];
                    if (piece == 0) continue;

                    bool isCurrentPlayerPiece = (board.IsWhiteTurn && piece > 0) || (!board.IsWhiteTurn && piece < 0);
                    if (!isCurrentPlayerPiece) continue;

                    switch (Math.Abs(piece))
                    {
                        case 1: // เบี้ย
                            GeneratePawnMoves(board, x, y, moves);
                            break;
                        case 2: // ม้า
                            GenerateKnightMoves(board, x, y, moves);
                            break;
                        case 3: // บิชอป
                            GenerateBishopMoves(board, x, y, moves);
                            break;
                        case 4: // เรือ
                            GenerateRookMoves(board, x, y, moves);
                            break;
                        case 5: // ควีน
                            GenerateQueenMoves(board, x, y, moves);
                            break;
                        case 6: // ราชา
                            GenerateKingMoves(board, x, y, moves);
                            break;
                    }
                }
            }
            var legalMoves = FilterLegalMoves(board, moves);
            // Prevent illegal "king capture" outputs without LINQ allocation.
            for (int i = legalMoves.Count - 1; i >= 0; i--)
            {
                var move = legalMoves[i];
                if (Math.Abs(board.Board[move.ToX, move.ToY]) == 6)
                {
                    legalMoves.RemoveAt(i);
                }
            }

            return legalMoves;
        }
        // ตรวจสอบการเดินที่ถูกต้องตามกฎ
        private static List<MoveModel> FilterLegalMoves(ChessBoardModel board, List<MoveModel> pseudoMoves)
        {
            List<MoveModel> legalMoves = new List<MoveModel>(pseudoMoves.Count);
            bool isWhite = board.IsWhiteTurn;

            foreach (MoveModel move in pseudoMoves)
            {
                bool moved = false;
                try
                {
                    board.MakeMoveUnsafe(move);
                    moved = true;

                    if (!board.IsInCheck(isWhite))
                    {
                        legalMoves.Add(move);
                    }
                }
                finally
                {
                    if (moved)
                    {
                        board.UndoMoveUnsafe();
                    }
                }
            }
            return legalMoves;
        }
        // ========== ฟังก์ชันสร้างการเดินของหมากแต่ละประเภท ==========
        private static void GeneratePawnMoves(ChessBoardModel board, int x, int y, List<MoveModel> moves)
        {
            int direction = board.IsWhiteTurn ? -1 : 1;
            int startRow = board.IsWhiteTurn ? 6 : 1;

            // เดินหน้า 1 ช่อง (ช่องว่าง)
            int newX = x + direction;
            if (newX >= 0 && newX < 8 && board.Board[newX, y] == 0)
            {
                AddPawnMove(board, x, y, newX, y, moves);
            }

            // เดินหน้า 2 ช่อง (เริ่มต้น)
            if (x == startRow && board.Board[newX, y] == 0 && board.Board[newX + direction, y] == 0)
            {
                AddPawnMove(board, x, y, newX + direction, y, moves);
            }

            // โจมตีทแยง (มีศัตรู)
            int[] captureY = { y - 1, y + 1 };
            foreach (int cy in captureY)
            {
                if (cy < 0 || cy >= 8) continue;
                int targetX = x + direction;
                if (targetX < 0 || targetX >= 8) continue;

                int targetPiece = board.Board[targetX, cy];
                if (targetPiece != 0 && ((board.IsWhiteTurn && targetPiece < 0) || (!board.IsWhiteTurn && targetPiece > 0)))
                {
                    AddPawnMove(board, x, y, targetX, cy, moves);
                }
            }

            // En Passant
            if (board.EnPassantTarget.HasValue)
            {
                Square enPassantSquare = board.EnPassantTarget.Value;
                int enPassantX = enPassantSquare.X;
                int enPassantY = enPassantSquare.Y;

                if (enPassantX < 0 || enPassantX >= 8 || enPassantY < 0 || enPassantY >= 8)
                {
                    return;
                }

                bool isCorrectRank = (board.IsWhiteTurn && x == 3) || (!board.IsWhiteTurn && x == 4);
                bool isAdjacentFile = Math.Abs(y - enPassantY) == 1;
                bool isOneStepForward = enPassantX == x + direction;

                if (isCorrectRank && isAdjacentFile && isOneStepForward)
                {
                    // ✅ จุดปลายทางของ En Passant คือ EnPassantTarget
                    moves.Add(new MoveModel(x, y, enPassantX, enPassantY));
                }
            }
        }

        private static void AddPawnMove(ChessBoardModel board, int fromX, int fromY, int toX, int toY, List<MoveModel> moves)
        {
            if (toX == 0 || toX == 7)
            {
                int[] promotionPieces = { 5, 4, 3, 2 };
                foreach (int piece in promotionPieces)
                {
                    MoveModel promoMove = new MoveModel(fromX, fromY, toX, toY);
                    promoMove.PromotionPiece = board.IsWhiteTurn ? piece : -piece; // <-- ใช้งาน setter
                    moves.Add(promoMove);
                }
            }
            else
            {
                moves.Add(new MoveModel(fromX, fromY, toX, toY));
            }
        }

        private static void GenerateKnightMoves(ChessBoardModel board, int x, int y, List<MoveModel> moves)
        {
            int[,] knightMoves = {
            {2, 1}, {2, -1}, {-2, 1}, {-2, -1},
            {1, 2}, {1, -2}, {-1, 2}, {-1, -2}
        };

            for (int i = 0; i < knightMoves.GetLength(0); i++)
            {
                int newX = x + knightMoves[i, 0];
                int newY = y + knightMoves[i, 1];

                if (newX >= 0 && newX < 8 && newY >= 0 && newY < 8)
                {
                    int targetPiece = board.Board[newX, newY];
                    if (targetPiece == 0 || (board.IsWhiteTurn ? targetPiece < 0 : targetPiece > 0))
                    {
                        moves.Add(new MoveModel(x, y, newX, newY));
                    }
                }
            }
        }

        private static void GenerateBishopMoves(ChessBoardModel board, int x, int y, List<MoveModel> moves)
        {
            int[,] directions = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
            GenerateSlidingMoves(board, x, y, directions, moves);
        }

        private static void GenerateRookMoves(ChessBoardModel board, int x, int y, List<MoveModel> moves)
        {
            int[,] directions = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            GenerateSlidingMoves(board, x, y, directions, moves);
        }

        private static void GenerateQueenMoves(ChessBoardModel board, int x, int y, List<MoveModel> moves)
        {
            GenerateBishopMoves(board, x, y, moves);
            GenerateRookMoves(board, x, y, moves);
        }

        private static void GenerateKingMoves(ChessBoardModel board, int x, int y, List<MoveModel> moves)
        {
            int[,] kingMoves = {
            {1, 0}, {-1, 0}, {0, 1}, {0, -1},
            {1, 1}, {1, -1}, {-1, 1}, {-1, -1}
        };

            // เดินปกติ
            for (int i = 0; i < kingMoves.GetLength(0); i++)
            {
                int newX = x + kingMoves[i, 0];
                int newY = y + kingMoves[i, 1];

                if (newX >= 0 && newX < 8 && newY >= 0 && newY < 8)
                {
                    int targetPiece = board.Board[newX, newY];
                    if (targetPiece == 0 || (board.IsWhiteTurn ? targetPiece < 0 : targetPiece > 0))
                    {
                        moves.Add(new MoveModel(x, y, newX, newY));
                    }
                }
            }

            // Castling
            int piece = board.Board[x, y];
            bool isWhite = piece > 0;
            int row = isWhite ? 7 : 0;

            // Kingside Castling
            if (CanCastleKingside(board, isWhite))
            {
                moves.Add(new MoveModel(x, y, row, 6));
            }

            // Queenside Castling
            if (CanCastleQueenside(board, isWhite))
            {
                moves.Add(new MoveModel(x, y, row, 2));
            }
        }
        // ========== ตรวจสอบเงื่อนไข Castling ==========
        private static bool CanCastleKingside(ChessBoardModel board, bool isWhite)
        {
            int row = isWhite ? 7 : 0;
            bool kingMoved = isWhite ? board.WhiteKingMoved : board.BlackKingMoved;
            bool rookMoved = isWhite ? board.WhiteRookKingSideMoved : board.BlackRookKingSideMoved;

            // ตรวจสอบว่า Rook ยังอยู่ที่ตำแหน่งเริ่มต้น และไม่ถูกกิน
            bool rookPresent = board.Board[row, 7] == (isWhite ? 4 : -4);

            return !kingMoved && !rookMoved && rookPresent &&
                   board.Board[row, 5] == 0 && board.Board[row, 6] == 0 &&
                   !IsSquareUnderAttack(board, new Square(row, 4)) &&
                   !IsSquareUnderAttack(board, new Square(row, 5)) &&
                   !IsSquareUnderAttack(board, new Square(row, 6));
        }

        private static bool CanCastleQueenside(ChessBoardModel board, bool isWhite)
        {
            int row = isWhite ? 7 : 0;
            bool kingMoved = isWhite ? board.WhiteKingMoved : board.BlackKingMoved;
            bool rookMoved = isWhite ? board.WhiteRookQueenSideMoved : board.BlackRookQueenSideMoved;

            // ตรวจสอบว่า Rook ยังอยู่ที่ตำแหน่งเริ่มต้น และไม่ถูกกิน
            bool rookPresent = board.Board[row, 0] == (isWhite ? 4 : -4);

            return !kingMoved && !rookMoved && rookPresent &&
                   board.Board[row, 1] == 0 && board.Board[row, 2] == 0 && board.Board[row, 3] == 0 &&
                   !IsSquareUnderAttack(board, new Square(row, 4)) &&
                   !IsSquareUnderAttack(board, new Square(row, 3)) &&
                   !IsSquareUnderAttack(board, new Square(row, 2));
        }
        // ========== ตรวจสอบความปลอดภัย ==========
        private static bool IsSquareUnderAttack(ChessBoardModel board, Square square)
        {
            return board.IsSquareUnderAttack(square, !board.IsWhiteTurn);
        }

        private static void GenerateSlidingMoves(ChessBoardModel board, int x, int y, int[,] directions, List<MoveModel> moves)
        {
            for (int d = 0; d < directions.GetLength(0); d++)
            {
                int dx = directions[d, 0];
                int dy = directions[d, 1];

                for (int step = 1; step < 8; step++)
                {
                    int newX = x + dx * step;
                    int newY = y + dy * step;

                    if (newX < 0 || newX >= 8 || newY < 0 || newY >= 8) break;

                    int targetPiece = board.Board[newX, newY];
                    if (targetPiece == 0)
                    {
                        moves.Add(new MoveModel(x, y, newX, newY));
                    }
                    else
                    {
                        if ((board.IsWhiteTurn && targetPiece < 0) || (!board.IsWhiteTurn && targetPiece > 0))
                        {
                            moves.Add(new MoveModel(x, y, newX, newY));
                        }
                        break;
                    }
                }
            }
        }
    }
}
