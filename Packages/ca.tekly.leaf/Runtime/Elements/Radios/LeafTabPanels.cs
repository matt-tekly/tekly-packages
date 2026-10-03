using System.Collections.Generic;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Leaf.Elements.Radios
{
	/// <summary>
	/// Shows the panel of the group's current option (its <see cref="LeafTab"/>) and hides the other tabs' panels,
	/// including tabs that are hidden. Follows every change of <see cref="LeafRadioGroup.Current"/>, including the
	/// first option becoming current and SetWithoutNotify.
	///
	/// The old panel hides before the new one shows. When the current option changes after a panel has been
	/// shown (a swap, not the starting state), a <see cref="LeafNavigationScope"/> on the new panel takes focus,
	/// waiting for something selectable if the panel is still being filled. Turn the panel scopes' Select On
	/// Enable off so the starting panel leaves focus to the screen.
	///
	/// The group can be anywhere, e.g. this can sit on the panels' container. It only follows the group while
	/// this component is enabled, and syncs again when re-enabled.
	/// </summary>
	public class LeafTabPanels : MonoBehaviour
	{
		[Tooltip("The group whose current option picks the panel. Filled from this GameObject or its parents when empty")]
		[SerializeField] private LeafRadioGroup m_group;

		private LeafRadioGroup m_subscribedGroup;
		private GameObject m_shown;
		private bool m_hasShownPanel;

		private static readonly List<LeafTab> s_tabs = new();

		private void OnEnable()
		{
			// Showing again starts over: the first panel shown is the starting state, not a swap
			m_shown = null;
			m_hasShownPanel = false;

			if (m_group == null) {
				TkLogger.Get<LeafTabPanels>().ErrorContext("Needs a LeafRadioGroup", this);
				return;
			}

			// Kept so OnDisable unsubscribes from the same group if the reference changes while enabled
			m_subscribedGroup = m_group;
			m_subscribedGroup.CurrentChanged += OnCurrentChanged;
			OnCurrentChanged(m_subscribedGroup.Current);
		}

		private void OnDisable()
		{
			if (m_subscribedGroup != null) {
				m_subscribedGroup.CurrentChanged -= OnCurrentChanged;
				m_subscribedGroup = null;
			}
		}

		private void OnCurrentChanged(ILeafRadioOption current)
		{
			var shown = current is Component component && component.TryGetComponent(out LeafTab currentTab)
				? currentTab.Panel
				: null;

			CollectTabs(s_tabs);

			// Hide first, so the selection in the old panel is hidden by the time the new one shows
			for (var i = 0; i < s_tabs.Count; i++) {
				var panel = s_tabs[i].Panel;
				if (panel != null && panel != shown) {
					panel.SetActive(false);
				}
			}

			s_tabs.Clear();

			if (shown != null) {
				shown.SetActive(true);
			}

			var isSwap = m_hasShownPanel && shown != null && shown != m_shown;

			m_shown = shown;
			m_hasShownPanel |= shown != null;

			if (isSwap && shown.TryGetComponent(out LeafNavigationScope scope)) {
				scope.TakeFocus();
			}
		}

		/// <summary>
		/// The group's tabs, hidden ones included. Tabs of nested groups are left out.
		/// </summary>
		private void CollectTabs(List<LeafTab> output)
		{
			m_subscribedGroup.GetComponentsInChildren(true, output);

			for (var i = output.Count - 1; i >= 0; i--) {
				if (output[i].GetComponentInParent<LeafRadioGroup>(true) != m_subscribedGroup) {
					output.RemoveAt(i);
				}
			}
		}

#if UNITY_EDITOR
		private void Reset()
		{
			m_group = GetComponentInParent<LeafRadioGroup>(true);
		}

		private void OnValidate()
		{
			if (m_group == null) {
				m_group = GetComponentInParent<LeafRadioGroup>(true);
			}
		}
#endif
	}
}
