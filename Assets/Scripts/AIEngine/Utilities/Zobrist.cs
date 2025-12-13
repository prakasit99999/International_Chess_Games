using UnityEngine;
using System;
using Random = System.Random;
namespace AIEngine.Utilities
{
    public class Zobrist
    {
        public static ulong SideToMove;
        public static ulong[,,] PieceSquare = new ulong[8, 8, 12];
        public static ulong[] Castling = new ulong[4];
        public static ulong[] EnPassantFile = new ulong[8];

        static Zobrist()
        {
            Random rand = new System.Random(123456); // Fixed seed for reproducibility

            for (int c = 0; c < 4; c++)
            {
                Castling[c] = RandomUlong(rand);
            }

            for (int f = 0; f < 8; f++)
            {
                EnPassantFile[f] = RandomUlong(rand);
            }
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int pice = 0; pice < 12; pice++)
                    {
                        PieceSquare[x, y, pice] = RandomUlong(rand);
                    }
                }
            }

            SideToMove = RandomUlong(rand);
        }

        private static ulong RandomUlong(Random rng)
        {
            byte[] buffer = new byte[8];
            rng.NextBytes(buffer);
            return System.BitConverter.ToUInt64(buffer, 0);
        }

        // เพิ่มเมธอดนี้ใน Zobrist.cs
        public static ulong ComputeKeyFromModel(ChessBoardModel model)
        {
            ulong key = 0UL;

            // piece squares
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int p = model.Board[x, y];
                    if (p != 0)
                    {
                        int idx = PieceToIndex(p);
                        key ^= PieceSquare[x, y, idx];
                    }
                }
            }

            // side to move: XOR only if it's Black to move (convention: XOR when not white)
            if (!model.IsWhiteTurn)
                key ^= SideToMove;

            // castling rights (use your boolean flags)
            if (!model.WhiteKingMoved)
            {
                if (!model.WhiteRookKingSideMoved) key ^= Castling[(int)ChessBoardModel.CastleIndex.WK];
                if (!model.WhiteRookQueenSideMoved) key ^= Castling[(int)ChessBoardModel.CastleIndex.WQ];
            }
            if (!model.BlackKingMoved)
            {
                if (!model.BlackRookKingSideMoved) key ^= Castling[(int)ChessBoardModel.CastleIndex.BK];
                if (!model.BlackRookQueenSideMoved) key ^= Castling[(int)ChessBoardModel.CastleIndex.BQ];
            }

            // en-passant: XOR by file (Y). Use EnPassantTarget.Value.Y as file index
            if (model.EnPassantTarget.HasValue)
            {
                int file = model.EnPassantTarget.Value.Y;
                if (file >= 0 && file < 8)
                    key ^= EnPassantFile[file];
            }

            return key;
        }


        public static int PieceToIndex(int piece)
        {
            if (piece == 0) throw new ArgumentException("Empty square has no piece index");
            int abs = Math.Abs(piece);
            int baseIndex = (abs - 1); //pawn -> 0, knight -> 1, bishop -> 2, rook -> 3, queen -> 4, king -> 5
            return piece > 0 ? baseIndex : baseIndex + 6; // white pieces 0-5, black pieces 6-11
        }
    }
}
