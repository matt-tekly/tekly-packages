// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using UnityEngine;

namespace Tekly.Common.Maths
{
    public static class MathUtils
    {
        public static double DoubleEpsilon = 0.0000000001d;
        
        public static bool IsApproximately(double a, double b)
        {
            return Math.Abs(a - b) < DoubleEpsilon; 
        }

        public static double Lerp(double a, double b, float t)
        {
            return a + (b - a) * Mathf.Clamp01(t);
        }

        public static Vector3 Vector3(float value)
        {
            return new Vector3(value, value, value);
        }
    }
}