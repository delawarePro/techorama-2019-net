using System;

namespace Shared.DataGeneration
{
    public static class Randomizer
    {
        private static readonly Random GlobalRandom = new Random();
        [ThreadStatic]
        private static Random LocalRandom;

        private static Random GetRandom()
        {
            if (LocalRandom == null)
            {
                lock (GlobalRandom)
                {
                    if (LocalRandom == null)
                    {
                        int seed = GlobalRandom.Next();
                        LocalRandom = new Random(seed);
                    }
                }
            }

            return LocalRandom;
        }

        public static T Randomize<T>(params T[] values)
        {
            if (values == null || values.Length == 0) return default;

            return values[GetRandom().Next(0, values.Length)];
        }

        public static int RandomInt(int minimum, int maximum)
        {
            return GetRandom().Next(minimum, maximum);
        }

        public static DateTime RandomDate(DateTime minimum, DateTime maximium)
        {
            var nofDays = (int)maximium.Subtract(minimum).TotalDays;
            return minimum.AddDays(GetRandom().Next(0, nofDays));
        }
    }
}
