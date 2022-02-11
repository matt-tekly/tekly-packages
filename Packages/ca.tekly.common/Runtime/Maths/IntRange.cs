// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
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
    }
}