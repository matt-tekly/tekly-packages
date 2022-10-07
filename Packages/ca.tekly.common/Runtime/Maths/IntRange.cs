using System;
using Tekly.Common.Utils;
using Random = UnityEngine.Random;

namespace Tekly.Common.Maths
{
    [Serializable]
    public class IntRange
    {
        public int Min;
        public int Max = 1;

        public IntRange() { }

        public IntRange(int min, int max)
        {
            Min = min;
            Max = max;
        }
        
        public int Get()
        {
            return Random.Range(Min, Max);
        }
        
        public int Get(NumberGenerator generator)
        {
            if (Max <= Min) {
                return Min;
            }
            
            return generator.Range(Min, Max);
        }
    }
}