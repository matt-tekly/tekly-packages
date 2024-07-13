using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tekly.Common.Gui;
using Tekly.Common.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tekly.DataModels.Models
{
	public class ModelsWindow : EditorWindow, ISerializationCallbackReceiver
	{
		private const float INDENT = 18;
		private const float SCROLLBAR_WIDTH = 15;
		private static readonly float s_height = EditorGUIUtility.singleLineHeight;

		[SerializeField] private List<ObjectEntry> m_entries = new List<ObjectEntry>();
		[SerializeField] private List<string> m_collapsedEntriesList = new List<string>();

		[SerializeField] private Vector2 m_scrollPos;
		[SerializeField] private string m_search;

		private Stack<(ObjectModel, string, int)> m_entriesStack = new Stack<(ObjectModel, string, int)>();
		
		private HashSet<string> m_collapsedEntries = new HashSet<string>();
		private List<ObjectEntry> m_visibleEntries = new List<ObjectEntry>();

		private SearchField m_searchField;
		private bool m_updateVisibleEntries = true;

		private Color[] m_backgroundColors = new Color[2];
		private Color m_highlightColor;

		[MenuItem("Tools/Tekly/DataModels", false, 1)]
		private static void OpenWindow()
		{
			GetWindow<ModelsWindow>("Models");
		}

		private void OnEnable()
		{
			wantsMouseMove = true;
			var backgroundColor = EditorGuiExt.BackgroundColor;

			m_backgroundColors[0] = backgroundColor.RGBMultiplied(.75f);
			m_backgroundColors[1] = backgroundColor.RGBMultiplied(.68f);
			m_highlightColor = backgroundColor * new Color(0, .5f, .7f, 1f);

			m_updateVisibleEntries = true;
		}

		private void Update()
		{
			Repaint();
		}

		public void OnGUI()
		{
			if (Event.current.type == EventType.MouseMove) {
				Repaint();
			}

			if (m_searchField == null) {
				m_searchField = new SearchField();
			}

			// OnGUI is called multiple times per frame with different events but we only want to do this work once.
			if (Event.current.type == EventType.Layout) {
				if (Application.isPlaying && !EditorApplication.isPaused) {
					UpdateEntries();
				}
			}

			if (m_updateVisibleEntries) {
				UpdateVisibleEntries();
			}

			var width = position.width;
			var height = position.height;

			var totalHeight = m_visibleEntries.Count * s_height;
			var viewWidth = width;

			if (totalHeight > height) {
				viewWidth -= SCROLLBAR_WIDTH;
			}

			var oldSearch = m_search;
			m_search = m_searchField.OnGUI(new Rect(2, 2, width - 5, 20), m_search);
			m_updateVisibleEntries |= oldSearch != m_search;

			var scrollRect = new Rect(0, 22, width, height - 20);
			m_scrollPos = GUI.BeginScrollView(scrollRect, m_scrollPos, new Rect(0, 0, viewWidth, totalHeight));

			var start = Mathf.FloorToInt(m_scrollPos.y / s_height);
			var end = Mathf.Min(start + Mathf.CeilToInt(scrollRect.height / s_height), m_visibleEntries.Count);

			if (string.IsNullOrEmpty(m_search)) {
				for (var row = start; row < end; row++) {
					DrawEntry(m_visibleEntries[row], row, viewWidth);
				}
			} else {
				for (var row = start; row < end; row++) {
					DrawEntrySearch(row, viewWidth);
				}
			}

			GUI.EndScrollView();
		}

		private bool MatchesSearch(string[] search, string fullPath)
		{
			if (search.Length == 1) {
				return fullPath.Contains(search[0], StringComparison.OrdinalIgnoreCase);
			}

			foreach (var entry in search) {
				if (!fullPath.Contains(entry, StringComparison.OrdinalIgnoreCase)) {
					return false;
				}
			}

			return true;
		}

		private bool IsExpanded(ObjectEntry entry)
		{
			return !m_collapsedEntries.Contains(entry.FullPath);
		}

		private void SetExpanded(ObjectEntry entry, bool expanded)
		{
			if (expanded) {
				m_collapsedEntries.Remove(entry.FullPath);
			} else {
				m_collapsedEntries.Add(entry.FullPath);
			}

			m_updateVisibleEntries = true;
		}

		private void DrawEntry(ObjectEntry objectEntry, int row, float viewWidth)
		{
			viewWidth -= 4;
			var backgroundRect = new Rect(4, row * s_height, viewWidth, s_height);
			var backgroundColor = m_backgroundColors[row % m_backgroundColors.Length];

			if (backgroundRect.Contains(Event.current.mousePosition)) {
				backgroundColor = m_highlightColor;
			}

			EditorGUI.DrawRect(backgroundRect, backgroundColor);

			// Hierarchy lines
			var color = EditorStyles.label.normal.textColor;
			color.a = .3f;

			for (var i = 0; i <= objectEntry.Depth; i++) {
				var lineRect = backgroundRect;
				lineRect.xMin = (i - 1) * INDENT + 10;
				lineRect.width = 1;
				EditorGUI.DrawRect(lineRect, color);
			}

			var indent = objectEntry.Depth * INDENT + 4;
			var foldOutRect = new Rect(indent, row * s_height, viewWidth - indent, s_height);

			if (objectEntry.IsObject) {
				var isExpanded = IsExpanded(objectEntry);
				var expanded = EditorGUI.Foldout(foldOutRect, isExpanded, objectEntry.Id, true);
				EditorGUIUtility.AddCursorRect(foldOutRect, MouseCursor.Link);

				if (expanded != isExpanded) {
					SetExpanded(objectEntry, expanded);

					if (Event.current.alt && Event.current.shift) {
						SetAllExpanded(expanded);
					} else if (Event.current.alt) {
						SetExpanded(objectEntry.Index, objectEntry.Depth, expanded);
					}
				}
			} else {
				foldOutRect.xMin += 14f;
				EditorGUI.LabelField(foldOutRect, objectEntry.Id);
				var idWidth = EditorStyles.label.CalcSize(objectEntry.Id).x;
				var valueWidth = EditorStyles.label.CalcSize(objectEntry.Value).x;
				var valueRect = new Rect(Mathf.Max(foldOutRect.xMax - valueWidth, foldOutRect.x + idWidth + 14), row * s_height, valueWidth, s_height);
				EditorGUI.LabelField(valueRect, objectEntry.Value);
			}
		}

		private void DrawEntrySearch(int index, float viewWidth)
		{
			var objectEntry = m_visibleEntries[index];
			var labelRect = new Rect(0, index * s_height, viewWidth, s_height);

			EditorGUI.DrawRect(labelRect, new Color(0, 0, 0, .2f * ((1 + index % 2) / 2f)));
			EditorGUI.LabelField(labelRect, objectEntry.FullPath);

			if (objectEntry.Value != null) {
				var idWidth = GUI.skin.label.CalcSize(objectEntry.FullPathGui).x;
				var valueWidth = GUI.skin.label.CalcSize(objectEntry.Value).x;
				var valueRect = new Rect(Mathf.Max(labelRect.xMax - valueWidth, labelRect.x + idWidth + 14), index * s_height, valueWidth, s_height);
				EditorGUI.LabelField(valueRect, objectEntry.Value);
			}
		}

		private void SetExpanded(int index, int startDepth, bool expanded)
		{
			while (++index < m_entries.Count && m_entries[index].Depth > startDepth) {
				var entry = m_entries[index];
				if (entry.IsObject) {
					SetExpanded(entry, expanded);
				}
			}
		}

		private void SetAllExpanded(bool expanded)
		{
			for (var index = 0; index < m_entries.Count; index++) {
				var entry = m_entries[index];
				if (entry.IsObject) {
					SetExpanded(entry, expanded);
				}
			}
		}

		private void UpdateEntries()
		{
			for (var index = 0; index < m_entries.Count; index++) {
				var entry = m_entries[index];
				ObjectEntryPool.Return(entry);
			}

			m_entries.Clear();

			CreateEntries(RootModel.Instance, m_entries, null);

			for (var index = 0; index < m_entries.Count; index++) {
				m_entries[index].Index = index;
			}

			m_updateVisibleEntries = true;
		}

		private void CreateEntries(ObjectModel objectModel, List<ObjectEntry> entries, string parentKey, int depth = 0)
		{
			for (var index = 0; index < objectModel.Models.Count; index++) {
				var modelReference = objectModel.Models[index];
				var childModel = modelReference.Model;
				var fullPath = CombineKeys(parentKey, modelReference.Key);


				var entry = ObjectEntryPool.Get();
				entry.Id.text = modelReference.Key;
				entry.Id.tooltip = modelReference.TypeName;
				entry.FullPath = fullPath;
				entry.FullPathGui.text = fullPath;
				entry.Depth = depth;
				entry.IsObject = childModel is ObjectModel;

				entries.Add(entry);

				switch (childModel) {
					case IValueModel childValueModel:
						var displayValue = childValueModel.ToDisplayString();
						if (displayValue != null) {
							displayValue = displayValue.Replace("\n", "\\n");
						}

						entry.Value.text = displayValue;
						break;
					case ObjectModel childObjectModel:
						CreateEntries(childObjectModel, entries, fullPath, depth + 1);
						break;
					default:
						entry.Value = new GUIContent("UNKNOWN TYPE");
						break;
				}
			}
		}

		private void UpdateVisibleEntries()
		{
			m_visibleEntries.Clear();

			if (string.IsNullOrEmpty(m_search)) {
				for (var index = 0; index < m_entries.Count; index++) {
					var objectEntry = m_entries[index];
					m_visibleEntries.Add(objectEntry);

					if (objectEntry.IsObject && !IsExpanded(objectEntry)) {
						while (++index < m_entries.Count && m_entries[index].Depth > objectEntry.Depth) { }

						index--;
					}
				}
			} else {
				var search = m_search.Split(" ");
				Parallel.For(0, m_entries.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (index) => {
					var entry = m_entries[index];
					entry.Visible = MatchesSearch(search, entry.FullPath);
				});

				for (var index = 0; index < m_entries.Count; index++) {
					var entry = m_entries[index];
					if (entry.Visible) {
						m_visibleEntries.Add(entry);
					}
				}
			}

			m_updateVisibleEntries = false;
		}

		private string CombineKeys(string parent, string child)
		{
			if (string.IsNullOrEmpty(parent)) {
				return child;
			}

			return StringPool.Get(parent, child);
		}

		public void OnBeforeSerialize()
		{
			m_collapsedEntriesList.Clear();
			m_collapsedEntriesList.AddRange(m_collapsedEntries);
		}

		public void OnAfterDeserialize()
		{
			m_collapsedEntries.Clear();
			foreach (var entry in m_collapsedEntriesList) {
				m_collapsedEntries.Add(entry);
			}
		}
	}

	internal static class StringPool
	{
		private static readonly Dictionary<Hash128, string> s_strings = new Dictionary<Hash128, string>();

		public static string Get(string first, string second)
		{
			var hash = new Hash128();
			hash.Append(first);
			hash.Append(second);

			if (!s_strings.TryGetValue(hash, out var val)) {
				val = first + "." + second;
				s_strings.Add(hash, val);
			}

			return val;
		}
	}
}