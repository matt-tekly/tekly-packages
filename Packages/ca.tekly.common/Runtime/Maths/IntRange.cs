// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

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

        public int Get()
        {
            return Random.Range(Min, Max);
        }
        
        public int Get(NumberGenerator generator)
        {
            return generator.Range(Min, Max);
        }
    }
}