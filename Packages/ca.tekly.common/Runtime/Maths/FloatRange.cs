using System;
using Tekly.Common.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Tekly.Common.Maths
{
    [Serializable]
    public class FloatRange
    {
        public float Min;
        public float Max = 1;

        public FloatRange() { }

        public FloatRange(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Get()
        {
            return Random.Range(Min, Max);
        }

        public float Get(NumberGenerator generator)
        {
            if (Max <= Min) {
                return Min;
            }

            return generator.Range(Min, Max);
        }

        public float Lerp(float ratio)
        {
            return Mathf.Lerp(Min, Max, ratio);
        }
        
        public float LerpUnclamped(float ratio)
        {
            return Mathf.LerpUnclamped(Min, Max, ratio);
        }
    }
}