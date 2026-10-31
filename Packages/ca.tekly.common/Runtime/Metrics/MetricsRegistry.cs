using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tekly.Common.Observables;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

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
		IDisposable Subscribe(Action<MetricUpdate> onChanged);
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
	/// It is invalid usage to record to part of a path, you can only record values to leaves. This is asserted in
	/// development builds.
	///
	/// Recording to a metric that already exists does not allocate. The first record to a new path allocates its node
	/// and any missing parent nodes.
	///
	/// See the tests for example API usage.
	/// </summary>
	public sealed class MetricsRegistry
	{
		private readonly Dictionary<string, MetricNode> m_nodes = new Dictionary<string, MetricNode>(StringComparer.Ordinal);

		private List<MetricNode> m_dirtyCurrent;
		private List<MetricNode> m_dirtyNext;

		private bool m_isEmitting;

		public MetricsRegistry(int initialDirtyCapacity = 64)
		{
			m_dirtyCurrent = new List<MetricNode>(Math.Max(8, initialDirtyCapacity));
			m_dirtyNext = new List<MetricNode>(Math.Max(8, initialDirtyCapacity));
		}

		/// <summary>
		/// Restores a registry from a save. A save with a malformed key throws an ArgumentException, as a corrupt save
		/// can't be trusted and restoring only part of it would hide the problem. Recovery is up to the save system.
		/// </summary>
		public MetricsRegistry(MetricsRegistrySave save, int initialDirtyCapacity = 64) : this(initialDirtyCapacity)
		{
			for (var i = 0; i < save.Entries.Count; i++) {
				var entry = save.Entries[i];

				var count = entry.Count;
				var amount = entry.Amount;

				if (count == 0) {
					continue;
				}

				var leaf = GetOrCreateNode(new MetricKey(entry.Key));

				for (var node = leaf; node != null; node = node.Parent) {
					node.Apply(count, amount);
				}

				leaf.MarkAsLeaf();
			}
		}

		public void Record(MetricKey key, double amount)
		{
			AssertFinite(key, amount);

			var leaf = GetOrCreateNode(key);
			AssertNoChildren(leaf);

			RecordToNodeAndParents(leaf, amount);
			leaf.MarkAsLeaf();
		}

		public IMetricNode GetNode(MetricKey key)
		{
			return GetOrCreateNode(key);
		}

		public MetricSnapshot Get(MetricKey key)
		{
			if (TryGetNode(key, out var node)) {
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
		/// Exceptions thrown by subscribers are logged and do not prevent other nodes from emitting.
		/// </summary>
		public void EmitModifiedNodes()
		{
			Assert.IsFalse(m_isEmitting, "EmitModifiedNodes() called re-entrantly");

			if (m_isEmitting) {
				return;
			}

			try {
				m_isEmitting = true;
				(m_dirtyCurrent, m_dirtyNext) = (m_dirtyNext, m_dirtyCurrent);

				m_dirtyCurrent.Clear();

				for (var i = 0; i < m_dirtyNext.Count; i++) {
					try {
						m_dirtyNext[i].EmitIfDirty();
					} catch (Exception e) {
						Debug.LogException(e);
					}
				}
			} finally {
				m_dirtyNext.Clear();
				m_isEmitting = false;
			}
		}

		/// <summary>
		/// Persists only the leaf nodes with non-zero values
		/// </summary>
		public MetricsRegistrySave ToSave()
		{
			var snapshot = new MetricsRegistrySave();

			foreach (var node in m_nodes.Values) {
				if (node.HasChildren || node.Count == 0) {
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

		/// <summary>
		/// Applies root first so parents are dirtied, and therefore emitted, before their children.
		/// </summary>
		private void RecordToNodeAndParents(MetricNode node, double amount)
		{
			if (node.Parent != null) {
				RecordToNodeAndParents(node.Parent, amount);
			}

			node.ApplyRecord(amount, m_dirtyCurrent);
		}

		private MetricNode GetOrCreateNode(MetricKey key)
		{
			ThrowIfEmpty(key);

			if (m_nodes.TryGetValue(key.Path, out var node)) {
				return node;
			}

			MetricNode parent = null;

			if (key.TryGetParent(out var parentKey)) {
				parent = GetOrCreateNode(parentKey);
				AssertNotLeaf(parent, key);
			}

			node = new MetricNode(key, parent);
			m_nodes.Add(key.Path, node);

			return node;
		}

		private bool TryGetNode(MetricKey key, out MetricNode node)
		{
			ThrowIfEmpty(key);
			return m_nodes.TryGetValue(key.Path, out node);
		}

		private static void ThrowIfEmpty(MetricKey key)
		{
			if (key.IsEmpty) {
				throw new ArgumentException("MetricKey is empty. Use a non-default MetricKey.", nameof(key));
			}
		}

		[Conditional("UNITY_ASSERTIONS")]
		private static void AssertFinite(MetricKey key, double amount)
		{
			if (!double.IsFinite(amount)) {
				Assert.IsTrue(false, $"Recording a non-finite amount to [{key}] would poison every parent metric.");
			}
		}

		[Conditional("UNITY_ASSERTIONS")]
		private static void AssertNoChildren(MetricNode node)
		{
			if (node.HasChildren) {
				Assert.IsTrue(false, $"Recording to [{node.Key}] which has child metrics. Only leaves can be recorded to.");
			}
		}

		[Conditional("UNITY_ASSERTIONS")]
		private static void AssertNotLeaf(MetricNode parent, MetricKey childKey)
		{
			if (parent.IsLeaf) {
				Assert.IsTrue(false, $"Creating child metric [{childKey}] under [{parent.Key}] which has been recorded to. Only leaves can be recorded to.");
			}
		}

		private sealed class MetricNode : IMetricNode
		{
			public MetricKey Key => m_key;
			public long Count { get; private set; }
			public double Amount { get; private set; }

			public MetricNode Parent { get; }
			public bool HasChildren => m_childCount > 0;

			/// <summary>
			/// True once a value has been recorded directly to this node. Such a node must never gain children.
			/// </summary>
			public bool IsLeaf { get; private set; }

			private readonly MetricKey m_key;

			private int m_childCount;

			private long m_startCount;
			private double m_startAmount;
			private bool m_dirty;

			private Triggerable<MetricUpdate> m_changed;

			public MetricNode(MetricKey key, MetricNode parent)
			{
				m_key = key;
				Parent = parent;

				if (parent != null) {
					parent.m_childCount++;
				}
			}

			public void MarkAsLeaf()
			{
				IsLeaf = true;
			}

			public MetricSnapshot GetSnapshot()
			{
				return new MetricSnapshot(m_key, Count, Amount);
			}

			public IDisposable Subscribe(Action<MetricUpdate> onChanged)
			{
				if (m_changed == null) {
					m_changed = new Triggerable<MetricUpdate>();
				}

				return m_changed.Subscribe(onChanged);
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
				return $"{m_key.Path} count={Count} amount={Amount}" +
				       (m_dirty ? " [DIRTY]" : "") +
				       (HasChildren ? $" children={m_childCount}" : "");
			}
		}
	}
}
