using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafToggle : Toggle
	{
		private LeafNavigationElement m_leaf;

		protected override void Awake()
		{
			base.Awake();
			m_leaf = GetComponent<LeafNavigationElement>();
		}

		public override void OnMove(AxisEventData eventData)
		{
			if (m_leaf != null && m_leaf.TryNavigate(eventData)) {
				return;
			}

			// base.OnMove(eventData);
		}
	}
}