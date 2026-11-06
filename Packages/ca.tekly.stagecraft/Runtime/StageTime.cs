namespace Tekly.Stagecraft
{
	/// <summary>
	/// The stage's clock. Deltas come in unscaled from whoever drives the stage and are scaled here. The stage keeps
	/// no accumulator of its own: the fixed step is whatever calls FixedTick.
	/// </summary>
	public sealed class StageTime
	{
		/// <summary>Scaled seconds of presentation time, summed from Tick.</summary>
		public float Elapsed { get; internal set; }

		/// <summary>Scaled delta of the current or last Tick. Zero while paused.</summary>
		public float DeltaTime { get; internal set; }

		/// <summary>Scaled seconds of gameplay time, summed from FixedTick.</summary>
		public float FixedElapsed { get; internal set; }

		/// <summary>Scaled delta of the current or last FixedTick. Zero while paused.</summary>
		public float FixedDeltaTime { get; internal set; }

		/// <summary>Number of FixedTicks so far, paused ones excluded.</summary>
		public int FixedStep { get; internal set; }

		/// <summary>Multiplies every delta. 1 by default.</summary>
		public float Scale { get; set; } = 1f;

		/// <summary>While paused nothing ticks and time doesn't advance, but sync points still run.</summary>
		public bool IsPaused { get; set; }
	}
}
