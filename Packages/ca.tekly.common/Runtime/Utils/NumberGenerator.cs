namespace Tekly.Common.Utils
{
    /// <summary>
    /// Random number generator
    /// </summary>
    public class NumberGenerator
    {
        private uint m_seed;
        private uint m_sequence;
        
        public NumberGenerator(uint seed)
        {
            m_seed = seed;
        }

        public void Reset(uint seed)
        {
            m_seed = seed;
            m_sequence = 0;
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
            return Range(0, 1) <= likelyHood;
        }
    }
}