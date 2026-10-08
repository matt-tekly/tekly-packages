using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tekly.Localizations
{
	/// <summary>
	/// Read-only, searchable table view of a LocalizationData's strings.
	/// Shared by the LocalizationData inspector and the .jloc importer inspector.
	///
	/// Virtualized: the filtered row list and row heights are cached and only rebuilt when the
	/// data, search text or width changes, and only rows inside the scroll viewport are drawn.
	/// </summary>
	public class LocalizationDataView
	{
		private const float ID_COLUMN_RATIO = 0.35f;
		private const float MIN_ID_WIDTH = 100f;
		private const float PADDING = 4f;
		private const float SCROLLBAR_WIDTH = 14f;
		private const float MIN_VIEW_HEIGHT = 120f;
		private const float MAX_VIEW_HEIGHT = 600f;

		private SearchField m_searchField;
		private string m_search = "";
		private Vector2 m_scroll;

		// Caches
		private readonly List<int> m_filtered = new List<int>();
		private readonly List<float> m_offsets = new List<float>();
		private readonly List<float> m_heights = new List<float>();
		private LocalizationStringData[] m_cachedStrings;
		private string m_cachedSearch;
		private float m_cachedWidth = -1f;
		private float m_totalHeight;
		private float m_width;

		private static GUIStyle s_idStyle;
		private static GUIStyle s_formatStyle;
		private static readonly GUIContent s_content = new GUIContent();
		private static readonly Color s_altRowColor = new Color(0f, 0f, 0f, 0.08f);
		private static readonly Color s_lineColor = new Color(0f, 0f, 0f, 0.2f);

		public void Draw(LocalizationData data, Editor owner)
		{
			EnsureStyles();

			var strings = data.Strings ?? Array.Empty<LocalizationStringData>();

			m_searchField ??= new SearchField();
			m_search = m_searchField.OnGUI(m_search) ?? "";

			if (m_width <= 0f) {
				m_width = EditorGUIUtility.currentViewWidth - 40f;
			}

			EnsureFiltered(strings);
			EnsureHeights(strings, m_width - SCROLLBAR_WIDTH);

			EditorGUILayout.Space(2);
			DrawHeader(m_width - SCROLLBAR_WIDTH);

			var viewHeight = Mathf.Clamp(m_totalHeight, MIN_VIEW_HEIGHT, MAX_VIEW_HEIGHT);
			var viewRect = GUILayoutUtility.GetRect(0, viewHeight, GUILayout.ExpandWidth(true));

			// Layout events report a dummy rect; only trust the width on Repaint.
			if (Event.current.type == EventType.Repaint && Mathf.Abs(viewRect.width - m_width) > 1f) {
				m_width = viewRect.width;
				owner.Repaint();
			}

			DrawRows(strings, viewRect);

			EditorGUILayout.Space(2);

			var countLabel = m_search.Length == 0
				? $"{strings.Length} strings"
				: $"{m_filtered.Count} of {strings.Length} strings";

			EditorGUILayout.LabelField(countLabel, EditorStyles.centeredGreyMiniLabel);
		}

		private void DrawRows(LocalizationStringData[] strings, Rect viewRect)
		{
			var contentWidth = viewRect.width - SCROLLBAR_WIDTH;
			var contentRect = new Rect(0, 0, contentWidth, m_totalHeight);

			m_scroll = GUI.BeginScrollView(viewRect, m_scroll, contentRect, false, true);

			var top = m_scroll.y;
			var bottom = top + viewRect.height;

			for (var row = FindFirstVisible(top); row < m_filtered.Count; row++) {
				var y = m_offsets[row];

				if (y > bottom) {
					break;
				}

				var rowRect = new Rect(0, y, contentWidth, m_heights[row]);
				DrawRow(strings[m_filtered[row]], rowRect, row);
			}

			GUI.EndScrollView();
		}

		private int FindFirstVisible(float top)
		{
			var low = 0;
			var high = m_filtered.Count - 1;
			var result = 0;

			while (low <= high) {
				var mid = (low + high) / 2;

				if (m_offsets[mid] + m_heights[mid] < top) {
					low = mid + 1;
				} else {
					result = mid;
					high = mid - 1;
				}
			}

			return result;
		}

		private void EnsureFiltered(LocalizationStringData[] strings)
		{
			if (ReferenceEquals(strings, m_cachedStrings) && m_search == m_cachedSearch) {
				return;
			}

			m_cachedStrings = strings;
			m_cachedSearch = m_search;
			m_cachedWidth = -1f;
			m_scroll = Vector2.zero;
			m_filtered.Clear();

			for (var i = 0; i < strings.Length; i++) {
				if (Matches(strings[i])) {
					m_filtered.Add(i);
				}
			}
		}

		private void EnsureHeights(LocalizationStringData[] strings, float width)
		{
			if (Mathf.Abs(width - m_cachedWidth) < 1f) {
				return;
			}

			m_cachedWidth = width;
			m_offsets.Clear();
			m_heights.Clear();

			SplitColumns(new Rect(0, 0, width, 0), out var idRect, out var formatRect);

			var y = 0f;
			foreach (var index in m_filtered) {
				var stringData = strings[index];

				s_content.text = stringData.Id ?? "";
				var idHeight = s_idStyle.CalcHeight(s_content, idRect.width);

				s_content.text = stringData.Format ?? "";
				var formatHeight = s_formatStyle.CalcHeight(s_content, formatRect.width);

				var height = Mathf.Max(idHeight, formatHeight) + PADDING;

				m_offsets.Add(y);
				m_heights.Add(height);
				y += height;
			}

			s_content.text = null;
			m_totalHeight = y;
		}

		private bool Matches(LocalizationStringData stringData)
		{
			if (m_search.Length == 0) {
				return true;
			}

			return Contains(stringData.Id, m_search) || Contains(stringData.Format, m_search);
		}

		private static bool Contains(string value, string search)
		{
			return value != null && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void DrawHeader(float width)
		{
			var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
			rect.width = width;
			SplitColumns(rect, out var idRect, out var formatRect);

			EditorGUI.LabelField(idRect, "Id", EditorStyles.boldLabel);
			EditorGUI.LabelField(formatRect, "Text", EditorStyles.boldLabel);

			EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), s_lineColor);
		}

		private static void DrawRow(LocalizationStringData stringData, Rect rect, int rowIndex)
		{
			var id = stringData.Id ?? "";
			var format = stringData.Format ?? "";

			if (rowIndex % 2 == 1) {
				EditorGUI.DrawRect(rect, s_altRowColor);
			}

			SplitColumns(rect, out var idRect, out var formatRect);
			idRect.y += PADDING * 0.5f;
			formatRect.y += PADDING * 0.5f;
			idRect.height -= PADDING;
			formatRect.height -= PADDING;

			EditorGUI.SelectableLabel(idRect, id, s_idStyle);
			EditorGUI.SelectableLabel(formatRect, format, s_formatStyle);

			var evt = Event.current;
			if (evt.type == EventType.ContextClick && rect.Contains(evt.mousePosition)) {
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Copy Id"), false, () => EditorGUIUtility.systemCopyBuffer = id);
				menu.AddItem(new GUIContent("Copy Text"), false, () => EditorGUIUtility.systemCopyBuffer = format);
				menu.ShowAsContext();
				evt.Use();
			}
		}

		private static void SplitColumns(Rect rect, out Rect idRect, out Rect formatRect)
		{
			var idWidth = Mathf.Max(MIN_ID_WIDTH, rect.width * ID_COLUMN_RATIO);

			idRect = new Rect(rect.x, rect.y, idWidth - PADDING, rect.height);
			formatRect = new Rect(rect.x + idWidth, rect.y, rect.width - idWidth, rect.height);
		}

		private static void EnsureStyles()
		{
			if (s_idStyle != null) {
				return;
			}

			s_idStyle = new GUIStyle(EditorStyles.label) {
				wordWrap = true,
				richText = false,
				fontStyle = FontStyle.Bold
			};

			// Rich text off so <color> etc. show as written instead of rendering.
			s_formatStyle = new GUIStyle(EditorStyles.label) {
				wordWrap = true,
				richText = false
			};
		}
	}

	[CustomEditor(typeof(LocalizationData))]
	public class LocalizationDataEditor : Editor
	{
		private LocalizationDataView m_view;

		public override void OnInspectorGUI()
		{
			m_view ??= new LocalizationDataView();
			m_view.Draw((LocalizationData)target, this);
		}
	}

	[CustomEditor(typeof(LocalizationDataImporter))]
	public class LocalizationDataImporterEditor : ScriptedImporterEditor
	{
		private LocalizationDataView m_view;

		public override bool showImportedObject => false;
		protected override bool needsApplyRevert => false;

		public override void OnInspectorGUI()
		{
			if (assetTarget is LocalizationData data) {
				m_view ??= new LocalizationDataView();
				m_view.Draw(data, this);
			} else {
				EditorGUILayout.HelpBox("No localization data imported.", MessageType.Info);
			}
		}
	}
}
