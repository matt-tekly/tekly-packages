using System;
using Tekly.DevBoard.Pages;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// Which page a panel wants, and getting the page host to show it. When the wanted page doesn't exist, an
	/// interactive panel shows the nearest page above it and an overlay hides; either way the panel goes back to
	/// the page once it's registered again, e.g. the next time that part of the game loads.
	/// </summary>
	internal sealed class PanelNavigation
	{
		/// <summary>
		/// The page the panel wants to show. Differs from the page shown while that page doesn't exist.
		/// </summary>
		public string Path => m_pendingPath ?? m_waitingPath ?? m_host.CurrentPage?.Path ?? string.Empty;

		/// <summary>
		/// True while an overlay is hidden waiting for its page.
		/// </summary>
		public bool IsWaitingHidden { get; private set; }

		private readonly PageTree m_tree;
		private readonly PageHost m_host;
		private readonly Func<bool> m_isOverlay;
		private readonly Action m_shownChanged;
		private readonly Action m_wantedPageAppeared;

		// The wanted page, while it doesn't exist
		private string m_waitingPath;

		// A page to show in the next Update. Page tree events only queue work: they also fire while the scene is
		// torn down (registrants unregistering in OnDestroy), and building widgets then leaks them past the scene close.
		private string m_pendingPath;

		/// <param name="shownChanged">Called when the page shown, or whether the panel is waiting hidden, may be about to change.</param>
		/// <param name="wantedPageAppeared">Called when the page the panel was waiting for is registered.</param>
		public PanelNavigation(PageTree tree, PageHost host, Func<bool> isOverlay, Action shownChanged, Action wantedPageAppeared)
		{
			m_tree = tree;
			m_host = host;
			m_isOverlay = isOverlay;
			m_shownChanged = shownChanged;
			m_wantedPageAppeared = wantedPageAppeared;

			m_tree.PageAdded += OnPageAdded;
		}

		/// <summary>
		/// Shows the page at path, or the nearest page above it (or, for an overlay, nothing) until it exists.
		/// </summary>
		public void ShowPath(string path)
		{
			path = PageTree.NormalizePath(path);

			var page = m_tree.FindNearest(path);
			m_waitingPath = page.Path == path ? null : path;

			// An overlay is pinned to its page: rather than show some other page, it hides until the page is back
			IsWaitingHidden = m_isOverlay() && m_waitingPath != null;

			// Before showing, so a panel coming back from hiding builds the page while active
			m_shownChanged();

			if (!IsWaitingHidden) {
				m_host.Show(page);
			}
		}

		/// <summary>
		/// Called every frame. Shows a page queued by a page tree event.
		/// </summary>
		public void Update()
		{
			if (m_pendingPath == null) {
				return;
			}

			var path = m_pendingPath;
			m_pendingPath = null;
			ShowPath(path);
		}

		/// <summary>
		/// The page being shown was removed. Keep wanting it, so the panel returns to it when it's registered again.
		/// </summary>
		public void CurrentPageRemoved(string path)
		{
			m_pendingPath = m_waitingPath ?? path;
		}

		public void Teardown()
		{
			m_tree.PageAdded -= OnPageAdded;
		}

		private void OnPageAdded(PageNode page)
		{
			if (m_waitingPath == page.Path) {
				m_pendingPath = page.Path;
				m_wantedPageAppeared();
			}
		}
	}
}
