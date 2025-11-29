using UnityEngine;
using System;
using Random = System.Random;
namespace AIEngine.Utilities
{
    public class Zobrist 
    {
        public static ulong[,,] PieceSquare = new ulong[8, 8, 12];
        public static ulong SideToMove;

        static Zobrist()
        {
            Random rand = new System.Random(123456); // Fixed seed for reproducibility
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

        public static ulong ComputeKeyFromBoard(int[,] board, bool isWhiteTurn)
        {
            ulong key = 0UL;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    int p = board[x, y];
                    if (p == 0) continue;
                    int idx = PieceToIndex(p);
                    key ^= PieceSquare[x, y, idx];
                }
            }
            if (!isWhiteTurn) key ^= SideToMove;

            return key;

        }

        public static int PieceToIndex(int piece)
        {
            if (piece == 0) throw new ArgumentException("Empty square has no piece index");
            int  abs = Math.Abs(piece);
            int baseIndex = (abs - 1); //pawn -> 0, knight -> 1, bishop -> 2, rook -> 3, queen -> 4, king -> 5
            return piece > 0 ? baseIndex : baseIndex + 6; // white pieces 0-5, black pieces 6-11
        }
    }
}
