using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Tekly.Common.Gui;
using Tekly.Common.Search;
using Tekly.Common.Utils;
using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Tekly.DataModels.Models
{
	/// <summary>
	/// Shows the RootModel tree in Play mode. Values that can be overridden are edited in place, which overrides
	/// them until the override is cleared. Outside Play mode it shows the models from the last Play session.
	/// </summary>
	public class ModelsWindow : EditorWindow, ISerializationCallbackReceiver, IHasCustomMenu
	{
		private const float INDENT = 18;
		private const float SCROLLBAR_WIDTH = 15;
		private const float TOOLBAR_HEIGHT = 20;
		private const float CLEAR_WIDTH = 18;
		private const float SYNC_INTERVAL = 0.1f;
		private const float OPTIONS_WIDTH = 60;
		private static readonly float s_height = EditorGUIUtility.singleLineHeight;

		[SerializeField] private List<ObjectEntry> m_entries = new List<ObjectEntry>();
		[SerializeField] private List<string> m_collapsedEntriesList = new List<string>();

		[SerializeField] private Vector2 m_scrollPos;
		[SerializeField] private string m_search;
		[SerializeField] private string m_selectedPath;

		private HashSet<string> m_collapsedEntries = new HashSet<string>();
		private List<ObjectEntry> m_visibleEntries = new List<ObjectEntry>();

		private SearchField m_searchField;
		private readonly SearchQuery m_searchQuery = new();
		private bool m_updateVisibleEntries = true;

		private Color[] m_backgroundColors = new Color[2];
		private Color m_highlightColor;
		private Color m_selectedColor;

		private Color m_lineColor = new Color(0.769f, 0.769f, 0.769f, 0.3f);
		private Color m_overrideRowColor = new Color(1f, 0.55f, 0f, 0.12f);
		private Color m_overrideMarkColor = new Color(1f, 0.6f, 0.1f, 1f);
		private float m_timeBetweenRenders = 0.25f;
		private float m_nextRenderTime;
		private float m_nextSyncTime;
		private bool m_forceSync;

		[NonSerialized] private int m_overrideCount = -1;
		[NonSerialized] private int m_shownOverrideCount = -1;
		private readonly GUIContent m_overrideCountContent = new GUIContent();
		private readonly GUIContent m_clearContent = new GUIContent("x", "Clear override");
		private readonly GUIContent m_optionsContent = new GUIContent("Options");
		private readonly GUIContent m_activateContent = new GUIContent("Activate", "Activates the button");
		private readonly GUIContent m_emitContent = new GUIContent("Emit", "Emits the trigger");

		private string m_scrollToPath;
		private float m_scrollViewHeight;
		private int m_firstDrawnRow;

		private readonly Option m_editValues = new Option("EditValues", "Edit Values", true);
		private readonly Option m_showTypes = new Option("ShowTypes", "Show Types", true);
		private readonly Option m_hideCommonTypes = new Option("HideCommonTypes", "Hide Common Types", false);

		private static GUIStyle s_typeStyle;
		private static GUIStyle s_messageStyle;

		private static readonly int s_floatFieldHash = "EditorTextField".GetHashCode();

		[MenuItem("Tools/Tekly/DataModels", false, 1)]
		private static void OpenWindow()
		{
			GetWindow<ModelsWindow>("Models");
		}

		private void OnEnable()
		{
			m_nextRenderTime = 0;
			m_forceSync = true;

			wantsMouseMove = true;
			var backgroundColor = EditorGuiExt.BackgroundColor;

			m_backgroundColors[0] = backgroundColor.RGBMultiplied(.75f);
			m_backgroundColors[1] = backgroundColor.RGBMultiplied(.68f);
			m_highlightColor = backgroundColor * new Color(0, .5f, .7f, 1f);
			m_selectedColor = EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.53f, 1f) : new Color(0.58f, 0.72f, 0.92f, 1f);

			m_updateVisibleEntries = true;

			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
		{
			if (stateChange == PlayModeStateChange.EnteredEditMode) {
				// Keep the last session's entries to look at, but let go of its models.
				foreach (var entry in m_entries) {
					entry.Model = null;
				}
			}

			m_forceSync = true;
			Repaint();
		}

		private void Update()
		{
			if (Application.isPlaying && Time.realtimeSinceStartup >= m_nextRenderTime) {
				m_nextRenderTime = Time.realtimeSinceStartup + m_timeBetweenRenders;
				Repaint();
			}
		}

		public void OnGUI()
		{
			InitStyles();

			if (Event.current.type == EventType.MouseMove) {
				Repaint();
			}

			if (m_searchField == null) {
				m_searchField = new SearchField();
			}

			// OnGUI is called multiple times per frame with different events but we only want to do this work once.
			if (Event.current.type == EventType.Layout) {
				if (Application.isPlaying) {
					SyncEntries();
				}

				if (m_updateVisibleEntries) {
					UpdateVisibleEntries();
				}

				if (m_scrollToPath != null) {
					ScrollToPath(m_scrollToPath);
					m_scrollToPath = null;
				}
			}

			var width = position.width;
			var height = position.height;

			DrawToolbar(new Rect(0, 0, width, TOOLBAR_HEIGHT));

			var contentRect = new Rect(0, TOOLBAR_HEIGHT, width, height - TOOLBAR_HEIGHT);

			if (DrawEmptyState(contentRect)) {
				return;
			}

			if (!Application.isPlaying) {
				var noticeRect = new Rect(4, contentRect.y, width - 8, s_height);
				GUI.Label(noticeRect, "Not in Play Mode. Showing the models from the last Play session.", EditorStyles.miniLabel);
				contentRect.yMin += s_height;
			}

			var totalHeight = m_visibleEntries.Count * s_height;
			var viewWidth = width;

			if (totalHeight > contentRect.height) {
				viewWidth -= SCROLLBAR_WIDTH;
			}

			m_scrollViewHeight = contentRect.height;
			m_scrollPos = GUI.BeginScrollView(contentRect, m_scrollPos, new Rect(0, 0, viewWidth, totalHeight));

			var start = Mathf.FloorToInt(m_scrollPos.y / s_height);
			var end = Mathf.Min(start + Mathf.CeilToInt(contentRect.height / s_height) + 1, m_visibleEntries.Count);

			// Rows are only drawn while visible so a field's control id moves to another row when scrolling.
			// Drop focus instead of letting the text being typed jump to a different model.
			if (start != m_firstDrawnRow) {
				m_firstDrawnRow = start;
				if (EditorGUIUtility.editingTextField) {
					GUIUtility.keyboardControl = 0;
				}
			}

			var isSearching = !string.IsNullOrEmpty(m_search);

			for (var row = start; row < end; row++) {
				if (isSearching) {
					DrawEntrySearch(m_visibleEntries[row], row, viewWidth);
				} else {
					DrawEntry(m_visibleEntries[row], row, viewWidth);
				}
			}

			GUI.EndScrollView();
		}

		private void InitStyles()
		{
			if (s_typeStyle != null) {
				return;
			}

			s_typeStyle = new GUIStyle(EditorStyles.miniLabel) {
				alignment = TextAnchor.MiddleRight
			};

			s_typeStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.55f, 0.55f, 0.55f) : new Color(0.4f, 0.4f, 0.4f);

			s_messageStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {
				fontSize = 12,
				wordWrap = true
			};
		}

		private void DrawToolbar(Rect toolbarRect)
		{
			if (Event.current.type == EventType.Repaint) {
				EditorStyles.toolbar.Draw(toolbarRect, false, false, false, false);
			}

			if (m_overrideCount < 0) {
				m_overrideCount = GetOverrideCount();
			}

			if (m_overrideCount != m_shownOverrideCount) {
				m_shownOverrideCount = m_overrideCount;
				m_overrideCountContent.text = $"Overrides: {m_overrideCount}";
			}

			var optionsRect = new Rect(toolbarRect.xMax - OPTIONS_WIDTH, toolbarRect.y, OPTIONS_WIDTH, toolbarRect.height);
			var clearAllRect = new Rect(optionsRect.x - 64, toolbarRect.y, 64, toolbarRect.height);
			var countWidth = EditorStyles.miniLabel.CalcSize(m_overrideCountContent).x + 8;
			var countRect = new Rect(clearAllRect.x - countWidth, toolbarRect.y + 2, countWidth, toolbarRect.height - 2);
			var searchRect = new Rect(toolbarRect.x + 4, toolbarRect.y + 2, countRect.x - toolbarRect.x - 8, toolbarRect.height - 2);

			var oldSearch = m_search;
			m_search = m_searchField.OnToolbarGUI(searchRect, m_search);
			m_updateVisibleEntries |= oldSearch != m_search;

			GUI.Label(countRect, m_overrideCountContent, EditorStyles.miniLabel);

			using (EditorGuiExt.EnabledBlock(Application.isPlaying && m_overrideCount > 0)) {
				if (GUI.Button(clearAllRect, "Clear All", EditorStyles.toolbarButton)) {
					ClearAllOverrides();
				}
			}

			if (EditorGUI.DropdownButton(optionsRect, m_optionsContent, FocusType.Passive, EditorStyles.toolbarDropDown)) {
				var menu = new GenericMenu();
				AddOptionItems(menu, string.Empty);
				menu.DropDown(optionsRect);
			}
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			AddOptionItems(menu, "Options/");
		}

		private void AddOptionItems(GenericMenu menu, string prefix)
		{
			AddOptionItem(menu, prefix, m_editValues);
			AddOptionItem(menu, prefix, m_showTypes);

			if (m_showTypes.Value) {
				AddOptionItem(menu, prefix, m_hideCommonTypes);
			} else {
				menu.AddDisabledItem(new GUIContent(prefix + m_hideCommonTypes.Label), m_hideCommonTypes.Value);
			}
		}

		private void AddOptionItem(GenericMenu menu, string prefix, Option option)
		{
			menu.AddItem(new GUIContent(prefix + option.Label), option.Value, () => {
				option.Value = !option.Value;
				GUIUtility.keyboardControl = 0;
				Repaint();
			});
		}

		/// <summary>
		/// Draws a message instead of the tree when there's nothing to show. Returns true if it did.
		/// </summary>
		private bool DrawEmptyState(Rect contentRect)
		{
			string message = null;

			if (Application.isPlaying && RootModel.Instance == null) {
				message = "There is no RootModel.\nIt's created when entering Play Mode.";
			} else if (m_entries.Count == 0) {
				message = Application.isPlaying ? "The RootModel has no models yet." : "Enter Play Mode to see the data models.";
			} else if (m_visibleEntries.Count == 0 && !string.IsNullOrEmpty(m_search)) {
				message = $"No models match \"{m_search}\".";
			}

			if (message == null) {
				return false;
			}

			contentRect.xMin += 8;
			contentRect.xMax -= 8;
			GUI.Label(contentRect, message, s_messageStyle);

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
			HandleRowEvents(objectEntry, backgroundRect, false);

			if (Event.current.type == EventType.Repaint) {
				DrawRowBackground(objectEntry, row, backgroundRect);

				// Hierarchy lines
				for (var i = 0; i <= objectEntry.Depth; i++) {
					var lineRect = backgroundRect;
					lineRect.xMin = (i - 1) * INDENT + 10;
					lineRect.width = 1;
					DrawRect(lineRect, m_lineColor);
				}
			}

			var indent = objectEntry.Depth * INDENT + 4;
			var foldOutRect = new Rect(indent, row * s_height, viewWidth - indent, s_height);
			var valueRect = GetValueRect(backgroundRect);
			var labelEnd = indent + 14 + EditorStyles.label.CalcSize(objectEntry.Id).x;

			if (objectEntry.IsObject) {
				// Drawn before the foldout so the button gets the click instead of the foldout
				var typeEnd = valueRect.xMax;
				if (DrawAction(objectEntry, valueRect)) {
					foldOutRect.xMax = valueRect.x - 4;
					typeEnd = valueRect.x - 6;
				}

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

				DrawType(objectEntry, backgroundRect.y, labelEnd, typeEnd);
			} else {
				foldOutRect.xMin += 14f;
				foldOutRect.xMax = valueRect.x - 4;
				LabelField(foldOutRect, objectEntry.Id);
				DrawType(objectEntry, backgroundRect.y, labelEnd, valueRect.x - 6);
				DrawValue(objectEntry, valueRect);
			}
		}

		private void DrawEntrySearch(ObjectEntry objectEntry, int row, float viewWidth)
		{
			viewWidth -= 4;

			var backgroundRect = new Rect(4, row * s_height, viewWidth, s_height);
			HandleRowEvents(objectEntry, backgroundRect, true);

			if (Event.current.type == EventType.Repaint) {
				DrawRowBackground(objectEntry, row, backgroundRect);
			}

			var valueRect = GetValueRect(backgroundRect);
			var pathRect = backgroundRect;
			pathRect.width = Mathf.Min(EditorStyles.label.CalcSize(objectEntry.FullPathGui).x, valueRect.x - 4 - pathRect.x);

			// Clicking the path shows the model in the tree
			EditorGUIUtility.AddCursorRect(pathRect, MouseCursor.Link);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && pathRect.Contains(Event.current.mousePosition)) {
				ShowInTree(objectEntry.FullPath);
				Event.current.Use();
			}

			LabelField(pathRect, objectEntry.FullPathGui);

			if (objectEntry.IsObject) {
				var typeEnd = DrawAction(objectEntry, valueRect) ? valueRect.x - 6 : valueRect.xMax;
				DrawType(objectEntry, backgroundRect.y, pathRect.xMax, typeEnd);
			} else {
				DrawType(objectEntry, backgroundRect.y, pathRect.xMax, valueRect.x - 6);
				DrawValue(objectEntry, valueRect);
			}
		}

		private void DrawRowBackground(ObjectEntry objectEntry, int row, Rect backgroundRect)
		{
			var backgroundColor = m_backgroundColors[row % m_backgroundColors.Length];

			if (objectEntry.FullPath == m_selectedPath) {
				backgroundColor = m_selectedColor;
			} else if (backgroundRect.Contains(Event.current.mousePosition)) {
				backgroundColor = m_highlightColor;
			}

			DrawRect(backgroundRect, backgroundColor);

			if (objectEntry.IsOverridden) {
				DrawRect(backgroundRect, m_overrideRowColor);
			}
		}

		/// <summary>
		/// The column values are drawn in. The clear override button sits to its right.
		/// </summary>
		private Rect GetValueRect(Rect rowRect)
		{
			var valueWidth = Mathf.Clamp(rowRect.width * 0.35f, 80, 320);
			return new Rect(rowRect.xMax - CLEAR_WIDTH - 2 - valueWidth, rowRect.y + 1, valueWidth, rowRect.height - 2);
		}

		/// <summary>
		/// Draws the model's type dimmed and right aligned to xMax, if there's room for it after labelEnd.
		/// </summary>
		private void DrawType(ObjectEntry objectEntry, float y, float labelEnd, float xMax)
		{
			if (Event.current.type != EventType.Repaint || !m_showTypes.Value) {
				return;
			}

			if (m_hideCommonTypes.Value && objectEntry.IsCommonType) {
				return;
			}

			var typeWidth = s_typeStyle.CalcSize(objectEntry.Type).x;
			var typeRect = new Rect(xMax - typeWidth, y, typeWidth, s_height);

			if (typeRect.x < labelEnd + 8) {
				return;
			}

			s_typeStyle.Draw(typeRect, objectEntry.Type, false, false, false, false);
		}

		/// <summary>
		/// Draws a button to activate ButtonModels and emit TriggerModels. Returns true if the model has one.
		/// </summary>
		private bool DrawAction(ObjectEntry objectEntry, Rect valueRect)
		{
			if (!Application.isPlaying) {
				return false;
			}

			var actionRect = valueRect;
			actionRect.width = Mathf.Min(80, valueRect.width);

			switch (objectEntry.Model) {
				case ButtonModel buttonModel:
					if (GUI.Button(actionRect, m_activateContent, EditorStyles.miniButton)) {
						ActivateButton(buttonModel);
					}

					return true;
				case TriggerModel triggerModel:
					if (GUI.Button(actionRect, m_emitContent, EditorStyles.miniButton)) {
						EmitTrigger(triggerModel);
					}

					return true;
				default:
					return false;
			}
		}

		private void ActivateButton(ButtonModel buttonModel)
		{
			buttonModel.Activate();
			m_forceSync = true;
			Repaint();
		}

		private void EmitTrigger(TriggerModel triggerModel)
		{
			triggerModel.Emit();
			m_forceSync = true;
			Repaint();
		}

		private void DrawValue(ObjectEntry objectEntry, Rect valueRect)
		{
			if (DrawAction(objectEntry, valueRect)) {
				return;
			}

			var model = Application.isPlaying ? objectEntry.Model : null;
			var editModel = m_editValues.Value ? model : null;

			switch (editModel) {
				case BoolValueModel boolModel: {
					EditorGUI.BeginChangeCheck();
					var value = EditorGUI.Toggle(valueRect, GUIContent.none, boolModel.Value);
					if (EditorGUI.EndChangeCheck()) {
						SetOverride(boolModel, value ? "true" : "false");
					}

					break;
				}
				case NumberValueModel numberModel: {
					EditorGUI.BeginChangeCheck();
					var value = EditorGUI.DelayedDoubleField(valueRect, GUIContent.none, numberModel.Value);
					if (EditorGUI.EndChangeCheck()) {
						SetOverride(numberModel, value.ToString("R"));
					}

					break;
				}
				case SpriteValueModel _:
					LabelField(valueRect, objectEntry.Value);
					break;
				case IOverridableValue overridableValue: {
					EditorGUI.BeginChangeCheck();
					var value = EditorGUI.DelayedTextField(valueRect, GUIContent.none, objectEntry.RawValue ?? string.Empty);
					if (EditorGUI.EndChangeCheck()) {
						SetOverride(overridableValue, value);
					}

					break;
				}
				default:
					LabelField(valueRect, objectEntry.Value);
					break;
			}

			if (!objectEntry.IsOverridden) {
				return;
			}

			DrawRect(new Rect(valueRect.x - 4, valueRect.y, 2, valueRect.height), m_overrideMarkColor);

			if (model is IOverridableValue overridden) {
				var clearRect = new Rect(valueRect.xMax + 2, valueRect.y, CLEAR_WIDTH, valueRect.height);
				if (GUI.Button(clearRect, m_clearContent, EditorStyles.miniButton)) {
					ClearOverride(overridden);
				}
			}
		}

		/// <summary>
		/// Selects the row on click and opens its context menu on right click.
		/// </summary>
		private void HandleRowEvents(ObjectEntry objectEntry, Rect rowRect, bool isSearching)
		{
			var evt = Event.current;
			if (!rowRect.Contains(evt.mousePosition)) {
				return;
			}

			if (evt.type == EventType.MouseDown && evt.button == 0) {
				m_selectedPath = objectEntry.FullPath;
				Repaint();
			} else if (evt.type == EventType.ContextClick) {
				m_selectedPath = objectEntry.FullPath;
				ShowContextMenu(objectEntry, isSearching);
				evt.Use();
			}
		}

		private void ShowContextMenu(ObjectEntry objectEntry, bool isSearching)
		{
			// Entries are pooled and may be reused before a menu item runs so only capture what the items need.
			var path = objectEntry.FullPath;
			var model = Application.isPlaying ? objectEntry.Model : null;
			var rawValue = objectEntry.RawValue;

			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = path);

			if (objectEntry.IsObject) {
				if (model is ObjectModel objectModel) {
					menu.AddItem(new GUIContent("Copy Value (JSON)"), false, () => {
						var sb = new StringBuilder();
						objectModel.ToJson(sb);
						EditorGUIUtility.systemCopyBuffer = sb.ToString();
					});
				} else {
					menu.AddDisabledItem(new GUIContent("Copy Value (JSON)"));
				}
			} else {
				menu.AddItem(new GUIContent("Copy Value"), false, () => EditorGUIUtility.systemCopyBuffer = rawValue ?? string.Empty);
			}

			switch (model) {
				case ButtonModel buttonModel:
					menu.AddItem(new GUIContent("Activate"), false, () => ActivateButton(buttonModel));
					break;
				case TriggerModel triggerModel:
					menu.AddItem(new GUIContent("Emit"), false, () => EmitTrigger(triggerModel));
					break;
			}

			if (model is IOverridableValue overridableValue && overridableValue.OverrideValue) {
				menu.AddItem(new GUIContent("Clear Override"), false, () => ClearOverride(overridableValue));
			} else {
				menu.AddDisabledItem(new GUIContent("Clear Override"));
			}

			if (isSearching) {
				menu.AddSeparator(string.Empty);
				menu.AddItem(new GUIContent("Show in Tree"), false, () => ShowInTree(path));
			} else if (objectEntry.IsObject) {
				menu.AddSeparator(string.Empty);
				menu.AddItem(new GUIContent("Expand Children"), false, () => SetChildrenExpanded(path, true));
				menu.AddItem(new GUIContent("Collapse Children"), false, () => SetChildrenExpanded(path, false));
			}

			menu.ShowAsContext();
		}

		private void SetOverride(IOverridableValue overridableValue, string value)
		{
			overridableValue.SetOverrideValue(value);
			m_forceSync = true;
			Repaint();
		}

		private void ClearOverride(IOverridableValue overridableValue)
		{
			overridableValue.OverrideValue = false;
			m_forceSync = true;
			Repaint();
		}

		private void ClearAllOverrides()
		{
			foreach (var entry in m_entries) {
				if (entry.Model is IOverridableValue overridableValue && overridableValue.OverrideValue) {
					overridableValue.OverrideValue = false;
				}
			}

			GUIUtility.keyboardControl = 0;
			m_forceSync = true;
			Repaint();
		}

		private int GetOverrideCount()
		{
			var count = 0;
			foreach (var entry in m_entries) {
				if (entry.IsOverridden) {
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// Leaves search, expands the entry's parents and scrolls to and selects it.
		/// </summary>
		private void ShowInTree(string path)
		{
			var entry = FindEntry(path);
			if (entry == null) {
				return;
			}

			var depth = entry.Depth;
			for (var index = entry.Index - 1; index >= 0 && depth > 0; index--) {
				var parent = m_entries[index];
				if (parent.Depth < depth) {
					m_collapsedEntries.Remove(parent.FullPath);
					depth = parent.Depth;
				}
			}

			GUIUtility.keyboardControl = 0;
			m_search = string.Empty;
			m_selectedPath = path;
			m_scrollToPath = path;
			m_updateVisibleEntries = true;
			Repaint();
		}

		private void ScrollToPath(string path)
		{
			for (var row = 0; row < m_visibleEntries.Count; row++) {
				if (m_visibleEntries[row].FullPath == path) {
					m_scrollPos.y = Mathf.Max(0, row * s_height - (m_scrollViewHeight - s_height) * 0.5f);
					return;
				}
			}
		}

		private void SetChildrenExpanded(string path, bool expanded)
		{
			var entry = FindEntry(path);
			if (entry == null) {
				return;
			}

			if (expanded) {
				SetExpanded(entry, true);
			}

			SetExpanded(entry.Index, entry.Depth, expanded);
		}

		private ObjectEntry FindEntry(string path)
		{
			foreach (var entry in m_entries) {
				if (entry.FullPath == path) {
					return entry;
				}
			}

			return null;
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

		/// <summary>
		/// Updates the entries' values in place. The entries are only rebuilt when the structure of the models changed.
		/// </summary>
		private void SyncEntries()
		{
			if (!m_forceSync && Time.realtimeSinceStartup < m_nextSyncTime) {
				return;
			}

			m_forceSync = false;
			m_nextSyncTime = Time.realtimeSinceStartup + SYNC_INTERVAL;

			var rootModel = RootModel.Instance;

			if (rootModel == null) {
				if (m_entries.Count > 0) {
					ReturnEntries();
					m_overrideCount = 0;
					m_updateVisibleEntries = true;
				}

				return;
			}

			var cursor = 0;
			if (!UpdateEntries(rootModel, 0, ref cursor) || cursor != m_entries.Count) {
				RebuildEntries(rootModel);
			}

			m_overrideCount = GetOverrideCount();
		}

		/// <summary>
		/// Walks the models alongside the existing entries updating their values. Returns false as soon as a model
		/// doesn't line up with its entry, meaning the entries need to be rebuilt.
		/// </summary>
		private bool UpdateEntries(ObjectModel objectModel, int depth, ref int cursor)
		{
			var models = objectModel.Models;

			for (var index = 0; index < models.Count; index++) {
				var modelReference = models[index];

				if (cursor >= m_entries.Count) {
					return false;
				}

				var entry = m_entries[cursor];
				if (!ReferenceEquals(entry.Model, modelReference.Model) || entry.Depth != depth
				    || !string.Equals(entry.Id.text, modelReference.Key, StringComparison.Ordinal)) {
					return false;
				}

				cursor++;

				switch (modelReference.Model) {
					case IValueModel valueModel:
						UpdateValue(entry, valueModel);
						break;
					case ObjectModel childObjectModel:
						if (!UpdateEntries(childObjectModel, depth + 1, ref cursor)) {
							return false;
						}

						break;
				}
			}

			return true;
		}

		private void RebuildEntries(ObjectModel rootModel)
		{
			ReturnEntries();

			CreateEntries(rootModel, m_entries, null);

			for (var index = 0; index < m_entries.Count; index++) {
				m_entries[index].Index = index;
			}

			m_updateVisibleEntries = true;
		}

		private void ReturnEntries()
		{
			for (var index = 0; index < m_entries.Count; index++) {
				ObjectEntryPool.Return(m_entries[index]);
			}

			m_entries.Clear();
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
				entry.Type.text = modelReference.TypeName;
				entry.FullPath = fullPath;
				entry.FullPathGui.text = fullPath;
				entry.FullPathGui.tooltip = "Click to show in the tree";
				entry.Depth = depth;
				entry.IsObject = childModel is ObjectModel;
				entry.IsCommonType = IsCommonType(childModel);
				entry.Model = childModel;

				entries.Add(entry);

				switch (childModel) {
					case IValueModel childValueModel:
						UpdateValue(entry, childValueModel);
						break;
					case ObjectModel childObjectModel:
						CreateEntries(childObjectModel, entries, fullPath, depth + 1);
						break;
				}
			}
		}

		/// <summary>
		/// Types that are obvious from the value or the foldout so showing them is mostly noise.
		/// </summary>
		private static bool IsCommonType(IModel model)
		{
			return model is StringValueModel || model is NumberValueModel || model is BoolValueModel
			       || model?.GetType() == typeof(ObjectModel);
		}

		private static void UpdateValue(ObjectEntry entry, IValueModel valueModel)
		{
			var displayValue = valueModel.ToDisplayString();

			// ValueModels cache their display string so this only builds a new string when the value changed.
			if (!ReferenceEquals(displayValue, entry.RawValue)) {
				entry.RawValue = displayValue;
				entry.Value.text = displayValue?.Replace("\n", "\\n");
			}

			entry.IsOverridden = valueModel is IOverridableValue overridableValue && overridableValue.OverrideValue;
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
				m_searchQuery.Set(m_search);
				Parallel.For(0, m_entries.Count,
					new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (index) => {
						var entry = m_entries[index];
						entry.Visible = m_searchQuery.Matches(entry.FullPath);
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

		private void DrawRect(Rect rectPosition, Color color)
		{
			if (Event.current.type != EventType.Repaint) {
				return;
			}

			var texture = (Texture)EditorGUIUtility.whiteTexture;
			var zero = Vector4.zero;
			GUI.DrawTexture(rectPosition, texture, ScaleMode.StretchToFill, true, 0, color, zero, zero);
		}

		private void LabelField(Rect labelPosition, GUIContent label)
		{
			var controlId = GUIUtility.GetControlID(s_floatFieldHash, FocusType.Passive, labelPosition);

			if (Event.current.type != EventType.Repaint) {
				return;
			}

			EditorStyles.label.Draw(labelPosition, label, controlId);
		}
	}

	/// <summary>
	/// A ModelsWindow toggle that is saved in EditorPrefs. The value is loaded on first use as EditorPrefs can't be
	/// read while the window is being constructed.
	/// </summary>
	internal class Option
	{
		public readonly string Label;

		public bool Value {
			get {
				if (!m_loaded) {
					m_value = EditorPrefs.GetBool(m_key, m_defaultValue);
					m_loaded = true;
				}

				return m_value;
			}
			set {
				m_value = value;
				m_loaded = true;
				EditorPrefs.SetBool(m_key, value);
			}
		}

		private readonly string m_key;
		private readonly bool m_defaultValue;
		private bool m_value;
		private bool m_loaded;

		public Option(string key, string label, bool defaultValue)
		{
			m_key = "Tekly.DataModels.ModelsWindow." + key;
			Label = label;
			m_defaultValue = defaultValue;
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
