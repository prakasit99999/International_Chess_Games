using System.Collections.Generic;
using System.Linq;

namespace AIEngine.Utilities
{
    public class OpeningBook
    {
        private static readonly Dictionary<string, List<MoveModel>> Book = new Dictionary<string, List<MoveModel>>();

        static OpeningBook()
        {
            // ตัวอย่างตาเดินเริ่มต้น (Starting Position)
            // คีย์คือบอร์ดเริ่มต้น (Serialized or Hash)
            // ในที่นี้สมมติใช้การ Serialize แบบง่ายๆ หรือเช็คสถานะเริ่มต้น
            InitializeStandardOpenings();
        }

        private static void InitializeStandardOpenings()
        {
            // ตาแรกสำหรับสีขาว (White's first moves)
            // จากบอร์ดเริ่มต้น: e2 -> e4 (6,4 -> 4,4)
            var startingBoardKey = GetStartingBoardKey();
            Book[startingBoardKey] = new List<MoveModel>
            {
                new MoveModel(6, 4, 4, 4), // e4 (King's Pawn)
                new MoveModel(6, 3, 4, 3), // d4 (Queen's Pawn)
                new MoveModel(7, 6, 5, 5), // Nf3 (Reti Opening)
                new MoveModel(6, 2, 4, 2)  // c4 (English Opening)
            };
        }

        public static MoveModel GetMove(ChessBoardModel board)
        {
            // ในที่นี้เราจะเช็คแค่ตาแรกเพื่อเป็นตัวอย่าง (Starting Position)
            if (IsStartingPosition(board))
            {
                var key = GetStartingBoardKey();
                if (Book.ContainsKey(key))
                {
                    var moves = Book[key];
                    return moves[new System.Random().Next(moves.Count)];
                }
            }
            return null;
        }

        private static bool IsStartingPosition(ChessBoardModel board)
        {
            // ตรวจสอบว่านี่คือตาแรกของเกมหรือไม่ (Full board scan หรือ check turn count)
            return board.CurrentTurn == 0 || board.Board[6, 4] == 1 && board.Board[1, 4] == -1 && board.Board[4, 4] == 0;
        }

        private static string GetStartingBoardKey()
        {
            return "STARTING_POS";
        }
    }
}
