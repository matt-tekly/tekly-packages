using System;
using System.Collections.Generic;
using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// Picks one of a list of choices. The button shows the current choice, and clicking it opens the choices
	/// in a popup floating over the board, under the button (or over it, when there's more room above). Pressing
	/// outside the popup closes it. Works on indexes, so the ContainerWidget.Dropdown overloads can bind it to
	/// strings, enums or any other list.
	///
	/// The popup and the choice buttons are widget variants set on the prefab or with WithPopupVariant and
	/// WithOptionVariant: "dropdown_popup" and "dropdown_option" by default, falling back to "scrollview" and
	/// "button" when those aren't registered.
	/// Its layout should fit its content, like scrollview's LayoutProxy does. The dropdown bounds it: at least
	/// as wide as the button, and no taller than the room available.
	///
	/// With WithSearch, the popup is a search field with results, for long lists.
	/// </summary>
	public class DropdownWidget : Widget
	{
		[SerializeField] private LabelWidget m_label;
		[SerializeField] private ButtonWidget m_button;

		[Tooltip("Widget variant for the popup the choices open in. Falls back to \"scrollview\" when not registered")]
		[SerializeField] private string m_popupVariant = "dropdown_popup";

		[Tooltip("Button variant for each choice. Falls back to \"button\" when not registered")]
		[SerializeField] private string m_optionVariant = "dropdown_option";

		private const string NO_CHOICE = "-";
		private const string POPUP_FALLBACK_VARIANT = "scrollview";
		private const string OPTION_FALLBACK_VARIANT = "button";

		// Opens upwards when there's less than this much room below and more room above
		private const float MIN_HEIGHT_BELOW = 160f;
		private const float POPUP_GAP = 2f;

		private static readonly Vector3[] s_corners = new Vector3[4];

		private Func<int> m_getCount;
		private Func<int, string> m_getText;
		private Func<int> m_getIndex;
		private Action<int> m_setIndex;

		private bool m_searchable;
		private SearchList<int> m_search;
		private int m_shownIndex = int.MinValue;

		private PopupBlocker m_blocker;
		private ScrollViewWidget m_popup;
		private bool m_closeRequested;

		public bool IsOpen => m_blocker != null;

		private void Awake()
		{
			m_button.WithAction(Toggle);
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			// The page was switched, the panel hidden, or the dropdown is being destroyed
			Close();
		}

		public void Initialize(string label, Func<int> getCount, Func<int, string> getText, Func<int> getIndex, Action<int> setIndex)
		{
			m_getCount = getCount;
			m_getText = getText;
			m_getIndex = getIndex;
			m_setIndex = setIndex;

			if (m_label != null) {
				m_label.Text = label;
				m_label.gameObject.SetActive(!string.IsNullOrEmpty(label));
			}

			// Awake can run after Initialize when the dropdown is created inside an inactive container
			m_button.WithAction(Toggle);
			Close();

			try {
				Refresh();
			} catch (Exception) {
				// Don't break the code building the board. Tick retries, and logs the error if it keeps throwing
			}
		}

		/// <summary>
		/// Sets the widget variant of the popup the choices open in, instead of the prefab's.
		/// </summary>
		public DropdownWidget WithPopupVariant(string variant)
		{
			m_popupVariant = variant;
			Close();

			return this;
		}

		/// <summary>
		/// Sets the button variant used for each choice, instead of the prefab's.
		/// </summary>
		public DropdownWidget WithOptionVariant(string variant)
		{
			m_optionVariant = variant;
			Close();

			return this;
		}

		/// <summary>
		/// Shows a search field instead of the full list when opened.
		/// </summary>
		public DropdownWidget WithSearch(bool searchable = true)
		{
			m_searchable = searchable;
			Close();

			return this;
		}

		public void Toggle()
		{
			if (IsOpen) {
				Close();
			} else {
				Open();
			}
		}

		public void Open()
		{
			Close();

			var layer = DevBoard.Instance?.PopupLayer;

			if (layer == null || !isActiveAndEnabled) {
				return;
			}

			if (!DevBoard.Instance.TryGet(m_popupVariant, out ScrollViewWidget prefab)) {
				prefab = DevBoard.Instance.Get<ScrollViewWidget>(POPUP_FALLBACK_VARIANT);
			}

			m_blocker = PopupBlocker.Create(layer, $"Dropdown {name}", RequestClose);
			m_popup = Instantiate(prefab, m_blocker.RectTransform, false);
			m_popup.WithoutSavedState();

			if (m_searchable) {
				var root = ContainerWidget.CreatePlain(m_popup.Content, "Search");
				m_search = new SearchList<int>(root, AllIndexes, i => m_getText(i), BuildSearchRow, "Search");
			} else {
				var count = m_getCount();

				for (var i = 0; i < count; i++) {
					var index = i;
					m_popup.Button(m_getText(index), () => Select(index), OptionVariant());
				}
			}

			Place();
		}

		public void Close()
		{
			m_closeRequested = false;
			m_search?.Dispose();
			m_search = null;
			m_popup = null;

			if (m_blocker != null) {
				ContainerWidget.DestroyWidget(m_blocker.gameObject);
				m_blocker = null;
			}
		}

		protected override void Tick()
		{
			Refresh();
		}

		private void LateUpdate()
		{
			if (m_closeRequested) {
				Close();
			} else if (IsOpen) {
				// Follow the button if the panel moves or resizes while the popup is open
				Place();
			}
		}

		/// <summary>
		/// Closing destroys the popup, so it waits until after the click or press that asked for it.
		/// </summary>
		private void RequestClose()
		{
			m_closeRequested = true;
		}

		private void Select(int index)
		{
			m_setIndex?.Invoke(index);
			RequestClose();
			Refresh();
		}

		/// <summary>
		/// Lines the popup up with the button, below it or above it, and caps its height to the room on that side.
		/// </summary>
		private void Place()
		{
			var layer = m_blocker.RectTransform;
			var layerRect = layer.rect;

			((RectTransform) m_button.transform).GetWorldCorners(s_corners);
			var bottomLeft = (Vector2) layer.InverseTransformPoint(s_corners[0]);
			var topRight = (Vector2) layer.InverseTransformPoint(s_corners[2]);

			var roomBelow = bottomLeft.y - layerRect.yMin - POPUP_GAP;
			var roomAbove = layerRect.yMax - topRight.y - POPUP_GAP;
			var below = roomBelow >= MIN_HEIGHT_BELOW || roomBelow >= roomAbove;

			var popup = (RectTransform) m_popup.transform;
			var buttonWidth = topRight.x - bottomLeft.x;
			var room = Mathf.Max(0f, below ? roomBelow : roomAbove);

			// The popup's own layout (a LayoutProxy on scroll views) sizes it to the choices. Bound it: at least
			// as wide as the button, no wider than the screen, no taller than the room on its side.
			if (popup.TryGetComponent(out LayoutItem popupLayout)) {
				popupLayout.MinWidth = buttonWidth;
				popupLayout.MaxWidth = layerRect.width;
				popupLayout.MaxHeight = room;
			} else {
				popup.sizeDelta = new Vector2(buttonWidth, room);
			}

			// Lined up with the button's left edge, shifted left when that would run off the right of the screen.
			// The width is from the last layout, so a popup that grows settles on the next frame.
			var width = popup.rect.width;
			var x = Mathf.Max(layerRect.xMin, Mathf.Min(bottomLeft.x, layerRect.xMax - width));

			popup.anchorMin = popup.anchorMax = layer.pivot;
			popup.pivot = new Vector2(0f, below ? 1f : 0f);
			popup.anchoredPosition = new Vector2(x, below ? bottomLeft.y - POPUP_GAP : topRight.y + POPUP_GAP);
		}

		private string OptionVariant()
		{
			return DevBoard.Instance.TryGet(m_optionVariant, out ButtonWidget _) ? m_optionVariant : OPTION_FALLBACK_VARIANT;
		}

		private void BuildSearchRow(SearchRow<int> row)
		{
			row.Root.Button(m_getText(row.Item), () => Select(row.Item), OptionVariant());
		}

		private IEnumerable<int> AllIndexes()
		{
			var count = m_getCount();

			for (var i = 0; i < count; i++) {
				yield return i;
			}
		}

		private void Refresh()
		{
			if (m_getIndex == null) {
				return;
			}

			var index = m_getIndex();

			if (index == m_shownIndex) {
				return;
			}

			m_shownIndex = index;
			m_button.Label = index >= 0 && index < m_getCount() ? m_getText(index) : NO_CHOICE;
		}
	}
}
