using System;
using System.Collections.Generic;
using Tekly.Trellis;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// Picks one of a list of choices. The button shows the current choice, and clicking it opens the choices
	/// in a popup floating over the board, under the button (or over it, when there's more room above). Pressing
	/// outside the popup closes it. Works on indexes, so the Dropdown factories below can bind it to
	/// strings, enums or any other list.
	///
	/// The popup is a DropdownPopup ("dropdown_popup" by default), which lays itself out. The dropdown only places
	/// it and bounds it: at least as wide as the button, no wider than the screen, no taller than the room on its
	/// side. Each choice is a DropdownOption of the option prefab ("dropdown_option" by default), so a choice
	/// can be more than a line of text, e.g. a title with a subtitle under it.
	///
	/// With WithSearch, the popup shows a search field over the choices, for long lists.
	/// </summary>
	public class DropdownWidget : Widget
	{
		[SerializeField] private LabelWidget m_label;
		[SerializeField] private ButtonWidget m_button;

		[Tooltip("Name of the DropdownPopup prefab the choices open in")]
		[FormerlySerializedAs("m_popupVariant")]
		[SerializeField] private string m_popupPrefab = "dropdown_popup";

		[Tooltip("Name of the DropdownOption prefab for each choice")]
		[FormerlySerializedAs("m_optionVariant")]
		[SerializeField] private string m_optionPrefab = "dropdown_option";

		private const string NO_CHOICE = "-";

		// Opens upwards when there's less than this much room below and more room above
		private const float MIN_HEIGHT_BELOW = 160f;
		private const float POPUP_GAP = 2f;

		private static readonly Vector3[] s_corners = new Vector3[4];

		private Func<int> m_getCount;
		private Func<int, DropdownChoice> m_getChoice;
		private Binding<int> m_index;
		private BindingWatch<int> m_watch;

		private bool m_searchable;

		private PopupBlocker m_blocker;
		private DropdownPopup m_popup;
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

		/// <summary>
		/// index is the index of the current choice, -1 for none.
		/// </summary>
		public void Initialize(string label, Func<int> getCount, Func<int, DropdownChoice> getChoice, Binding<int> index)
		{
			m_getCount = getCount;
			m_getChoice = getChoice;
			m_index = index;
			m_watch.Invalidate();

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
		/// Sets the prefab the choices open in, instead of the one set on this dropdown's prefab.
		/// </summary>
		public DropdownWidget WithPopupPrefab(WidgetPrefab<DropdownPopup> prefab)
		{
			m_popupPrefab = prefab.Name;
			Close();

			return this;
		}

		/// <summary>
		/// Sets the prefab used for each choice, instead of the one set on this dropdown's prefab.
		/// </summary>
		public DropdownWidget WithOptionPrefab(WidgetPrefab<DropdownOption> prefab)
		{
			m_optionPrefab = prefab.Name;
			Close();

			return this;
		}

		/// <summary>
		/// Shows a search field over the choices when opened.
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

			var layer = DevBoardUI.Instance?.PopupLayer;

			if (layer == null || !isActiveAndEnabled) {
				return;
			}

			m_blocker = PopupBlocker.Create(layer, $"Dropdown {name}", RequestClose);
			m_popup = Instantiate(DevBoardUI.Instance.Get<DropdownPopup>(m_popupPrefab), m_blocker.RectTransform, false);
			var choices = new DropdownChoice[m_getCount()];

			for (var i = 0; i < choices.Length; i++) {
				choices[i] = m_getChoice(i);
			}

			m_popup.Initialize(choices, m_index.Read(), Select, m_searchable, m_optionPrefab);
			m_popup.Cancelled += RequestClose;

			Place();

			// Opening a searchable dropdown is usually to search it. Does nothing without a search field
			m_popup.FocusSearch();
		}

		public void Close()
		{
			m_closeRequested = false;
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
			m_index?.Write(index);
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

			// The popup sizes itself to its choices. Bound it: at least as wide as the button, no wider than the
			// screen, no taller than the room on its side (the list inside shrinks and scrolls)
			if (popup.TryGetComponent(out LayoutItem popupLayout)) {
				popupLayout.MinWidth = buttonWidth;
				popupLayout.MaxWidth = layerRect.width;
				popupLayout.MaxHeight = room;
			}

			// Lined up with the button's left edge, shifted left when that would run off the right of the screen.
			// The width is from the last layout, so a popup that grows settles on the next frame.
			var width = popup.rect.width;
			var x = Mathf.Max(layerRect.xMin, Mathf.Min(bottomLeft.x, layerRect.xMax - width));

			popup.anchorMin = popup.anchorMax = layer.pivot;
			popup.pivot = new Vector2(0f, below ? 1f : 0f);
			popup.anchoredPosition = new Vector2(x, below ? bottomLeft.y - POPUP_GAP : topRight.y + POPUP_GAP);
		}

		private void Refresh()
		{
			if (m_index == null || !m_watch.Poll(m_index, out var index)) {
				return;
			}

			m_button.Label = index >= 0 && index < m_getCount() ? m_getChoice(index).Title : NO_CHOICE;
		}
	}

	public static class DropdownWidgetFactory
	{
		/// <summary>
		/// A dropdown over a list of items. choices is read each time the dropdown refreshes or opens, so it can change.
		/// getChoice is what an option shows for an item, e.g. a title and subtitle. A getter value that isn't in
		/// choices shows as no choice.
		/// </summary>
		public static DropdownWidget Dropdown<T>(this ContainerWidget parent, string label, IReadOnlyList<T> choices,
			Func<T, DropdownChoice> getChoice, Func<T> getValue, Action<T> setValue, WidgetPrefab<DropdownWidget> prefab = default)
		{
			var comparer = EqualityComparer<T>.Default;

			int IndexOf(T value)
			{
				for (var i = 0; i < choices.Count; i++) {
					if (comparer.Equals(choices[i], value)) {
						return i;
					}
				}

				return -1;
			}

			var instance = parent.Create(prefab.Or(WidgetPrefabs.Dropdown));
			instance.Initialize(label, () => choices.Count, i => getChoice(choices[i]),
				new Binding<int>(() => IndexOf(getValue()), i => setValue(choices[i])));

			return instance;
		}

		/// <summary>
		/// A dropdown over a list of items, each shown as a line of text from getText.
		/// </summary>
		public static DropdownWidget Dropdown<T>(this ContainerWidget parent, string label, IReadOnlyList<T> choices,
			Func<T, string> getText, Func<T> getValue, Action<T> setValue, WidgetPrefab<DropdownWidget> prefab = default)
		{
			return parent.Dropdown(label, choices, item => new DropdownChoice(getText(item)), getValue, setValue, prefab);
		}

		public static DropdownWidget Dropdown(this ContainerWidget parent, string label, IReadOnlyList<string> choices,
			Func<string> getValue, Action<string> setValue, WidgetPrefab<DropdownWidget> prefab = default)
		{
			return parent.Dropdown(label, choices, s => s, getValue, setValue, prefab);
		}

		/// <summary>
		/// A dropdown over every value of an enum.
		/// </summary>
		public static DropdownWidget Dropdown<TEnum>(this ContainerWidget parent, string label, Func<TEnum> getValue,
			Action<TEnum> setValue, WidgetPrefab<DropdownWidget> prefab = default) where TEnum : struct, Enum
		{
			var values = (TEnum[]) Enum.GetValues(typeof(TEnum));
			return parent.Dropdown(label, values, v => v.ToString(), getValue, setValue, prefab);
		}
	}
}
