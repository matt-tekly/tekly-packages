using System;

namespace Tekly.Common.Metrics
{
	/// <summary>
	/// A dot-separated path identifying a metric (e.g. "player.actions.jump").
	///
	/// Paths must be well formed: no empty segments and no whitespace at the start or end of a segment. Malformed
	/// paths throw. Constructing a key only validates the string, it does not allocate.
	/// </summary>
	public readonly struct MetricKey : IEquatable<MetricKey>
	{
		public string Path => m_path;
		public bool IsEmpty => m_path == null;

		private readonly string m_path;

		public MetricKey(string path)
		{
			Validate(path);
			m_path = path;
		}

		/// <summary>
		/// Gets the key one level up ("a.b.c" gives "a.b"). Returns false for single segment keys.
		/// Allocates the parent path string.
		/// </summary>
		public bool TryGetParent(out MetricKey parent)
		{
			var index = m_path == null ? -1 : m_path.LastIndexOf('.');

			if (index < 0) {
				parent = default;
				return false;
			}

			parent = new MetricKey(m_path.Substring(0, index));
			return true;
		}

		/// <summary>
		/// Creates a child key ("a.b" + "c" gives "a.b.c"). Allocates the new path string.
		/// </summary>
		public MetricKey Append(string segment)
		{
			return new MetricKey(m_path == null ? segment : m_path + "." + segment);
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
			return m_path == null ? 0 : StringComparer.Ordinal.GetHashCode(m_path);
		}

		public static implicit operator MetricKey(string value)
		{
			return new MetricKey(value);
		}

		private static void Validate(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) {
				throw new ArgumentException("MetricKey must be non-empty.", nameof(path));
			}

			var start = 0;

			for (var i = 0; i <= path.Length; i++) {
				if (i < path.Length && path[i] != '.') {
					continue;
				}

				var end = i - 1;

				if (end < start) {
					throw new ArgumentException($"MetricKey [{path}] contains an empty segment.", nameof(path));
				}

				if (char.IsWhiteSpace(path[start]) || char.IsWhiteSpace(path[end])) {
					var segment = path.Substring(start, end - start + 1);
					throw new ArgumentException($"MetricKey [{path}] has whitespace at the edge of segment [{segment}].", nameof(path));
				}

				start = i + 1;
			}
		}
	}
}
