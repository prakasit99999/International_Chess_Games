using UnityEngine;
namespace Utils
{
    public static class GameConstants
    {
        public const string PlayerPrefsKey = "ChessGameSettings";
    }

    public class GameUtils
    {
        public static string FormatTime(float seconds)
        {
            int min = Mathf.FloorToInt(seconds / 60f);
            int sec = Mathf.FloorToInt(seconds % 60f);
            return $"{min:00}:{sec:00}";
        }

    }
}
