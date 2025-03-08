using System;
using System.Collections.Generic;
using Tekly.DebugKit.Utils;
using Tekly.DebugKit.Widgets;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Performance
{
	public class PerformanceMonitor : IDisposable
	{
		public bool Enabled {
			get => m_enabled.Value;
			set {
				m_enabled.Value = value;
				m_container.Enabled = value;
			}
		}

		private readonly Container m_container;
		private readonly BoolPref m_enabled = new BoolPref("performance.stats.enabled");
		private readonly FpsMonitor m_fpsMonitor;
		private readonly Menu m_menu;

		private readonly List<PerformanceStat> m_stats = new List<PerformanceStat>();

		public PerformanceMonitor(VisualElement root, DebugKit debugKit)
		{
			m_fpsMonitor = new FpsMonitor();
			m_container = new Container(root, "dk-performance-monitor");

			Enabled = m_enabled.Value;

			m_menu = debugKit.Menu("Perf Monitor");
			m_menu.Row(row => {
				row.Checkbox("Enabled", () => Enabled, v => Enabled = v);
				row.FlexibleSpace();
				row.ButtonRow(buttonRow => {
					buttonRow.Button("All", "positive", () => SetAllEnabled(true));
					buttonRow.Button("None", "negative", () => SetAllEnabled(false));	
				});
			});

			AddStat(new FpsStat(m_fpsMonitor, m_container));
			AddStat(new FpsLowsStat(m_fpsMonitor, m_container));

			AddProfileRecorderStat("Set Pass", ProfilerCategory.Render, "SetPass Calls Count");
			AddProfileRecorderStat("Batches", ProfilerCategory.Render, "Batches Count");
			AddProfileRecorderStat("Draw Calls", ProfilerCategory.Render, "Draw Calls Count");
			AddProfileRecorderStat("Vertices", ProfilerCategory.Render, "Vertices Count");
			AddProfileRecorderStat("Triangles", ProfilerCategory.Render, "Triangles Count");

#if UNITY_EDITOR
			// These only seem to work in the editor
			AddProfileRecorderStat("Texture #", ProfilerCategory.Memory, "Texture Count");
			AddProfileRecorderStat("Texture MB", ProfilerCategory.Memory, "Texture Memory");
			AddProfileRecorderStat("Mesh #", ProfilerCategory.Memory, "Mesh Count");
			AddProfileRecorderStat("Mesh MB", ProfilerCategory.Memory, "Mesh Memory");
			AddProfileRecorderStat("Material #", ProfilerCategory.Memory, "Material Count");
			AddProfileRecorderStat("GameObjects #", ProfilerCategory.Memory, "Game Object Count");
#endif
		}

		private void AddProfileRecorderStat(string name, ProfilerCategory category, string profiler)
		{
			AddStat(new ProfileRecorderStat(name, category, profiler, m_container));
		}

		private void AddStat(PerformanceStat stat)
		{
			m_stats.Add(stat);
			m_menu.Checkbox(stat.Name, () => stat.Enabled, v => stat.Enabled = v);
		}

		public void Update()
		{
			m_container.Update();
			m_fpsMonitor.Tick(Time.unscaledDeltaTime);
		}

		public void Dispose()
		{
			foreach (var stat in m_stats) {
				stat.Dispose();
			}
		}

		private void SetAllEnabled(bool enabled)
		{
			foreach (var stat in m_stats) {
				stat.Enabled = enabled;
			}
		}
	}
}