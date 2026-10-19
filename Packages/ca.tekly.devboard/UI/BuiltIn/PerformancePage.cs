using System;
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
		private const int FRAME_WIDTH = 7;     // "16.7 ms"
		private const int COUNTER_WIDTH = 6;   // "999.9K", see Compact
		private const int BYTES_WIDTH = 10;    // "1,234.5 MB"

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
			var row = page.Row();
			Frames(page, Column(row, "Frames"));
			Rendering(page, Column(row, "Rendering"));
		}

		/// <summary>
		/// A scope of its own, so each column's labels line up with each other rather than across both.
		/// </summary>
		private static ContainerNode Column(ContainerNode row, string heading)
		{
			var column = row.Column().WithFlexibleWidth().WithWidthGroupScope();
			column.Heading(heading);

			return column;
		}

		private static void Frames(Segment page, ContainerNode column)
		{
			var frames = new FrameStats();

			LifeCycle.Instance.Update += frames.Tick;
			page.OnDispose(() => LifeCycle.Instance.Update -= frames.Tick);

			column.Property("FPS", () => frames.Fps, "{0:F0}").WithTolerance(0.5f).WithMonospaced().WithValueWidth(FRAME_WIDTH);
			column.Property("Frame", () => frames.FrameMs, "{0:F1} ms").WithTolerance(0.05f).WithMonospaced().WithValueWidth(FRAME_WIDTH);
			column.Property("1% Low", () => frames.OnePercentLow, "{0:F0}").WithTolerance(0.5f).WithMonospaced().WithValueWidth(FRAME_WIDTH);
			column.Button("Reset", frames.Reset).InteractiveOnly();
		}

		private static void Rendering(Segment page, ContainerNode column)
		{
			Counter(page, column, "SetPass Calls", ProfilerCategory.Render, "SetPass Calls Count");
			Counter(page, column, "Batches", ProfilerCategory.Render, "Batches Count");
			Counter(page, column, "Draw Calls", ProfilerCategory.Render, "Draw Calls Count");
			Counter(page, column, "Vertices", ProfilerCategory.Render, "Vertices Count");
			Counter(page, column, "Triangles", ProfilerCategory.Render, "Triangles Count");
		}

		private static void Memory(Segment page)
		{
			var foldout = page.Foldout("Memory").WithStyle(Styles.SubtleFoldout);
			Bytes(page, foldout, "Total Used", "Total Used Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, foldout, "Total Reserved", "Total Reserved Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, foldout, "GC Used", "GC Used Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, foldout, "GC Reserved", "GC Reserved Memory", BYTES_PER_MB, "{0:N1} MB");
			Bytes(page, foldout, "GC Alloc / Frame", "GC Allocated In Frame", BYTES_PER_KB, "{0:N1} KB", AVERAGED_FRAMES);

#if UNITY_EDITOR
			// These read 0 outside the editor
			Counter(page, foldout, "Textures", ProfilerCategory.Memory, "Texture Count");
			Bytes(page, foldout, "Texture Memory", "Texture Memory", BYTES_PER_MB, "{0:N1} MB");
			Counter(page, foldout, "Meshes", ProfilerCategory.Memory, "Mesh Count");
			Bytes(page, foldout, "Mesh Memory", "Mesh Memory", BYTES_PER_MB, "{0:N1} MB");
			Counter(page, foldout, "Materials", ProfilerCategory.Memory, "Material Count");
			Counter(page, foldout, "GameObjects", ProfilerCategory.Memory, "Game Object Count");
#endif
		}

		/// <summary>
		/// Shows a per-frame profiler counter, averaged over AVERAGED_FRAMES.
		/// </summary>
		private static void Counter(Segment page, ContainerNode parent, string label, ProfilerCategory category, string counter)
		{
			Show(page, parent, label, category, counter, AVERAGED_FRAMES, Compact, COUNTER_WIDTH);
		}

		/// <summary>
		/// Shows a memory counter in bytes, divided by unit. With frames above 1, shows the average of that many
		/// frames, for counters that jump around from frame to frame.
		/// </summary>
		private static void Bytes(Segment page, ContainerNode parent, string label, string counter, float unit, string format,
			int frames = 1)
		{
			Show(page, parent, label, ProfilerCategory.Memory, counter, frames, value => string.Format(format, value / unit), BYTES_WIDTH);
		}

		/// <summary>
		/// Shows a profiler counter as text, read every READ_INTERVAL. Hidden if this player doesn't have the
		/// counter.
		/// </summary>
		private static void Show(Segment page, ContainerNode parent, string label, ProfilerCategory category, string counter,
			int frames, Func<float, string> toText, int width)
		{
			var recorder = Record(page, category, counter, frames);
			var text = string.Empty;
			var nextRead = 0f;
			
			parent.Property(label, () => {
					if (Time.unscaledTime >= nextRead) {
						nextRead = Time.unscaledTime + READ_INTERVAL;
						text = toText(recorder.Valid ? Average(recorder) : 0f);
					}

					return text;
				})
				.WithMonospaced()
				.WithValueWidth(width)
				.VisibleWhen(() => recorder.Valid);
		}

		/// <summary>
		/// A count in at most six characters: "9,999", "48.2K", "1.3M".
		/// </summary>
		private static string Compact(float count)
		{
			if (count < 10_000f) {
				return count.ToString("N0");
			}

			if (count < 1_000_000f) {
				return (count / 1_000f).ToString("0.#") + "K";
			}

			if (count < 1_000_000_000f) {
				return (count / 1_000_000f).ToString("0.#") + "M";
			}

			return (count / 1_000_000_000f).ToString("0.#") + "B";
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
