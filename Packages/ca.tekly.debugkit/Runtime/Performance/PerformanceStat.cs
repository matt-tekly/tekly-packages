using System;
using Tekly.DebugKit.Utils;
using Tekly.DebugKit.Widgets;

namespace Tekly.DebugKit.Performance
{
	public class PerformanceStat : IDisposable
	{
		public string Name { get; }
		public string Format { get; }

		public bool Enabled {
			get => m_enabled.Value;
			set {
				m_enabled.Value = value;
				m_container.Enabled = value;
			}
		}

		protected virtual double Value { get; } 
		
		private readonly Container m_container;
		private readonly BoolPref m_enabled;
		
		public PerformanceStat(string name, string format, Container container)
		{
			Name = name;
			Format = format;

			m_container = container.Column();
			m_container.Property(name, GetValue, format);
			
			m_enabled = new BoolPref($"performance.stats.{name}.enabled", true);
		}

		private object GetValue()
		{
			return Value;
		}

		public virtual void Dispose()
		{
			
		}
	}
}