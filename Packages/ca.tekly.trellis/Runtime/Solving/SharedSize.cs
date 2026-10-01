namespace Tekly.Trellis
{
	/// <summary>
	/// The size a member of a size group ends up with. Has no UnityEngine dependency.
	/// </summary>
	public static class SharedSize
	{
		/// <summary>
		/// groupMin and groupPreferred are the largest among the members (including this one). The shared
		/// size is raised to floor (0 for none) and capped at cap (infinity for none); floor wins if they
		/// conflict. A member never goes below its own min.
		/// </summary>
		public static void Combine(float ownMin, float groupMin, float groupPreferred, float floor, float cap,
			out float min, out float preferred)
		{
			min = SolverMath.Max(ownMin, Limit(groupMin, floor, cap));
			preferred = SolverMath.Max(min, Limit(groupPreferred, floor, cap));
		}

		private static float Limit(float value, float floor, float cap)
		{
			return SolverMath.Max(SolverMath.Min(value, cap), floor);
		}
	}
}
