using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// Removes segments when its GameObject is destroyed: ones bound with <see cref="PageSegment.BindTo"/>, and
	/// ones added with DevBoard.Page whose owner is this GameObject or one of its components.
	/// </summary>
	[AddComponentMenu("")]
	public class PageSegmentBinding : MonoBehaviour
	{
		private readonly List<PageSegment> m_segments = new();
		private readonly List<object> m_owners = new();

		/// <summary>
		/// Releases owner's segments when gameObject is destroyed.
		/// </summary>
		internal static void Watch(GameObject gameObject, object owner)
		{
			Get(gameObject).m_owners.Add(owner);
		}

		internal static PageSegmentBinding Get(GameObject gameObject)
		{
			if (!gameObject.TryGetComponent(out PageSegmentBinding binding)) {
				binding = gameObject.AddComponent<PageSegmentBinding>();
			}

			return binding;
		}

		internal void Add(PageSegment segment)
		{
			m_segments.Add(segment);
		}

		private void OnDestroy()
		{
			foreach (var segment in m_segments) {
				segment.Dispose();
			}

			m_segments.Clear();

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
