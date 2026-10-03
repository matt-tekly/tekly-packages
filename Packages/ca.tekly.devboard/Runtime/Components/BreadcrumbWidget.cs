using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.DevBoard.Components
{
	/// <summary>
	/// One step in a breadcrumb trail: what to show, and the key handed back when it's selected.
	/// </summary>
	public readonly struct Crumb
	{
		public readonly string Title;
		public readonly string Key;

		public Crumb(string title, string key)
		{
			Title = title;
			Key = key;
		}
	}

	/// <summary>
	/// A trail like "DevBoard / Game / Economy" in a single text, where every crumb but the last is a link.
	/// The link under the pointer is drawn in the hover color.
	///
	/// Prefab setup: a TMP text with Raycast Target on, on this GameObject or a child of it (found automatically
	/// if Text isn't set). Long trails are handled by the text's own overflow settings.
	/// </summary>
	public class BreadcrumbWidget : Widget, IPointerMoveHandler, IPointerExitHandler, IPointerClickHandler
	{
		[SerializeField] private TMP_Text m_text;
		[SerializeField] private string m_separator = "/";
		[SerializeField] private Color m_linkColor = new Color(0.7f, 0.7f, 0.7f, 1f);
		[SerializeField] private Color m_hoverColor = Color.white;

		/// <summary>
		/// Drawn between crumbs, with a space either side.
		/// </summary>
		public string Separator {
			get => m_separator;
			set {
				m_separator = value;
				Refresh();
			}
		}

		private readonly List<Crumb> m_crumbs = new();
		private readonly StringBuilder m_builder = new();

		private Action<string> m_onSelect;
		private int m_hoveredLink = -1;

		/// <summary>
		/// Shows crumbs, first to last. onSelect gets the key of the crumb that was picked.
		/// </summary>
		public void Set(IReadOnlyList<Crumb> crumbs, Action<string> onSelect)
		{
			m_onSelect = onSelect;
			m_crumbs.Clear();

			if (crumbs != null) {
				m_crumbs.AddRange(crumbs);
			}

			m_hoveredLink = -1;
			Refresh();
		}

		public void OnPointerMove(PointerEventData eventData)
		{
			SetHovered(FindLink(eventData));
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			SetHovered(-1);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			var index = FindLink(eventData);

			if (index >= 0) {
				m_onSelect?.Invoke(m_crumbs[index].Key);
			}
		}

		private void Awake()
		{
			if (m_text == null) {
				m_text = GetComponentInChildren<TMP_Text>();
			}
		}

		private void SetHovered(int index)
		{
			if (index != m_hoveredLink) {
				m_hoveredLink = index;
				Refresh();
			}
		}

		/// <summary>
		/// The index of the crumb link under the pointer, or -1.
		/// </summary>
		private int FindLink(PointerEventData eventData)
		{
			if (m_text == null) {
				return -1;
			}

			var camera = eventData.enterEventCamera != null ? eventData.enterEventCamera : eventData.pressEventCamera;
			var link = TMP_TextUtilities.FindIntersectingLink(m_text, eventData.position, camera);

			if (link < 0 || link >= m_text.textInfo.linkCount) {
				return -1;
			}

			// Link ids are crumb indices
			return int.TryParse(m_text.textInfo.linkInfo[link].GetLinkID(), out var index) && index < m_crumbs.Count - 1
				? index
				: -1;
		}

		private void Refresh()
		{
			if (m_text == null) {
				return;
			}

			m_builder.Clear();

			var linkColor = ColorUtility.ToHtmlStringRGBA(m_linkColor);
			var hoverColor = ColorUtility.ToHtmlStringRGBA(m_hoverColor);

			for (var i = 0; i < m_crumbs.Count; i++) {
				if (i > 0) {
					m_builder.Append(' ').Append("<noparse>").Append(m_separator).Append("</noparse>").Append(' ');
				}

				var isLast = i == m_crumbs.Count - 1;

				if (!isLast) {
					var color = i == m_hoveredLink ? hoverColor : linkColor;
					m_builder.Append("<link=\"").Append(i).Append("\"><color=#").Append(color).Append('>');
				}

				// Titles are shown as written, even if they contain something that looks like a tag
				m_builder.Append("<noparse>").Append(m_crumbs[i].Title).Append("</noparse>");

				if (!isLast) {
					m_builder.Append("</color></link>");
				}
			}

			m_text.SetText(m_builder);
		}
	}
}
