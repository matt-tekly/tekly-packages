// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

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

        public FloatRange()
        {
            
        }

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
            return generator.Range(Min, Max);
        }
    }
}