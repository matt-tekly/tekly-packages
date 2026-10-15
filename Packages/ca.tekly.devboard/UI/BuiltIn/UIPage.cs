using System.Collections.Generic;
using Tekly.DevBoard.Nodes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.DevBoard.BuiltIn
{
	/// <summary>
	/// The EventSystem's selection, and hiding the game's canvases to see what's behind them. DevBoard's own
	/// canvas is left alone.
	/// </summary>
	internal static class UIPage
	{
		public const string PATH = "System/UI";

		// The canvases Hide turned off, so Show turns back on only those, not ones the game had off itself
		private static readonly List<Canvas> s_hidden = new();

		/// <summary>
		/// Forgets the last session's canvases, for when entering Play mode doesn't reload the domain.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetState()
		{
			s_hidden.Clear();
		}

		public static void Register(object owner)
		{
			DevBoard.Page(PATH, Build, owner: owner);
		}

		private static void Build(Segment page)
		{
			page.Heading("Selection");
			var form = page.Form();
			form.Property("Selected", () => SelectionName());
			form.Property("EventSystem", () => EventSystem.current != null ? EventSystem.current.name : "None");

			page.Heading("Canvases");
			form = page.Form();
			// Hide All hides every enabled canvas but DevBoard's
			form.Property("Hidden", () => s_hidden.Count);

			var row = form.Row().WithPadding(0);
			row.Button("Hide All", HideCanvases).InteractiveOnly();
			row.Button("Show All", ShowCanvases)
				.InteractableWhen(() => s_hidden.Count > 0)
				.InteractiveOnly();
		}

		private static string SelectionName()
		{
			if (EventSystem.current == null) {
				return "No EventSystem";
			}

			var selected = EventSystem.current.currentSelectedGameObject;
			return selected != null ? selected.name : "None";
		}

		private static void HideCanvases()
		{
			var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			foreach (var canvas in canvases) {
				if (!canvas.enabled || DevBoardUI.Instance.IsDevBoardObject(canvas.gameObject)) {
					continue;
				}

				canvas.enabled = false;
				s_hidden.Add(canvas);
			}
		}

		private static void ShowCanvases()
		{
			foreach (var canvas in s_hidden) {
				// Destroyed since, e.g. with its scene
				if (canvas != null) {
					canvas.enabled = true;
				}
			}

			s_hidden.Clear();
		}
	}
}
