using System;

namespace Tekly.Common.Utils
{
    /// <summary>
    /// Random number generator
    /// </summary>
    public class NumberGenerator
    {
        private uint m_seed;
        private uint m_sequence;

        public uint Seed => m_seed;
        public uint Sequence => m_sequence;

        public NumberGenerator()
        {
            m_seed = GenerateUnitySeed();
            m_sequence = 0;
        }

        public NumberGenerator(uint seed, uint sequence = default)
        {
            m_seed = seed;
            m_sequence = sequence;
        }

        public NumberGenerator(NumberGeneratorState state)
        {
            m_seed = state.Seed;
            m_sequence = state.Sequence;
        }

        public void ReSeed()
        {
            ReSeed(GenerateUnitySeed());
        }

        public void ReSeed(uint seed, uint sequence = default)
        {
            m_seed = seed;
            m_sequence = sequence;
        }

        public void ReSeed(NumberGeneratorState state)
        {
            ReSeed(state.Seed, state.Sequence);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            return XXHash.Int(m_seed, m_sequence++, minInclusive, maxExclusive);
        }

        public float Range(float min, float max)
        {
            return XXHash.Float(m_seed, m_sequence++, min, max);
        }

        /// <summary>
        /// Simulates the chance that likelyHood is greater than a number between 0 and 1.
        /// </summary>
        /// <param name="likelyHood">Between 0 and 1</param>
        public bool Chance(float likelyHood)
        {
            return Range(0f, 1f) <= likelyHood;
        }

        public NumberGeneratorState ToState()
        {
            return new NumberGeneratorState {
                Seed = m_seed,
                Sequence = m_sequence
            };
        }

        public static uint GenerateUnitySeed()
        {
            return (uint) UnityEngine.Random.Range(0, int.MaxValue - 1);
        }

        public static NumberGenerator FromUnity()
        {
            return new NumberGenerator(GenerateUnitySeed());
        }
    }

    [Serializable]
    public struct NumberGeneratorState
    {
        public uint Seed;
        public uint Sequence;
    }
}