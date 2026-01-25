using System;

namespace Tekly.Common.Metrics
{
	public readonly struct MetricKey : IEquatable<MetricKey>
	{
		public string Path => m_path;
		public ReadOnlySpan<string> Segments => m_segments;
		
		private readonly string m_path;
		private readonly string[] m_segments;

		public MetricKey(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) {
				throw new ArgumentException("MetricKey must be non-empty.", nameof(path));
			}

			m_path = path;
			m_segments = m_path.Split('.', StringSplitOptions.RemoveEmptyEntries);

			if (m_segments.Length == 0) {
				throw new ArgumentException("MetricKey must contain at least one segment.", nameof(path));
			}
		}

		private MetricKey(string path, string[] segments) {
			m_path = path;
			m_segments = segments;
		}

		public override string ToString()
		{
			return m_path;
		}

		public bool Equals(MetricKey other)
		{
			return string.Equals(m_path, other.m_path, StringComparison.Ordinal);
		}

		public override bool Equals(object obj)
		{
			return obj is MetricKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			return StringComparer.Ordinal.GetHashCode(m_path);
		}
		
		public MetricKey Append(string segment) {
			if (m_segments == null) {
				return new MetricKey(segment, new[] { segment });
			}
			
			var newSegments = new string[m_segments.Length + 1];
			Array.Copy(m_segments, newSegments, m_segments.Length);
			newSegments[m_segments.Length] = segment;
			
			return new MetricKey($"{m_path}.{segment}", newSegments);
		}

		public static implicit operator MetricKey(string value)
		{
			return new MetricKey(value);
		}
	}
}