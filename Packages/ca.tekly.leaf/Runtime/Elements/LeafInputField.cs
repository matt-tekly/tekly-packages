using TMPro;
using UnityEngine.EventSystems;

namespace Tekly.Leaf.Elements
{
	public class LeafInputField : TMP_InputField
	{
		private LeafNavigationElement m_leaf;
		
		protected override void Awake()
		{
			base.Awake();
			m_leaf = GetComponent<LeafNavigationElement>();
		}
		
		public override void OnMove(AxisEventData eventData)
		{
			if (isFocused) {
				return;
			}
			
			if (m_leaf != null) {
				m_leaf.TryNavigate(eventData);	
			}
		}
	}
}