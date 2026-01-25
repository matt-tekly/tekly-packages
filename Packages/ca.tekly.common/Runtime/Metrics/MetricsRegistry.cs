using System;
using System.Collections.Generic;
using Tekly.Common.Observables;
using UnityEngine.Assertions;

namespace Tekly.Common.Metrics
{
	[Serializable]
	public sealed class MetricsRegistrySave
	{
		public int Version = 1;
		public List<MetricNodeSave> Entries = new List<MetricNodeSave>();
	}

	[Serializable]
	public sealed class MetricNodeSave
	{
		public string Key;
		public long Count;
		public double Amount;
	}


	public interface IMetricNode
	{
		MetricKey Key { get; }
		long Count { get; }
		double Amount{ get; }
		IDisposable Subscribe(Action<MetricUpdate> snapshot);
	}

	/// <summary>
	/// MetricsRegistry is a hierarchical, aggregated metrics system used for gameplay progression, triggers, tutorials.
	///
	/// Metrics are identified by dot-separated MetricKeys (e.g. "player.actions.jump").
	/// Recording a metric increments its Count and Amount, and automatically aggregates those values into all parent
	/// paths ("player", "player.actions", etc.).
	///
	/// Recording metrics never invokes subscribers immediately. Instead, modified nodes are buffered and emitted only
	/// when EmitModifiedNodes() is called. This two-phase model (record then emit) ensures deterministic behavior,
	/// avoids re-entrancy issues, and makes the system safe to use in Unity frame-based workflows.
	///
	/// Subscribers can listen to any metric path and will receive a single aggregated update per emit cycle, even if
	/// the metric was recorded multiple times. Metrics recorded during emission are deferred until the next emit call.
	///
	/// You MUST call EmitModifiedNodes() if you want subscriptions to work. Recommended to be called at the start of
	/// your frame.
	///
	/// It is invalid usage to record to part of a path, you can only record values to leaves.
	///
	/// See the tests for example API usage.
	/// </summary>
	public sealed class MetricsRegistry
	{
		private readonly MetricNode m_root;

		private List<MetricNode> m_dirtyCurrent;
		private List<MetricNode> m_dirtyNext;

		private bool m_isEmitting;

		public MetricsRegistry(int initialDirtyCapacity = 64)
		{
			m_root = MetricNode.CreateRoot();

			m_dirtyCurrent = new List<MetricNode>(Math.Max(8, initialDirtyCapacity));
			m_dirtyNext = new List<MetricNode>(Math.Max(8, initialDirtyCapacity));
		}

		public MetricsRegistry(MetricsRegistrySave save, int initialDirtyCapacity = 64) : this(initialDirtyCapacity)
		{
			for (var i = 0; i < save.Entries.Count; i++) {
				var entry = save.Entries[i];
				if (entry == null || string.IsNullOrEmpty(entry.Key)) {
					continue;
				}

				var count = entry.Count;
				var amount = entry.Amount;

				if (count == 0) {
					continue;
				}

				var leafKey = new MetricKey(entry.Key);
				var node = m_root;
				var segments = leafKey.Segments;

				for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++) {
					node = node.GetOrCreateChild(segments[segmentIndex]);
					node.Apply(count, amount);
				}
			}
		}

		public void Record(MetricKey key, double amount)
		{
			var node = m_root;
			var segments = key.Segments;

			for (var i = 0; i < segments.Length; i++) {
				node = node.GetOrCreateChild(segments[i]);
				node.ApplyRecord(amount, m_dirtyCurrent);
			}
		}
		
		public IMetricNode GetNode(MetricKey key)
		{
			return GetOrCreateNode(key);
		}

		public MetricSnapshot Get(MetricKey key)
		{
			if (GetNodeOrNull(key, out var node)) {
				return node.GetSnapshot();
			}

			return new MetricSnapshot(key, 0, 0);
		}

		/// <summary>
		/// Remember to call EmitModifiedNodes() at the start of your frame if you want subscriptions to work.
		/// </summary>
		public IDisposable Subscribe(MetricKey key, Action<MetricUpdate> onChanged)
		{
			if (onChanged == null) {
				throw new ArgumentNullException(nameof(onChanged));
			}

			var node = GetOrCreateNode(key);
			return node.Subscribe(onChanged);
		}

		/// <summary>
		/// You must call this to have subscription events fire.
		/// </summary>
		public void EmitModifiedNodes()
		{
			Assert.IsFalse(m_isEmitting, "EmitModifiedNodes() called re-entrantly");

			try {
				m_isEmitting = true;
				(m_dirtyCurrent, m_dirtyNext) = (m_dirtyNext, m_dirtyCurrent);

				m_dirtyCurrent.Clear();

				for (var i = 0; i < m_dirtyNext.Count; i++) {
					m_dirtyNext[i].EmitIfDirty();
				}

				m_dirtyNext.Clear();
			} finally {
				m_isEmitting = false;
			}
		}

		/// <summary>
		/// Persists only the leaf nodes with non-zero values
		/// </summary>
		public MetricsRegistrySave ToSave()
		{
			var snapshot = new MetricsRegistrySave();

			if (!m_root.HasChildren) {
				return snapshot;
			}
			
			var stack = new Stack<MetricNode>();
			stack.Push(m_root);

			while (stack.Count > 0) {
				var node = stack.Pop();
				
				if (node.HasChildren) {
					foreach (var kvp in node.Children) {
						stack.Push(kvp.Value);
					}

					continue;
				}

				if (node.Count == 0) {
					continue;
				}

				snapshot.Entries.Add(
					new MetricNodeSave {
						Key = node.Key.Path,
						Count = node.Count,
						Amount = node.Amount
					}
				);
			}

			return snapshot;
		}

		private MetricNode GetOrCreateNode(MetricKey key)
		{
			var node = m_root;
			var segments = key.Segments;

			for (var i = 0; i < segments.Length; i++) {
				node = node.GetOrCreateChild(segments[i]);
			}

			return node;
		}

		private bool GetNodeOrNull(MetricKey key, out MetricNode metricNode)
		{
			metricNode = m_root;
			var segments = key.Segments;

			for (var i = 0; i < segments.Length; i++) {
				if (!metricNode.TryGetChild(segments[i], out var next)) {
					return false;
				}

				metricNode = next;
			}

			return true;
		}
		
		private sealed class MetricNode : IMetricNode
		{
			public MetricKey Key => m_key;

			public long Count { get; private set; }
			public double Amount { get; private set; }

			public Dictionary<string, MetricNode> Children;
			public bool HasChildren => Children != null && Children.Count > 0;

			private readonly string m_name;
			private readonly string m_fullPath;
			private readonly MetricKey m_key;

			private long m_startCount;
			private double m_startAmount;
			private bool m_dirty;

			private Triggerable<MetricUpdate> m_changed;

			private MetricNode(string name, string fullPath, MetricKey key)
			{
				m_name = name;
				m_fullPath = fullPath;
				m_key = key;

				Count = 0;
				Amount = 0;

				m_startCount = 0;
				m_startAmount = 0.0;

				m_changed = null;
			}

			public static MetricNode CreateRoot()
			{
				return new MetricNode(null, null, default);
			}

			public bool TryGetChild(string segment, out MetricNode child)
			{
				if (Children == null) {
					child = null;
					return false;
				}

				return Children.TryGetValue(segment, out child!);
			}

			public MetricNode GetOrCreateChild(string segment)
			{
				if (Children == null) {
					Children = new Dictionary<string, MetricNode>(StringComparer.Ordinal);
				} else if (Children.TryGetValue(segment, out var existing)) {
					return existing;
				}

				var fullPath = string.IsNullOrEmpty(m_fullPath) ? segment : $"{m_fullPath}.{segment}";

				var key = new MetricKey(fullPath);
				var child = new MetricNode(segment, fullPath, key);
				Children.Add(segment, child);
				return child;
			}

			public MetricSnapshot GetSnapshot()
			{
				return new MetricSnapshot(m_key, Count, Amount);
			}
			
			public IDisposable Subscribe(Action<MetricUpdate> handler)
			{
				if (m_changed == null) {
					m_changed = new Triggerable<MetricUpdate>();
				}

				return m_changed.Subscribe(handler);
			}

			public void ApplyRecord(double amountDelta, List<MetricNode> dirtyList)
			{
				if (!m_dirty) {
					m_startCount = Count;
					m_startAmount = Amount;
					m_dirty = true;
					dirtyList.Add(this);
				}

				// Apply the delta.
				Count += 1;
				Amount += amountDelta;
			}

			public void EmitIfDirty()
			{
				if (!m_dirty) {
					return;
				}

				m_dirty = false;

				if (m_changed == null) {
					return;
				}

				var countDelta = Count - m_startCount;
				var amountDelta = Amount - m_startAmount;

				m_changed.Emit(new MetricUpdate(m_key, Count, Amount, countDelta, amountDelta));
			}

			public void Apply(long count, double amount)
			{
				Count += count;
				Amount += amount;
			}
			
			public override string ToString()
			{
				if (m_fullPath == null) {
					return $"<Root> count={Count} amount={Amount}";
				}

				return $"{m_fullPath} count={Count} amount={Amount}" +
				       (m_dirty ? " [DIRTY]" : "") +
				       (HasChildren ? $" children={Children.Count}" : "");
			}
		}
	}
}