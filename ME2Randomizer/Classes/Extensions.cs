using System;
using System.Collections.Generic;

namespace ME2Randomizer.Classes
{
    public static class RandomExtensions
    {
        public static float NextFloat(
            this Random random,
            double minValue,
            double maxValue)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }
    }

    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list, Random random = null)
        {
            if (random == null && rng == null) rng = new Random();
            Random randomToUse = random ?? rng;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = randomToUse.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
