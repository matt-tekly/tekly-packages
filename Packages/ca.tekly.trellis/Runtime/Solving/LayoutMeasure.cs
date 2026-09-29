namespace Tekly.Trellis
{
	/// <summary>
	/// How big something can be along one axis. Sizes exclude margins.
	/// Create values with <see cref="Create"/> so the rules below always hold:
	/// min is at least 0, max is at least min (min wins), preferred sits between min and max,
	/// and flexible is at least 0.
	/// </summary>
	public struct LayoutMeasure
	{
		public float Min;
		public float Preferred;
		public float Flexible;
		public float Max;
		public float MarginStart;
		public float MarginEnd;

		public float Margins => MarginStart + MarginEnd;
		public float OuterMin => Min + Margins;
		public float OuterPreferred => Preferred + Margins;

		public static LayoutMeasure Create(float min, float preferred, float flexible = 0f,
			float max = float.PositiveInfinity, float marginStart = 0f, float marginEnd = 0f)
		{
			min = SolverMath.Max(0f, min);
			max = SolverMath.Max(min, max);

			return new LayoutMeasure {
				Min = min,
				Preferred = SolverMath.Clamp(preferred, min, max),
				Flexible = SolverMath.Max(0f, flexible),
				Max = max,
				MarginStart = marginStart,
				MarginEnd = marginEnd
			};
		}

		public override string ToString()
		{
			return $"(min {Min}, pref {Preferred}, flex {Flexible}, max {Max}, margin {MarginStart}/{MarginEnd})";
		}
	}
}
