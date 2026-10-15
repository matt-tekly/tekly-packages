using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.DevBoard;
using Tekly.DevBoard.Nodes;
using Tekly.Trellis;
using UnityEngine;
using UnityEngine.EventSystems;

// Tekly.DevBoard is also a namespace, which wins over the class when written from inside Tekly.Leaf
using DevBoardApi = Tekly.DevBoard.DevBoard;

namespace Tekly.Leaf.Integration
{
	/// <summary>
	/// Adds Leaf's pages to DevBoard. This assembly only compiles when the DevBoard package is in the project, so
	/// Leaf itself doesn't depend on it. A Singleton, so what it remembers starts fresh every play session.
	/// </summary>
	internal class LeafDevBoardPages : Singleton<LeafDevBoardPages>
	{
		private const string PATH = "System/Leaf";

		// Values that change all the time get a fixed width, so the panel doesn't resize with them
		private const int VALUE_WIDTH = 30;

		// The last thing selected outside DevBoard. Clicking DevBoard selects its own controls, which isn't what
		// this page is for
		private GameObject m_lastSelected;

		// The canvases Hide All turned off, so Show All turns back on only those, not ones the game had off itself
		private readonly List<Canvas> m_hiddenCanvases = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void Register()
		{
			DevBoardApi.Page(PATH, Instance.Build, owner: typeof(LeafDevBoardPages));
		}

		private void Build(Segment page)
		{
			// One form for the whole page, so every section's values line up in the same column
			var form = page.Form();
			form.Heading("Input");
			form.Property("Disabled", () => LeafCore.Instance.IsInputDisabled ? "Yes" : "No");

			form.Heading("Selection");
			// The last selection outside DevBoard, since clicking DevBoard selects its own controls
			form.Property("Selected", () => Name(Selected())).WithValueWidth(VALUE_WIDTH);

			// The end of the path is the part that tells objects apart, so it's trimmed from the front
			var pathRow = form.Row().WithPadding(0).WithCrossAlignment(CrossAlignment.Center);
			pathRow.Property("Path", () => TrimStart(Path(Selected()), VALUE_WIDTH))
				.WithValueWidth(VALUE_WIDTH);
#if UNITY_EDITOR
			pathRow.IconButton("Select in Hierarchy", Icons.Search,
					() => SelectInHierarchy(Selected()))
				.InteractableWhen(() => Selected() != null);
#else
			pathRow.IconButton("Copy Path", Icons.Copy,
				() => GUIUtility.systemCopyBuffer = Path(Selected()));
#endif

			form.Property("EventSystem", () => EventSystem.current != null ? EventSystem.current.name : "None");

			form.Heading("Cursor");
			form.Property("Wanted", () => LeafCore.Instance.Cursor.Current);
			form.Property("Asked By", () => OwnerName(LeafCore.Instance.Cursor.Owner)).WithValueWidth(VALUE_WIDTH);
			form.Property("Requests", () => LeafCore.Instance.Cursor.RequestCount);

			// Hide All hides every enabled canvas but DevBoard's, to see what's behind the UI
			form.Heading("Canvases");
			form.Property("Hidden", () => m_hiddenCanvases.Count);

			var canvasRow = form.ButtonRow();
			canvasRow.Button("Hide All", HideCanvases).InteractiveOnly();
			canvasRow.Button("Show All", ShowCanvases)
				.InteractableWhen(() => m_hiddenCanvases.Count > 0)
				.InteractiveOnly();
		}

		private void HideCanvases()
		{
			var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			foreach (var canvas in canvases) {
				if (!canvas.enabled || DevBoardApi.Instance.IsBoardObject(canvas.gameObject)) {
					continue;
				}

				canvas.enabled = false;
				m_hiddenCanvases.Add(canvas);
			}
		}

		private void ShowCanvases()
		{
			foreach (var canvas in m_hiddenCanvases) {
				// Destroyed since, e.g. with its scene
				if (canvas != null) {
					canvas.enabled = true;
				}
			}

			m_hiddenCanvases.Clear();
		}

		/// <summary>
		/// The current selection, or while DevBoard's own controls are selected, the last one before them.
		/// </summary>
		private GameObject Selected()
		{
			var current = LeafCore.Instance.Selection.Current.Value;

			if (current != null && !DevBoardApi.Instance.IsBoardObject(current)) {
				m_lastSelected = current;
			}

			return m_lastSelected != null ? m_lastSelected : null;
		}

		/// <summary>
		/// A component's GameObject and type, or just the type for anything else asking.
		/// </summary>
		private static string OwnerName(object owner)
		{
			switch (owner) {
				case null:
					return "None";
				case Component component when component != null:
					return $"{component.name} ({component.GetType().Name})";
				case Component:
					return "Destroyed";
				default:
					return owner.GetType().Name;
			}
		}

		/// <summary>
		/// The end of text, at most max characters, cut at a '/' where there is one so whole names show.
		/// </summary>
		private static string TrimStart(string text, int max)
		{
			if (text.Length <= max) {
				return text;
			}

			var start = text.Length - (max - 2);
			var slash = text.IndexOf('/', start);

			return "…" + (slash >= 0 && slash < text.Length - 1 ? text.Substring(slash) : "/" + text.Substring(start));
		}

#if UNITY_EDITOR
		/// <summary>
		/// Selects target in the editor and pings it in the Hierarchy, so the Inspector shows it.
		/// </summary>
		private static void SelectInHierarchy(GameObject target)
		{
			if (target == null) {
				return;
			}

			UnityEditor.Selection.activeGameObject = target;
			UnityEditor.EditorGUIUtility.PingObject(target);
		}
#endif

		private static string Name(GameObject target)
		{
			return target != null ? target.name : "None";
		}

		/// <summary>
		/// The selected object's place in the hierarchy, from the root down.
		/// </summary>
		private static string Path(GameObject target)
		{
			if (target == null) {
				return "None";
			}

			var path = target.name;

			for (var parent = target.transform.parent; parent != null; parent = parent.parent) {
				path = parent.name + "/" + path;
			}

			return path;
		}
	}
}
