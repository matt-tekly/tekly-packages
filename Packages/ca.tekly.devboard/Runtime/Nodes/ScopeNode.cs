using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Nodes
{
	/// <summary>
	/// A container that builds its own content with a builder, and can tear it down and build it again. Segments,
	/// list rows and dynamic scopes are scopes; p.Scope makes a plain one.
	///
	/// Undo anything the builder hooks up (events, subscriptions, spawned objects) with OnDispose. It runs when
	/// the scope is rebuilt or removed, e.g. its segment is disposed or its list row goes away.
	/// </summary>
	public class ScopeNode : ContainerNode
	{
		/// <summary>
		/// True once the scope has been removed. A disposed scope is never rebuilt.
		/// </summary>
		public bool IsDisposed { get; private set; }

		private readonly Action<ScopeNode> m_build;
		private List<Action> m_onDispose;
		private bool m_building;
		private bool m_built;

		public ScopeNode(string label, Action<ScopeNode> build) : base(ContainerKind.Column, label)
		{
			m_build = build;
		}

		internal override string DefaultId => "scope";

		protected override bool CanAddChildren => m_building || base.CanAddChildren;

		/// <summary>
		/// Runs action when this content is torn down. Callbacks run in the reverse order they were added.
		/// </summary>
		public void OnDispose(Action action)
		{
			if (action == null) {
				return;
			}

			// Already torn down, e.g. a callback registered from a delayed call: run it straight away
			if (IsDisposed) {
				Run(action);
				return;
			}

			m_onDispose ??= new List<Action>();
			m_onDispose.Add(action);
		}

		/// <summary>
		/// Tears the content down and builds it again at the end of the frame, for every panel showing it.
		/// </summary>
		public void Rebuild()
		{
			if (!IsDisposed) {
				DevBoard.Instance.QueueRebuild(this);
			}
		}

		/// <summary>
		/// Runs the builder. A scope made inside another builder is built when that builder finishes, so its own
		/// settings can still be changed until then. Rebuilds run it again.
		/// </summary>
		internal void Build()
		{
			m_built = true;
			BeginChanges();
			m_building = true;

			try {
				RunBuilder();
			} catch (Exception exception) {
				Debug.LogException(exception);
				Add(new LabelNode($"{exception.GetType().Name}: {exception.Message}", LabelKind.Error));
			} finally {
				m_building = false;
			}

			Freeze();
			EndChanges();
		}

		/// <summary>
		/// Tears the content down and builds it again straight away. Panels see one change.
		/// </summary>
		internal void RebuildNow()
		{
			if (IsDisposed) {
				return;
			}

			BeginChanges();
			RunOnDispose();
			ClearChildren();
			Build();
			EndChanges();
		}

		protected virtual void RunBuilder()
		{
			m_build?.Invoke(this);
		}

		internal override void Freeze()
		{
			// Frozen by the builder around it before it's been built: build it now, which freezes it
			if (!m_built) {
				Build();
				return;
			}

			base.Freeze();
		}

		internal override void DisposeTree()
		{
			if (IsDisposed) {
				return;
			}

			base.DisposeTree();
			RunOnDispose();
			IsDisposed = true;
		}

		private void RunOnDispose()
		{
			if (m_onDispose == null) {
				return;
			}

			var actions = m_onDispose;
			m_onDispose = null;

			for (var i = actions.Count - 1; i >= 0; i--) {
				Run(actions[i]);
			}
		}

		private static void Run(Action action)
		{
			try {
				action();
			} catch (Exception exception) {
				Debug.LogException(exception);
			}
		}
	}

	/// <summary>
	/// A scope that rebuilds whenever a key changes, for UI whose shape depends on data, e.g. a server response
	/// that decides which controls exist. The key is polled while a panel shows the scope.
	/// </summary>
	public sealed class DynamicScope<TKey> : ScopeNode
	{
		private readonly Func<TKey> m_getKey;
		private readonly Action<ScopeNode, TKey> m_build;
		private readonly Func<TKey, TKey, bool> m_isSame;

		private TKey m_key;
		private bool m_hasKey;
		private int m_lastPollFrame = -1;

		internal DynamicScope(Func<TKey> getKey, Action<ScopeNode, TKey> build, Func<TKey, TKey, bool> isSame) : base(null, null)
		{
			m_getKey = getKey ?? throw new ArgumentNullException(nameof(getKey));
			m_build = build ?? throw new ArgumentNullException(nameof(build));
			m_isSame = isSame ?? EqualityComparer<TKey>.Default.Equals;
		}

		internal override string DefaultId => "dynamic";

		protected override void RunBuilder()
		{
			m_hasKey = false;
			m_key = m_getKey();
			m_hasKey = true;

			m_build(this, m_key);
		}

		internal override void Poll()
		{
			// Several panels can show the same scope
			if (m_lastPollFrame == Time.frameCount) {
				return;
			}

			m_lastPollFrame = Time.frameCount;

			// A getter that threw during the build is retried here; a working one only rebuilds on a change
			if (m_hasKey && m_isSame(m_key, m_getKey())) {
				return;
			}

			RebuildNow();
		}
	}

	public static class ScopeNodeFactory
	{
		/// <summary>
		/// A group of nodes that can be rebuilt on its own with Rebuild, e.g. from a refresh button next to it.
		/// </summary>
		public static ScopeNode Scope(this ContainerNode parent, Action<ScopeNode> build)
		{
			return parent.Add(new ScopeNode(null, build));
		}

		/// <summary>
		/// A group of nodes built from a key, and rebuilt whenever the key changes. getKey is polled while a panel
		/// shows the scope; isSame compares keys, null for the default equality.
		/// </summary>
		public static DynamicScope<TKey> Dynamic<TKey>(this ContainerNode parent, Func<TKey> getKey, Action<ScopeNode, TKey> build,
			Func<TKey, TKey, bool> isSame = null)
		{
			return parent.Add(new DynamicScope<TKey>(getKey, build, isSame));
		}
	}
}
