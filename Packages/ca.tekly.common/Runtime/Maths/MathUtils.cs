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

        public static Vector3 Vector3(this float value)
        {
            return new Vector3(value, value, value);
        }
        
        public static int RoundToNearest(int value, int nearest)
        {
            return (int)Math.Round(value / (double) nearest) * nearest;
        }
    }
}