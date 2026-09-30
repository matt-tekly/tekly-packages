namespace Tekly.Trellis
{
	/// <summary>
	/// The size a member of a size group ends up with. Has no UnityEngine dependency.
	/// </summary>
	public static class SharedSize
	{
		/// <summary>
		/// groupMin and groupPreferred are the largest among the members (including this one). The shared
		/// size is capped at cap (infinity for none), but a member never goes below its own min.
		/// </summary>
		public static void Combine(float ownMin, float groupMin, float groupPreferred, float cap,
			out float min, out float preferred)
		{
			min = SolverMath.Max(ownMin, SolverMath.Min(groupMin, cap));
			preferred = SolverMath.Max(min, SolverMath.Min(groupPreferred, cap));
		}
	}
}
