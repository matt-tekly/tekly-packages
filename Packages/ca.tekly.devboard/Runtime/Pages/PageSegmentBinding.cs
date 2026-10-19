using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// Releases owners when its GameObject is destroyed: the segments and commands added with DevBoard.Page,
	/// DevBoard.Command and DevBoard.CommandsFrom whose owner is this GameObject or one of its components, so
	/// MonoBehaviours don't need to call DevBoard.Release themselves.
	/// </summary>
	[AddComponentMenu("")]
	public class PageSegmentBinding : MonoBehaviour
	{
		private readonly List<object> m_owners = new();

		/// <summary>
		/// Releases owner when gameObject is destroyed.
		/// </summary>
		internal static void Watch(GameObject gameObject, object owner)
		{
			if (!gameObject.TryGetComponent(out PageSegmentBinding binding)) {
				binding = gameObject.AddComponent<PageSegmentBinding>();
			}

			binding.m_owners.Add(owner);
		}

		private void OnDestroy()
		{
			// DevBoard can already be gone when leaving play mode
			var devBoard = DevBoard.Instance;

			if (devBoard != null) {
				foreach (var owner in m_owners) {
					devBoard.ReleaseOwner(owner);
				}
			}

			m_owners.Clear();
		}
	}
}
