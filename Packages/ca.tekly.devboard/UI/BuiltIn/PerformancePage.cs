using Tekly.Common.LifeCycles;
using Tekly.DevBoard.Nodes;
using Unity.Profiling;
using UnityEngine;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// Frame rate, rendering counters and memory. Readable in an overlay panel, for keeping an eye on while playing.
	///
	/// Nothing is measured until the page is first shown. From then on it keeps measuring, so the numbers are
	/// ready when it's shown again.
	/// </summary>
	internal static class PerformancePage
	{
		public const string PATH = "System/Performance";

		private const float BYTES_PER_MB = 1024f * 1024f;
		private const float BYTES_PER_KB = 1024f;

		// Counters read as noise when redrawn every tick
		private const float READ_INTERVAL = 0.5f;

		// Frames averaged for per-frame counters, which jump around from frame to frame
		private const int AVERAGED_FRAMES = 30;

		// Values keep these widths, so the columns don't move as numbers change length
		private const int FRAME_WIDTH = 8;
		private const int COUNTER_WIDTH = 11;
		private const int BYTES_WIDTH = 11;

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, FramesAndRendering, order: 0, owner: owner);
			DevBoard.Page(PATH, Memory, order: 1, owner: owner);
		}

		/// <summary>
		/// Frames and rendering side by side, since both are short.
		/// </summary>
		private static void FramesAndRendering(Segment page)
		{
			var row = page.Row().WithPadding(0);
			Frames(page, Column(row, "Frames"));
			Rendering(page, Column(row, "Rendering"));
		}

		private static ContainerNode Column(ContainerNode row, string heading)
		{
			var column = row.Stack().WithFlexibleWidth();
			column.Heading(heading);

			return column.Form();
		}

		private static void Frames(Segment page, ContainerNode form)
		{
			var frames = new FrameStats();

			LifeCycle.Instance.Update += frames.Tick;
			page.OnDispose(() => LifeCycle.Instance.Update -= frames.Tick);

			form.Property("FPS", () => frames.Fps, 0.5f, "{0:F0}").WithMonospaced().WithValueWidth(FRAME_WIDTH);
			form.Property("Frame", () => frames.FrameMs, 0.05f, "{0:F1} ms").WithMonospaced().WithValueWidth(FRAME_WIDTH);
			form.Property("1% Low", () => frames.OnePercentLow, 0.5f, "{0:F0}").WithMonospaced().WithValueWidth(FRAME_WIDTH);
			form.Button("Reset", frames.Reset).InteractiveOnly();
		}

		private static void Rendering(Segment page, ContainerNode form)
		{
			Counter(page, form, "SetPass Calls", ProfilerCategory.Render, "SetPass Calls Count");
			Counter(page, form, "Batches", ProfilerCategory.Render, "Batches Count");
			Counter(page, form, "Draw Calls", ProfilerCategory.Render, "Draw Calls Count");
			Counter(page, form, "Vertices", ProfilerCategory.Render, "Vertices Count");
			Counter(page, form, "Triangles", ProfilerCategory.Render, "Triangles Count");
		}

		private static void Memory(Segment page)
		{
			var form = page.Foldout("Memory", Styles.SubtleFoldout).Form();
			Bytes(page, form, "Total Used", "Total Used Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, form, "Total Reserved", "Total Reserved Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, form, "GC Used", "GC Used Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, form, "GC Reserved", "GC Reserved Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, form, "GC Alloc / Frame", "GC Allocated In Frame", BYTES_PER_KB, "{0:N1} KB", AVERAGED_FRAMES);

#if UNITY_EDITOR
			// These read 0 outside the editor
			Counter(page, form, "Textures", ProfilerCategory.Memory, "Texture Count");
			Bytes(page, form, "Texture Memory", "Texture Memory", BYTES_PER_MB, "{0:N1} MB");
			Counter(page, form, "Meshes", ProfilerCategory.Memory, "Mesh Count");
			Bytes(page, form, "Mesh Memory", "Mesh Memory", BYTES_PER_MB, "{0:N1} MB");
			Counter(page, form, "Materials", ProfilerCategory.Memory, "Material Count");
			Counter(page, form, "GameObjects", ProfilerCategory.Memory, "Game Object Count");
#endif
		}

		/// <summary>
		/// Shows a per-frame profiler counter, averaged over AVERAGED_FRAMES.
		/// </summary>
		private static void Counter(Segment page, ContainerNode form, string label, ProfilerCategory category, string counter)
		{
			Show(page, form, label, category, counter, AVERAGED_FRAMES, 1f, "{0:N0}", COUNTER_WIDTH);
		}

		/// <summary>
		/// Shows a memory counter in bytes, divided by unit. With frames above 1, shows the average of that many
		/// frames, for counters that jump around from frame to frame.
		/// </summary>
		private static void Bytes(Segment page, ContainerNode form, string label, string counter, float unit, string format,
			int frames = 1)
		{
			Show(page, form, label, ProfilerCategory.Memory, counter, frames, unit, format, BYTES_WIDTH);
		}

		/// <summary>
		/// Shows a profiler counter divided by unit, read every READ_INTERVAL. Hidden if this player doesn't have
		/// the counter.
		/// </summary>
		private static void Show(Segment page, ContainerNode form, string label, ProfilerCategory category, string counter,
			int frames, float unit, string format, int width)
		{
			var recorder = Record(page, category, counter, frames);
			var value = 0f;
			var nextRead = 0f;

			form.Property(label, () => {
					if (Time.unscaledTime >= nextRead) {
						nextRead = Time.unscaledTime + READ_INTERVAL;
						value = recorder.Valid ? Average(recorder) / unit : 0f;
					}

					return value;
				}, format)
				.WithMonospaced()
				.WithValueWidth(width)
				.VisibleWhen(() => recorder.Valid);
		}

		/// <summary>
		/// The average of the frames the recorder holds, or its last value if it holds one.
		/// </summary>
		private static float Average(ProfilerRecorder recorder)
		{
			var count = recorder.Count;

			if (count <= 1) {
				return recorder.LastValue;
			}

			var sum = 0f;

			for (var i = 0; i < count; i++) {
				sum += recorder.GetSample(i).Value;
			}

			return sum / count;
		}

		private static ProfilerRecorder Record(Segment page, ProfilerCategory category, string counter, int frames = 1)
		{
			var recorder = ProfilerRecorder.StartNew(category, counter, frames);
			page.OnDispose(() => recorder.Dispose());

			return recorder;
		}
	}
}
