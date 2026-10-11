// DevBoard is in the editor always, and in player builds that are development builds or define DEVBOARD_ENABLED,
// unless they define DEVBOARD_DISABLED. The UI assembly's define constraints say the same.
#if UNITY_EDITOR || ((DEVELOPMENT_BUILD || DEVBOARD_ENABLED) && !DEVBOARD_DISABLED)
#define DEVBOARD_ACTIVE
#endif

using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DevBoard.Access;
using Tekly.DevBoard.Commands;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;
using Debug = UnityEngine.Debug;
using GameObject = UnityEngine.GameObject;
using Component = UnityEngine.Component;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Where game code registers debug pages. The pages are shown by DevBoard's UI, which is only in builds that
	/// include DevBoard (see Enabled); this part is in every build so game code compiles everywhere.
	///
	/// Register pages with the static Page methods. In builds without DevBoard every call to them is removed by
	/// the compiler, arguments and all, so nothing runs and the builders they'd have called can be stripped:
	/// <code>
	/// DevBoard.Page("Ads", BuildAdsPage, owner: this);   // removed again with RemovePages(this)
	/// DevBoard.Page(ref m_page, "Ads", BuildAdsPage);    // removed again with m_page.Dispose()
	/// </code>
	/// </summary>
	public class DevBoard : Singleton<DevBoard>
	{
		/// <summary>
		/// True in builds that include DevBoard: the editor, development builds, and builds defining
		/// DEVBOARD_ENABLED, but not builds defining DEVBOARD_DISABLED. For skipping debug-only work at runtime;
		/// use your own define with #if for code that must be left out of release builds.
		/// </summary>
#if DEVBOARD_ACTIVE
		public static readonly bool Enabled = true;
#else
		public static readonly bool Enabled = false;
#endif

		/// <summary>
		/// Every registered page.
		/// </summary>
		public PageTree Pages { get; } = new();

		/// <summary>
		/// View state (foldouts, scroll positions, searches) that survives widgets being destroyed and rebuilt.
		/// </summary>
		public DevBoardState State { get; } = new();

		/// <summary>
		/// Finds nodes on any page by path, and reads, sets and runs them as text. Register here to make a custom
		/// node work with the command palette and other tools.
		/// </summary>
		public NodeAccess Access { get; }

		/// <summary>
		/// Commands typed into the console: [DevCommand] static methods and those added with Command.
		/// </summary>
		public CommandRegistry Commands { get; } = new();

		// Segments and commands registered with an owner, to remove with RemovePages
		private readonly Dictionary<object, List<IDisposable>> m_owned = new();

		// Scopes waiting for the end of the frame to rebuild
		private readonly List<ScopeNode> m_rebuilds = new();

		public DevBoard()
		{
			Access = new NodeAccess(Pages);
		}

		/// <summary>
		/// Adds a segment to the page at path, e.g. "Game/Economy". The page and any missing parents are created,
		/// and segments sharing a path appear one after another on the same page, sorted by order.
		///
		/// build makes the segment's nodes. It runs once, the first time something shows the segment, and every
		/// panel shows the same nodes; it runs again only when the segment is rebuilt. Values that change go
		/// through getters and setters, and anything it hooks up can be undone with Segment.OnDispose.
		///
		/// owner, when given, removes the segment with RemovePages(owner). A GameObject owner, or a component's,
		/// also removes it when destroyed.
		///
		/// Removed by the compiler in builds without DevBoard.
		/// </summary>
#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Page(string path, Action<Segment> build, string title = null, int order = 0, object owner = null)
		{
			if (!Enabled) {
				return;
			}

			var segment = Instance.AddPage(path, build, title, order);

			if (owner != null) {
				Instance.Own(owner, segment);
			}
		}

		/// <summary>
		/// Adds a segment like Page, and puts it in handle so handle.Dispose() removes it. Anything already in
		/// handle is removed first. In builds without DevBoard the call is removed by the compiler and handle stays
		/// empty, so disposing it does nothing.
		/// </summary>
#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Page(ref PageHandle handle, string path, Action<Segment> build, string title = null, int order = 0)
		{
			handle.Dispose();

			if (Enabled) {
				handle = new PageHandle(Instance.AddPage(path, build, title, order));
			}
		}

		/// <summary>
		/// Adds a command typed into the console: name, then arguments for the action's parameters, e.g.
		/// <code>
		/// DevBoard.Command("give gold", (int amount) => m_wallet.Add(amount), "Adds gold", owner: this);
		/// </code>
		/// The return value is shown as the result. A method group keeps its optional parameters' defaults; a
		/// lambda can't have any. See DevCommandAttribute for static methods, and for how arguments are typed.
		///
		/// suggest, when given, gives the values the console suggests as each argument is typed: it gets a
		/// parameter's name and returns its values, or null for none (enums and bools are suggested anyway):
		/// <code>
		/// DevBoard.Command("inventory remove", (string item) => Remove(item), owner: this, suggest: _ => m_inventory);
		/// </code>
		///
		/// owner, when given, removes the command with RemovePages(owner), or when a GameObject owner is destroyed.
		/// Removed by the compiler in builds without DevBoard.
		/// </summary>
#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command(string name, Action action, string description = null, object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<T1>(string name, Action<T1> action, string description = null, object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<T1, T2>(string name, Action<T1, T2> action, string description = null, object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<T1, T2, T3>(string name, Action<T1, T2, T3> action, string description = null, object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<TResult>(string name, Func<TResult> action, string description = null, object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<T1, TResult>(string name, Func<T1, TResult> action, string description = null, object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<T1, T2, TResult>(string name, Func<T1, T2, TResult> action, string description = null,
			object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Command<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> action, string description = null,
			object owner = null,
			Func<string, IEnumerable<string>> suggest = null) =>
			AddCommand(name, action, description, owner, suggest);

		private static void AddCommand(string name, Delegate action, string description, object owner,
			Func<string, IEnumerable<string>> suggest)
		{
			if (!Enabled) {
				return;
			}

			var command = Instance.Commands.Add(name, action, description);

			if (suggest != null) {
				foreach (var parameter in command.Parameters) {
					var parameterName = parameter.Name;
					command.WithSuggestions(parameterName, () => suggest(parameterName));
				}
			}

			if (owner != null) {
				Instance.Own(owner, command);
			}
		}

		/// <summary>
		/// Removes every segment and command registered with owner. Removed by the compiler in builds without
		/// DevBoard.
		/// </summary>
#if !DEVBOARD_ACTIVE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void RemovePages(object owner)
		{
			if (Enabled && owner != null && Instance != null) {
				Instance.ReleaseOwner(owner);
			}
		}

		/// <summary>
		/// Adds a segment and returns it, for code that keeps it to remove later. Unlike Page, calls to this stay
		/// in builds without DevBoard, where it returns a segment that does nothing and never builds.
		/// </summary>
		public PageSegment AddPage(string path, Action<Segment> build, string title = null, int order = 0)
		{
			return Enabled ? Pages.Add(path, build, title, order) : PageSegment.Inert;
		}

		/// <summary>
		/// Rebuilds scope at the end of the frame. Rebuilding straight away could destroy the widget whose click
		/// asked for it.
		/// </summary>
		internal void QueueRebuild(ScopeNode scope)
		{
			if (!m_rebuilds.Contains(scope)) {
				m_rebuilds.Add(scope);
			}
		}

		/// <summary>
		/// Runs the queued rebuilds. Called at the end of every frame by whatever is showing pages: DevBoard's
		/// panels, or a remote viewer. With nothing showing pages, nothing needs rebuilding.
		/// </summary>
		internal void RunRebuilds()
		{
			if (m_rebuilds.Count == 0) {
				return;
			}

			var scopes = m_rebuilds.ToArray();
			m_rebuilds.Clear();

			foreach (var scope in scopes) {
				try {
					scope.RebuildNow();
				} catch (Exception exception) {
					Debug.LogException(exception);
				}
			}
		}

		private void Own(object owner, IDisposable registration)
		{
			if (!m_owned.TryGetValue(owner, out var registrations)) {
				registrations = new List<IDisposable>();
				m_owned.Add(owner, registrations);

				// Destroying a GameObject owner removes its segments, so MonoBehaviours don't need RemovePages
				var gameObject = owner as GameObject ?? (owner as Component)?.gameObject;

				if (gameObject != null) {
					PageSegmentBinding.Watch(gameObject, owner);
				}
			}

			registrations.Add(registration);
		}

		internal void ReleaseOwner(object owner)
		{
			if (!m_owned.TryGetValue(owner, out var registrations)) {
				return;
			}

			m_owned.Remove(owner);

			foreach (var registration in registrations) {
				registration.Dispose();
			}
		}
	}

	/// <summary>
	/// Holds a segment added with DevBoard.Page(ref handle, ...), to remove it with Dispose. Empty by default, and
	/// in builds without DevBoard, where disposing it does nothing. Keep it in a field; copies share the segment.
	/// </summary>
	public struct PageHandle : IDisposable
	{
		private PageSegment m_segment;

		/// <summary>
		/// True while it holds a segment that hasn't been removed.
		/// </summary>
		public bool IsValid => m_segment != null && !m_segment.IsDisposed;

		internal PageHandle(PageSegment segment)
		{
			m_segment = segment;
		}

		public void Dispose()
		{
			m_segment?.Dispose();
			m_segment = null;
		}
	}
}
