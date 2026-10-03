using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Pages
{
	/// <summary>
	/// Added by <see cref="PageSegment.BindTo"/>. Disposes its segments when its GameObject is destroyed.
	/// </summary>
	[AddComponentMenu("")]
	public class PageSegmentBinding : MonoBehaviour
	{
		private readonly List<PageSegment> m_segments = new();

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
		}
	}
}
