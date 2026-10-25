using System.Collections.Generic;
using System.Text.RegularExpressions;
using Tekly.DevBoard.Components;
using Tekly.Trellis;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tekly.DevBoard.Console
{
	/// <summary>
	/// The console's output: all of it in one read-only text field, so a drag selects across lines and Ctrl+C
	/// copies it. It sizes itself to its text, for the scroll view around it to scroll. Copies leave out the rich
	/// text tags the console adds for colour. Set up in the console_output prefab.
	/// </summary>
	public sealed class ConsoleOutput : TMP_InputField
	{
		// Only the tags the console writes. Any other angle brackets are the user's text, like <item:text>
		private static readonly Regex s_tags = new(@"</?(color|b|i|noparse)(=[^>]*)?>|<alpha=[^>]*>", RegexOptions.Compiled);

		[SerializeField] private LayoutItem m_layout;

		// Pixels a second the view scrolls for each pixel the pointer is beyond its edge
		private const float AUTO_SCROLL_SPEED = 8f;

		private ScrollRect m_scrollParent;
		private PointerEventData m_drag;
		private bool m_styled;
		private bool m_textChanged;
		private float m_shownHeight = -1f;
		private float m_shownWidth = -1f;

		/// <summary>
		/// Shows the lines, oldest first.
		/// </summary>
		public void SetLines(IReadOnlyList<string> lines)
		{
			// Like a monospaced label. Needs the text's style sheet, which isn't there until the text has woken up,
			// after this has been enabled, so it can't be done in OnEnable or in the prefab
			if (!m_styled) {
				textComponent.textStyle = textComponent.styleSheet.GetStyle(LabelWidget.MONOSPACED);
				m_styled = true;
			}

			SetTextWithoutNotify(string.Join("\n", lines));
			m_textChanged = true;
		}

		/// <summary>
		/// Matches the field's height to its text. Called each frame: it only measures, which is a full TMP layout
		/// of the text, when the text or the width changed.
		/// </summary>
		public void FitToText()
		{
			var width = ((RectTransform) transform).rect.width;

			if (width <= 0f || (!m_textChanged && Mathf.Approximately(width, m_shownWidth))) {
				return;
			}

			var height = textComponent.GetPreferredValues(text, width, 0f).y;
			m_textChanged = false;
			m_shownWidth = width;

			if (Mathf.Approximately(height, m_shownHeight)) {
				return;
			}

			m_shownHeight = height;
			m_layout.PreferredHeight = height;
		}

		public override void OnBeginDrag(PointerEventData eventData)
		{
			m_drag = eventData;
			base.OnBeginDrag(eventData);
		}

		public override void OnDrag(PointerEventData eventData)
		{
			m_drag = eventData;
			base.OnDrag(eventData);
		}

		public override void OnEndDrag(PointerEventData eventData)
		{
			m_drag = null;
			base.OnEndDrag(eventData);
		}

		/// <summary>
		/// Dragging a selection past the top or bottom of the view scrolls it, faster the further out the pointer is.
		/// The field only follows the pointer when it moves, so it's fed the drag again once the view has moved under
		/// a pointer that's holding still.
		/// </summary>
		private void Update()
		{
			if (m_drag == null) {
				return;
			}

			var scroll = ScrollParent;

			if (scroll == null) {
				return;
			}

			var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform) scroll.transform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, m_drag.position, m_drag.pressEventCamera, out var local);

			var view = viewport.rect;
			var over = local.y > view.yMax ? local.y - view.yMax : local.y < view.yMin ? local.y - view.yMin : 0f;
			var range = scroll.content.rect.height - view.height;

			if (over == 0f || range <= 0f) {
				return;
			}

			scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + over * AUTO_SCROLL_SPEED * Time.unscaledDeltaTime / range);
			base.OnDrag(m_drag);
		}

		private ScrollRect ScrollParent {
			get {
				if (m_scrollParent == null) {
					m_scrollParent = GetComponentInParent<ScrollRect>();
				}

				return m_scrollParent;
			}
		}

		/// <summary>
		/// The field is as tall as its text, so there's nothing for it to scroll. It would still take the wheel from the
		/// scroll view around it, so the wheel goes to that instead.
		/// </summary>
		public override void OnScroll(PointerEventData eventData)
		{
			var scroll = ScrollParent;

			if (scroll != null) {
				scroll.OnScroll(eventData);
			}
		}

		public override void OnUpdateSelected(BaseEventData eventData)
		{
			var before = GUIUtility.systemCopyBuffer;
			base.OnUpdateSelected(eventData);
			var after = GUIUtility.systemCopyBuffer;

			// The base class copies the selection with its tags. Only a copy changes the buffer
			if (after != before && after.IndexOf('<') >= 0) {
				GUIUtility.systemCopyBuffer = s_tags.Replace(after, string.Empty);
			}
		}
	}
}
