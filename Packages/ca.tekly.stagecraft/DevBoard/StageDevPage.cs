using Tekly.DevBoard;
using Tekly.DevBoard.Nodes;

namespace Tekly.Stagecraft.Integration
{
	/// <summary>
	/// A DevBoard page for a stage: counts, time controls, per-group sizes and a Clear button. Costs nothing in
	/// builds without DevBoard; the page calls compile out.
	/// </summary>
	public static class StageDevPage
	{
		/// <summary>Adds the page at <paramref name="path"/>; removed with DevBoard.Release(owner) or when a GameObject owner is destroyed.</summary>
		public static void Register(Stage stage, string path = "Stage", object owner = null)
		{
			DevBoard.DevBoard.Page(path, segment => Build(segment, stage), "Stage", 0, owner);
			DevBoard.DevBoard.Command($"{path.ToLowerInvariant()} clear", stage.Clear, "Despawns every actor", owner);
		}

		/// <summary>Adds the page into a handle; removed with handle.Dispose().</summary>
		public static void Register(ref PageHandle handle, Stage stage, string path = "Stage")
		{
			DevBoard.DevBoard.Page(ref handle, path, segment => Build(segment, stage), "Stage");
		}

		private static void Build(Segment segment, Stage stage)
		{
			segment.Property("Alive", () => stage.AliveCount).WithMonospaced();
			segment.Property("Registered", () => stage.RegisteredCount).WithMonospaced();
			segment.Property("Pending add / remove", () => $"{stage.PendingAddCount} / {stage.PendingRemoveCount}").WithMonospaced();
			segment.Property("Elapsed", () => stage.Time.Elapsed, "{0:N2}").WithMonospaced();
			segment.Property("Fixed step", () => stage.Time.FixedStep).WithMonospaced();

			segment.Toggle("Paused", () => stage.Time.IsPaused, value => stage.Time.IsPaused = value);
			segment.Slider("Time scale", 0f, 3f, () => stage.Time.Scale, value => stage.Time.Scale = value);
			segment.Button("Clear", stage.Clear);

			var groups = segment.Foldout("Tick groups");
			foreach (var group in stage.Groups) {
				var captured = group;
				var label = captured.HasSystem ? $"{captured.Type.Name} (system)" : captured.Type.Name;
				groups.Property(label, () => $"{captured.RawCount} all, {captured.TickCount} tick, {captured.FixedTickCount} fixed").WithMonospaced();
			}

			var pools = segment.Foldout("Pools");
			pools.Property("Prefabs", () => stage.Pools.Count).WithMonospaced();
			pools.Property("Instances", () => {
				var total = 0;
				var free = 0;
				foreach (var pool in stage.Pools) {
					total += pool.TotalCount;
					free += pool.FreeCount;
				}

				return $"{total} total, {free} free";
			}).WithMonospaced();
			pools.Property("Scene actors", () => stage.AdoptedCount).WithMonospaced();
		}
	}
}
