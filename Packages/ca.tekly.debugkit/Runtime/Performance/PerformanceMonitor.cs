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

		private List<PerformanceStat> m_stats = new List<PerformanceStat>();

		public PerformanceMonitor(VisualElement root, DebugKit debugKit)
		{
			m_fpsMonitor = new FpsMonitor();
			m_container = new Container(root, "dk-performance-monitor");

			Enabled = m_enabled.Value;

			var menu = debugKit.Menu("Perf Monitor");
			menu.Row(row => {
				row.Checkbox("Enabled", () => Enabled, v => Enabled = v);
				row.FlexibleSpace();
				row.Button("All", "button-group-left", () => SetAllEnabled(true));
				row.Button("None", "button-group-right", () => SetAllEnabled(false));
			});
			

			m_stats.Add(new FpsStat(m_fpsMonitor, m_container));
			m_stats.Add(new FpsLowsStat(m_fpsMonitor, m_container));
			
			m_stats.Add(new ProfileRecorderStat("Set Pass", ProfilerCategory.Render, "SetPass Calls Count", m_container));
			m_stats.Add(new ProfileRecorderStat("Batches", ProfilerCategory.Render, "Batches Count", m_container));
			m_stats.Add(new ProfileRecorderStat("Draw Calls", ProfilerCategory.Render, "Draw Calls Count", m_container));
			m_stats.Add(new ProfileRecorderStat("Vertices", ProfilerCategory.Render, "Vertices Count", m_container));
			m_stats.Add(new ProfileRecorderStat("Triangles", ProfilerCategory.Render, "Triangles Count", m_container));

#if UNITY_EDITOR
			m_stats.Add(new ProfileRecorderStat("Texture #", ProfilerCategory.Memory, "Texture Count", m_container));
			m_stats.Add(new ProfileRecorderStat("Texture MB", ProfilerCategory.Memory, "Texture Memory", m_container));
			m_stats.Add(new ProfileRecorderStat("Mesh #", ProfilerCategory.Memory, "Mesh Count", m_container));
			m_stats.Add(new ProfileRecorderStat("Mesh MB", ProfilerCategory.Memory, "Mesh Memory", m_container));
			m_stats.Add(new ProfileRecorderStat("Material #", ProfilerCategory.Memory, "Material Count", m_container));
			m_stats.Add(new ProfileRecorderStat("GameObjects #", ProfilerCategory.Memory, "Game Object Count", m_container));
#endif
			
			foreach (var performanceStat in m_stats) {
				menu.Checkbox(performanceStat.Name, () => performanceStat.Enabled, v => performanceStat.Enabled = v);
			}
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