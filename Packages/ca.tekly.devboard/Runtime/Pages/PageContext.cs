using System;
using System.Collections.Generic;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Panels;
using UnityEngine;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// Passed to a segment's builder. A builder can run many times: once for every panel showing the page, and
	/// again whenever the segment is rebuilt. So builders should read values through getters, not keep widget
	/// references outside the builder, and undo anything they hook up (events, subscriptions) with OnDispose.
	/// </summary>
	public class PageContext
	{
		/// <summary>
		/// The container to build into.
		/// </summary>
		public ContainerWidget Root { get; }

		/// <summary>
		/// The panel showing this copy of the page.
		/// </summary>
		public DevBoardPanel Panel { get; }

		public PageSegment Segment { get; }
		public string Path => Segment.Path;

		/// <summary>
		/// True when the panel is a passive overlay. Builders can use it to show a more compact version.
		/// </summary>
		public bool IsOverlay => Panel.IsOverlay;

		private List<Action> m_onDispose;
		private bool m_disposed;

		internal PageContext(DevBoardPanel panel, PageSegment segment, ContainerWidget root)
		{
			Panel = panel;
			Segment = segment;
			Root = root;
		}

		/// <summary>
		/// Runs action when this copy of the segment is torn down: the segment is disposed or rebuilt, its page
		/// is removed, or its panel is closed.
		/// </summary>
		public void OnDispose(Action action)
		{
			if (action == null) {
				return;
			}

			// Already torn down, e.g. a callback registered from a delayed call: run it straight away
			if (m_disposed) {
				Run(action);
				return;
			}

			m_onDispose ??= new List<Action>();
			m_onDispose.Add(action);
		}

		/// <summary>
		/// Navigates this panel to another page.
		/// </summary>
		public void Open(string path)
		{
			Panel.Open(path);
		}

		/// <summary>
		/// Tears this segment down and builds it again, in this panel, at the end of the frame.
		/// </summary>
		public void Rebuild()
		{
			Panel.RequestRebuild(Segment);
		}

		internal void Dispose()
		{
			if (m_disposed) {
				return;
			}

			m_disposed = true;

			if (m_onDispose == null) {
				return;
			}

			// Reverse order, so things are undone in the opposite order they were set up
			for (var i = m_onDispose.Count - 1; i >= 0; i--) {
				Run(m_onDispose[i]);
			}

			m_onDispose = null;
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
}
