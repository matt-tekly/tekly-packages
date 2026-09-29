namespace Tekly.Trellis
{
	/// <summary>
	/// Float helpers so the solver has no UnityEngine dependency and can be tested anywhere.
	/// </summary>
	internal static class SolverMath
	{
		public const float EPSILON = 0.001f;

		public static float Max(float a, float b)
		{
			return a > b ? a : b;
		}

		public static float Min(float a, float b)
		{
			return a < b ? a : b;
		}

		public static float Clamp(float value, float min, float max)
		{
			if (value < min) {
				return min;
			}

			return value > max ? max : value;
		}

		public static float Lerp(float a, float b, float t)
		{
			return a + (b - a) * t;
		}

		/// <summary>
		/// Clamped 0-1. Returns 1 when a and b are (nearly) equal, so nothing counts as squeezed.
		/// </summary>
		public static float InverseLerp(float a, float b, float value)
		{
			if (b - a <= EPSILON) {
				return 1f;
			}

			return Clamp((value - a) / (b - a), 0f, 1f);
		}
	}
}
