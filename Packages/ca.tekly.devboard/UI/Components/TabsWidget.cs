using System;
using System.Collections.Generic;
using Tekly.Common.Ui.Fancy;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// A strip of tab buttons over the content of the selected tab. The tabs' contents go in Content; which one shows
	/// is up to whoever fills it, e.g. a TabsMirror. The buttons are the "button_tab" prefab, or the one set on the
	/// prefab, and show the selected tab through the button's On state.
	/// </summary>
	public class TabsWidget : ContainerWidget
	{
		[Tooltip("Where the tab buttons go")]
		[SerializeField] private RectTransform m_strip;

		[Tooltip("The widget prefab for each tab's button. Empty for button_tab")]
		[SerializeField] private string m_tabPrefab;

		[Tooltip("Optional. The content's background. Its top left corner is square while the first tab, which sits on it, is selected")]
		[SerializeField] private FancyRect m_contentBackground;

		[Tooltip("The radius of the content background's corners")]
		[SerializeField] private float m_contentRadius = 8f;

		private readonly List<ButtonWidget> m_buttons = new();
		private int m_selected = -1;

		/// <summary>
		/// Replaces the buttons with one per label. select is called with a button's index when it's clicked.
		/// </summary>
		public void SetTabs(IReadOnlyList<string> labels, Action<int> select)
		{
			foreach (var button in m_buttons) {
				DestroyWidget(button.gameObject);
			}

			m_buttons.Clear();

			var prefab = string.IsNullOrEmpty(m_tabPrefab) ? WidgetPrefabs.TabButton : new WidgetPrefab<ButtonWidget>(m_tabPrefab);

			for (var i = 0; i < labels.Count; i++) {
				var index = i;
				var button = Instantiate(DevBoardUI.Instance.Get(prefab), m_strip, false);
				button.Initialize(() => select(index), labels[i]);
				button.WithOn(i == m_selected, true);

				m_buttons.Add(button);
			}
		}

		/// <summary>
		/// Marks the button at index as selected, and the rest as not. -1 for none.
		/// </summary>
		public void SetSelected(int index)
		{
			if (index == m_selected) {
				return;
			}

			m_selected = index;

			for (var i = 0; i < m_buttons.Count; i++) {
				m_buttons[i].WithOn(i == index);
			}

			if (m_contentBackground != null) {
				m_contentBackground.CornerRadii = new Vector4(index == 0 ? 0f : m_contentRadius, m_contentRadius, m_contentRadius, m_contentRadius);
			}
		}
	}
}
