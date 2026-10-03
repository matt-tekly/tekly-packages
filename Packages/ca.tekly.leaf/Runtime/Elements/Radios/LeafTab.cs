using UnityEngine;

namespace Tekly.Leaf.Elements.Radios
{
	/// <summary>
	/// Put beside a radio option (usually a <see cref="LeafRadioOptionUnselectable"/>) to give it a panel.
	/// <see cref="LeafTabPanels"/> shows the panel while the option is current. The option works the same
	/// without it.
	/// </summary>
	[DisallowMultipleComponent]
	public class LeafTab : MonoBehaviour
	{
		/// <summary>
		/// The radio option on this GameObject, or null if there isn't one.
		/// </summary>
		public ILeafRadioOption Option => GetComponent<ILeafRadioOption>();

		/// <summary>
		/// Shown while this tab's option is current. Several tabs can share one.
		/// </summary>
		public GameObject Panel => m_panel;

		[Tooltip("Shown while this tab is current. Several tabs can share one")]
		[SerializeField] private GameObject m_panel;

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (GetComponent<ILeafRadioOption>() == null) {
				Debug.LogWarning($"Tab [{name}] needs a LeafRadioOption or LeafRadioOptionUnselectable on the same GameObject", this);
			}
		}
#endif
	}
}
