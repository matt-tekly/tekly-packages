using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements
{
	public class LeafCursorElement : MonoBehaviour, IPointerEnterHandler
	{
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (TryGetComponent<Selectable>(out var selectable))
			{
				selectable.Select();	
			}
			
			
			if (TryGetComponent<Toggle>(out var toggle))
			{
				toggle.isOn = true;
			}
		}
	}
}