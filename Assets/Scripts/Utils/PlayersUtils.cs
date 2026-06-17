// Assets/Scripts/Utils/PlayersUtils.cs
using System;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class PlayerColorSetup
    {
        public string playerColor;
        public string aiColor;
    }

    public static class PlayersUtils
    {
        public const string White = "White";
        public const string Black = "Black";
        public const string Random = "Random";

        public static PlayerColorSetup ResolveSinglePlayerColors(string playerChoice)
        {
            string playerColor = NormalizeChoice(playerChoice);

            if (playerColor == Random)
                playerColor = UnityEngine.Random.value < 0.5f ? White : Black;

            return new PlayerColorSetup
            {
                playerColor = playerColor,
                aiColor = playerColor == White ? Black : White
            };
        }

        private static string NormalizeChoice(string choice)
        {
            if (string.IsNullOrWhiteSpace(choice))
                return White;

            switch (choice.Trim().ToLowerInvariant())
            {
                case "white":
                    return White;
                case "black":
                    return Black;
                case "random":
                    return Random;
                default:
                    return White;
            }
        }
    }
}
