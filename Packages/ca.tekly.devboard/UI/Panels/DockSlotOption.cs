using UnityEngine;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// Marks a radio option in a panel's dock group with the slot it docks the panel to.
	/// </summary>
	public class DockSlotOption : MonoBehaviour
	{
		[SerializeField] private DockSlot m_slot;

		public DockSlot Slot => m_slot;
	}
}
