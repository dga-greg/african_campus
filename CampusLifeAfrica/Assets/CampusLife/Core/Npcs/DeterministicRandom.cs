using System;

namespace CampusLife.Core.Npcs
{
    /// <summary>
    /// SplitMix64 generator. Identical sequences on every runtime (Mono, IL2CPP,
    /// .NET), so rosters and schedules are reproducible everywhere.
    /// </summary>
    public struct DeterministicRandom
    {
        ulong _state;

        public DeterministicRandom(ulong seed) { _state = seed; }

        public static DeterministicRandom From(string key, long salt = 0) => new DeterministicRandom(Hash(key) ^ (ulong)salt * 0x9E3779B97F4A7C15UL);

        public static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            if (s != null)
                foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        public ulong NextULong()
        {
            ulong z = _state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextULong() % (ulong)(maxExclusive - minInclusive));
        }

        public float Range(float min, float max) => (float)(min + (max - min) * NextDouble());

        public bool Chance(double p) => NextDouble() < p;

        public T Pick<T>(T[] items) => items[Range(0, items.Length)];
    }
}
