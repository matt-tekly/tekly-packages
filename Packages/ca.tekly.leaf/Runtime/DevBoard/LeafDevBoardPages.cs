using Tekly.DevBoard;
using Tekly.DevBoard.Nodes;
using UnityEngine;
using UnityEngine.EventSystems;

// Tekly.DevBoard is also a namespace, which wins over the class when written from inside Tekly.Leaf
using DevBoardApi = Tekly.DevBoard.DevBoard;

namespace Tekly.Leaf.Integration
{
	/// <summary>
	/// Adds Leaf's pages to DevBoard. This assembly only compiles when the DevBoard package is in the project, so
	/// Leaf itself doesn't depend on it.
	/// </summary>
	internal static class LeafDevBoardPages
	{
		private const string PATH = "System/Leaf";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void Register()
		{
			DevBoardApi.Page(PATH, Build, owner: typeof(LeafDevBoardPages));
		}

		private static void Build(Segment page)
		{
			page.Heading("Input");
			var form = page.Form();
			form.Property("Disabled", () => LeafCore.Instance.IsInputDisabled ? "Yes" : "No");

			page.Heading("Selection");
			form = page.Form();
			form.Property("Selected", () => Name(LeafCore.Instance.Selection.Current.Value));
			form.Property("Path", () => Path(LeafCore.Instance.Selection.Current.Value));
			form.Property("EventSystem", () => EventSystem.current != null ? EventSystem.current.name : "None");
		}

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
