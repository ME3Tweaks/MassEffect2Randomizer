using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers
{
    /// <summary>
    /// Thread safe random number generator. Call SetSingleThread() if you want a single-thread random mode with a seed. Ensure you do not use RNG in this mode across threads.
    /// </summary>
    public static class ThreadSafeRandom
    {
        /// <summary>
        /// Is this RNG for single thread use only? If so, we can just pull numbers directly
        /// </summary>
        private static bool SingleThreadMode { get; set; }
        private static Random GlobalRandom = new Random();

        public static void SetSingleThread(int seed)
        {
            SingleThreadMode = true;
            GlobalRandom = new Random(seed);
        }

        public static void Reset()
        {
            GlobalRandom = new Random();
            SingleThreadMode = false;
        }

        private static readonly ThreadLocal<Random> LocalRandom = new ThreadLocal<Random>(() =>
        {
            lock (GlobalRandom)
            {
                return new Random(GlobalRandom.Next());
            }
        });

        public static int Next(int min = 0, int max = Int32.MaxValue)
        {
            if (SingleThreadMode)
                return GlobalRandom.Next(min, max);
            return LocalRandom.Value.Next(min, max);
        }

        public static int Next(int max = Int32.MaxValue)
        {
            if (SingleThreadMode)
                return GlobalRandom.Next(max);
            return LocalRandom.Value.Next(max);
        }

        public static float NextFloat(double minValue, double maxValue)
        {
            if (SingleThreadMode)
                return (float)(GlobalRandom.NextDouble() * (maxValue - minValue) + minValue);
            return (float)(LocalRandom.Value.NextDouble() * (maxValue - minValue) + minValue);
        }

        public static float NextFloat()
        {
            if (SingleThreadMode)
                return (float)GlobalRandom.NextDouble();
            return (float)LocalRandom.Value.NextDouble();
        }

        public static double NextDouble()
        {
            if (SingleThreadMode)
                return GlobalRandom.NextDouble();
            return LocalRandom.Value.NextDouble();
        }

    }
}