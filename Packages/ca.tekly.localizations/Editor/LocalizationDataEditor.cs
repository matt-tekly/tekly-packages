using System;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tekly.Localizations
{
	/// <summary>
	/// Read-only, searchable table view of a LocalizationData's strings.
	/// Shared by the LocalizationData inspector and the .jsonloc importer inspector.
	/// </summary>
	public class LocalizationDataView
	{
		private const float ID_COLUMN_RATIO = 0.35f;
		private const float MIN_ID_WIDTH = 100f;
		private const float PADDING = 4f;

		private SearchField m_searchField;
		private string m_search = "";

		private static GUIStyle s_idStyle;
		private static GUIStyle s_formatStyle;
		private static readonly Color s_altRowColor = new Color(0f, 0f, 0f, 0.08f);
		private static readonly Color s_lineColor = new Color(0f, 0f, 0f, 0.2f);

		public void Draw(LocalizationData data)
		{
			EnsureStyles();

			var strings = data.Strings ?? Array.Empty<LocalizationStringData>();

			using (new EditorGUILayout.HorizontalScope()) {
				m_searchField ??= new SearchField();
				m_search = m_searchField.OnGUI(m_search);
			}

			var visible = 0;
			var rowIndex = 0;

			EditorGUILayout.Space(2);
			DrawHeader();

			foreach (var stringData in strings) {
				if (!Matches(stringData)) {
					continue;
				}

				DrawRow(stringData, rowIndex++);
				visible++;
			}

			EditorGUILayout.Space(2);

			var countLabel = string.IsNullOrEmpty(m_search)
				? $"{strings.Length} strings"
				: $"{visible} of {strings.Length} strings";

			EditorGUILayout.LabelField(countLabel, EditorStyles.centeredGreyMiniLabel);
		}

		private bool Matches(LocalizationStringData stringData)
		{
			if (string.IsNullOrEmpty(m_search)) {
				return true;
			}

			return Contains(stringData.Id, m_search) || Contains(stringData.Format, m_search);
		}

		private static bool Contains(string value, string search)
		{
			return value != null && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void DrawHeader()
		{
			var rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
			SplitColumns(rect, out var idRect, out var formatRect);

			EditorGUI.LabelField(idRect, "Id", EditorStyles.boldLabel);
			EditorGUI.LabelField(formatRect, "Text", EditorStyles.boldLabel);

			EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), s_lineColor);
		}

		private static void DrawRow(LocalizationStringData stringData, int rowIndex)
		{
			var width = EditorGUIUtility.currentViewWidth - 40f;
			SplitColumns(new Rect(0, 0, width, 0), out var idMeasure, out var formatMeasure);

			var id = stringData.Id ?? "";
			var format = stringData.Format ?? "";

			var height = Mathf.Max(
				s_idStyle.CalcHeight(new GUIContent(id), idMeasure.width),
				s_formatStyle.CalcHeight(new GUIContent(format), formatMeasure.width)) + PADDING;

			var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));

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
			m_view.Draw((LocalizationData)target);
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
				m_view.Draw(data);
			} else {
				EditorGUILayout.HelpBox("No localization data imported.", MessageType.Info);
			}
		}
	}
}
