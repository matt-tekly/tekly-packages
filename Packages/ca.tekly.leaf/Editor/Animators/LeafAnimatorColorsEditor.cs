using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements.Animators
{
	[CustomEditor(typeof(LeafAnimatorColors))]
	public class LeafAnimatorColorsEditor : Editor
	{
		private const float SPACING = 4f;
		private const float BLEND_WIDTH = 70f;
		private const float MENU_WIDTH = 18f;
		private const float SWATCH_WIDTH = 34f;
		private const float ROW_PADDING = 2f;

		private static readonly GUIContent[] s_modeLabels = {
			new GUIContent("Normal"), new GUIContent("Highlighted"), new GUIContent("Pressed")
		};
		private static readonly string[] s_colorFields = { "Normal", "Highlighted", "Pressed" };
		private static readonly LeafElementFlags[] s_flagOrder = {
			LeafElementFlags.Selected, LeafElementFlags.On, LeafElementFlags.Disabled
		};

		private static readonly Color s_rowTint = new Color(0.98f, 0.74f, 0.18f, 0.14f);
		private static readonly Color s_cellOutline = new Color(0.98f, 0.74f, 0.18f, 0.95f);
		private static readonly Color s_rowDivider = new Color(0.5f, 0.5f, 0.5f, 0.18f);
		private static readonly Color s_hiddenDot = new Color(0.5f, 0.5f, 0.5f, 0.6f);

		private static GUIStyle s_menuButton;

		// Shared between inspectors so the chosen state stays put while clicking between elements
		private static bool s_previewInScene;
		private static LeafElementMode s_previewMode;
		private static LeafElementFlags s_previewFlags;

		private SerializedProperty m_targets;
		private SerializedProperty m_activeTargets;

		private LeafAnimatorColors ColorsAnimator => (LeafAnimatorColors) target;

		private static GUIStyle MenuButton => s_menuButton ??= new GUIStyle(EditorStyles.label) {
			alignment = TextAnchor.MiddleCenter,
			padding = new RectOffset(0, 0, 0, 0),
			fontStyle = FontStyle.Bold
		};

		private void OnEnable()
		{
			m_targets = serializedObject.FindProperty("m_targets");
			m_activeTargets = serializedObject.FindProperty("m_activeTargets");

			if (s_previewInScene && !Application.isPlaying) {
				ColorsAnimator.EditorSetPreview(PreviewState);
			}
		}

		private void OnDisable()
		{
			if (target != null && ColorsAnimator.EditorHasPreview) {
				ColorsAnimator.EditorClearPreview();
			}
		}

		public override bool RequiresConstantRepaint() => Application.isPlaying;

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawPreviewBar();
			DrawWarnings();

			EditorGUILayout.Space(6f);
			for (var i = 0; i < m_targets.arraySize; i++) {
				if (DrawTarget(i)) {
					// The array changed mid-draw; apply and restart the GUI so layout stays consistent
					serializedObject.ApplyModifiedProperties();
					GUIUtility.ExitGUI();
				}
			}

			DrawAddTargetButton();

			EditorGUILayout.Space(10f);
			DrawActiveTargets();

			var changed = serializedObject.ApplyModifiedProperties();

			if (changed && s_previewInScene && !Application.isPlaying) {
				ColorsAnimator.EditorSetPreview(PreviewState);
			}
		}

		// ---- Preview ----

		private LeafElementState PreviewState {
			get {
				if (Application.isPlaying) {
					return ColorsAnimator.CurrentState;
				}

				// Disabled elements always report Normal
				var mode = (s_previewFlags & LeafElementFlags.Disabled) != 0 ? LeafElementMode.Normal : s_previewMode;
				return new LeafElementState(mode, s_previewFlags);
			}
		}

		private void DrawPreviewBar()
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
				if (Application.isPlaying) {
					EditorGUILayout.LabelField("Live state", PreviewState.ToString(), EditorStyles.boldLabel);
					return;
				}

				EditorGUI.BeginChangeCheck();

				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.LabelField(new GUIContent("State", "Pick a state to see which rows apply. Highlighted rows and outlined colors are what the element would show."), GUILayout.Width(40f));
					s_previewMode = (LeafElementMode) GUILayout.Toolbar((int) s_previewMode, s_modeLabels, EditorStyles.miniButton);
				}

				using (new EditorGUILayout.HorizontalScope()) {
					GUILayout.Space(44f);
					for (var i = 0; i < s_flagOrder.Length; i++) {
						var flag = s_flagOrder[i];
						var style = i == 0 ? EditorStyles.miniButtonLeft : i == s_flagOrder.Length - 1 ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid;
						var isSet = (s_previewFlags & flag) != 0;
						if (GUILayout.Toggle(isSet, flag.ToString(), style) != isSet) {
							s_previewFlags ^= flag;
						}
					}
				}

				using (new EditorGUILayout.HorizontalScope()) {
					GUILayout.Space(44f);
					s_previewInScene = GUILayout.Toggle(s_previewInScene, new GUIContent(" Preview in Scene", "Tint the graphics with this state. Only colors change; nothing is saved."), GUILayout.ExpandWidth(false));
					if ((s_previewFlags & LeafElementFlags.Disabled) != 0) {
						GUILayout.Label("Disabled always uses Normal", EditorStyles.miniLabel);
					}
				}

				if (EditorGUI.EndChangeCheck()) {
					if (s_previewInScene) {
						ColorsAnimator.EditorSetPreview(PreviewState);
					} else {
						ColorsAnimator.EditorClearPreview();
					}
				}
			}
		}

		// ---- Warnings ----

		private void DrawWarnings()
		{
			var selectable = ColorsAnimator.GetComponent<Selectable>();
			if (selectable != null && selectable.transition != Selectable.Transition.None) {
				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.HelpBox($"{selectable.GetType().Name} Transition is {selectable.transition}. It can fight this animator and flash white when disabled.", MessageType.Warning);
					if (GUILayout.Button("Set to None", GUILayout.Width(90f), GUILayout.Height(38f))) {
						Undo.RecordObject(selectable, "Set Transition to None");
						selectable.transition = Selectable.Transition.None;
						EditorUtility.SetDirty(selectable);
					}
				}
			}

			var seen = new HashSet<Object>();
			for (var i = 0; i < m_targets.arraySize; i++) {
				var graphic = m_targets.GetArrayElementAtIndex(i).FindPropertyRelative("m_target").objectReferenceValue;
				if (graphic != null && !seen.Add(graphic)) {
					EditorGUILayout.HelpBox($"{graphic.name} is driven by more than one target. Only the last one will be visible; merge them into one target with layers.", MessageType.Warning);
				}
			}

			// Toggle fades its own Graphic (the checkmark) in and out, which fights a color target on it
			if (selectable is Toggle toggle && toggle.graphic != null && seen.Contains(toggle.graphic)) {
				using (new EditorGUILayout.HorizontalScope()) {
					EditorGUILayout.HelpBox($"{toggle.graphic.name} is the Toggle's Graphic and also a color target. The Toggle fades it on its own, so the two will fight. Clear the Toggle's Graphic and show the checkmark with an Active Target (On) instead.", MessageType.Warning);
					if (GUILayout.Button("Clear Toggle\nGraphic", GUILayout.Width(90f), GUILayout.Height(52f))) {
						Undo.RecordObject(toggle, "Clear Toggle Graphic");
						toggle.graphic = null;
						EditorUtility.SetDirty(toggle);
					}
				}
			}
		}

		// ---- Targets ----

		/// <summary>
		/// Returns true if the targets array changed and the GUI has to restart.
		/// </summary>
		private bool DrawTarget(int index)
		{
			var element = m_targets.GetArrayElementAtIndex(index);
			var graphic = element.FindPropertyRelative("m_target");
			var colors = element.FindPropertyRelative("m_colors");
			var layers = element.FindPropertyRelative("m_layers");
			var fade = element.FindPropertyRelative("m_fadeDuration");
			var state = PreviewState;

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
				var header = EditorGUILayout.GetControlRect();
				var foldRect = new Rect(header.x + 2f, header.y, 16f, header.height);
				var buttonsWidth = 3 * 22f;
				var swatchRect = new Rect(header.xMax - buttonsWidth - SWATCH_WIDTH - SPACING, header.y + 1f, SWATCH_WIDTH, header.height - 2f);
				var fieldRect = new Rect(foldRect.xMax + 2f, header.y, swatchRect.x - foldRect.xMax - SPACING - 2f, header.height);

				// GUI.Toggle rather than EditorGUI.Foldout: in the Inspector, Foldout shifts its arrow left
				// into the margin, which the help box clips
				element.isExpanded = GUI.Toggle(foldRect, element.isExpanded, GUIContent.none, EditorStyles.foldout);
				EditorGUI.PropertyField(fieldRect, graphic, GUIContent.none);

				EditorGUI.DrawRect(swatchRect, Evaluate(colors, layers, state));
				GUI.Label(swatchRect, new GUIContent(string.Empty, "Color for the previewed state"));

				var buttonRect = new Rect(swatchRect.xMax + SPACING, header.y, 20f, header.height);
				using (new EditorGUI.DisabledScope(index == 0)) {
					if (GUI.Button(buttonRect, new GUIContent("▲", "Move target up"), EditorStyles.miniButtonLeft)) {
						m_targets.MoveArrayElement(index, index - 1);
						return true;
					}
				}

				buttonRect.x += 21f;
				using (new EditorGUI.DisabledScope(index == m_targets.arraySize - 1)) {
					if (GUI.Button(buttonRect, new GUIContent("▼", "Move target down"), EditorStyles.miniButtonMid)) {
						m_targets.MoveArrayElement(index, index + 1);
						return true;
					}
				}

				buttonRect.x += 21f;
				if (GUI.Button(buttonRect, new GUIContent("✕", "Remove target"), EditorStyles.miniButtonRight)) {
					m_targets.DeleteArrayElementAtIndex(index);
					return true;
				}

				if (!element.isExpanded) {
					return false;
				}

				if (graphic.objectReferenceValue == null) {
					EditorGUILayout.HelpBox("Assign the Graphic this target tints.", MessageType.Info);
				}

				EditorGUILayout.PropertyField(fade);
				EditorGUILayout.Space(4f);
				DrawColorTable(colors, layers, state);
			}

			EditorGUILayout.Space(2f);
			return false;
		}

		private void DrawColorTable(SerializedProperty colors, SerializedProperty layers, LeafElementState state)
		{
			var winner = WinningLayer(layers, state);

			// Column headers
			var headerRect = EditorGUILayout.GetControlRect();
			Columns(headerRect, out var flagsRect, out var blendRect, out var cells, out _);
			EditorGUI.LabelField(flagsRect, new GUIContent("Applies when", "A layer applies while all of its flags are set. Layers apply top to bottom; the last match wins."), EditorStyles.miniBoldLabel);
			EditorGUI.LabelField(blendRect, "Blend", EditorStyles.miniBoldLabel);
			for (var i = 0; i < 3; i++) {
				EditorGUI.LabelField(cells[i], s_modeLabels[i], EditorStyles.miniBoldLabel);
			}

			// Base colors
			var baseRect = RowRect(winner < 0);
			Columns(baseRect, out flagsRect, out blendRect, out cells, out _);
			EditorGUI.LabelField(new Rect(flagsRect.x, flagsRect.y, blendRect.xMax - flagsRect.x, flagsRect.height), new GUIContent("Base colors", "Used when no layer matches"));
			DrawColorCells(cells, colors, winner < 0 ? (int) state.Mode : -1);

			// Layers
			for (var i = 0; i < layers.arraySize; i++) {
				DrawLayerRow(layers, i, i == winner ? (int) state.Mode : -1);
			}

			// Add button, aligned with the flags column
			var addRect = EditorGUILayout.GetControlRect();
			Columns(addRect, out flagsRect, out _, out _, out _);
			if (EditorGUI.DropdownButton(flagsRect, new GUIContent("+ Add Layer", "Add colors for a combination of flags"), FocusType.Keyboard, EditorStyles.miniPullDown)) {
				ShowAddLayerMenu(flagsRect, colors, layers);
			}
		}

		private void DrawLayerRow(SerializedProperty layers, int index, int outlinedCell)
		{
			var layer = layers.GetArrayElementAtIndex(index);
			var rect = RowRect(outlinedCell >= 0);
			Columns(rect, out var flagsRect, out var blendRect, out var cells, out var menuRect);

			FlagsDropdown(flagsRect, layer.FindPropertyRelative("m_flags"));
			EditorGUI.PropertyField(blendRect, layer.FindPropertyRelative("m_blend"), GUIContent.none);
			DrawColorCells(cells, layer.FindPropertyRelative("m_colors"), outlinedCell);

			var clickedMenu = GUI.Button(menuRect, new GUIContent("⋮", "Layer options"), MenuButton);
			var rightClicked = Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition);
			if (clickedMenu || rightClicked) {
				ShowRowMenu(layers, index, "layer");
				if (rightClicked) {
					Event.current.Use();
				}
			}
		}

		/// <summary>
		/// Reserves a padded row, tinted when it's the row in effect for the previewed state.
		/// </summary>
		private static Rect RowRect(bool highlighted)
		{
			var outer = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + ROW_PADDING * 2f);
			if (Event.current.type == EventType.Repaint) {
				if (highlighted) {
					EditorGUI.DrawRect(new Rect(outer.x - 2f, outer.y, outer.width + 4f, outer.height), s_rowTint);
				}

				EditorGUI.DrawRect(new Rect(outer.x, outer.y, outer.width, 1f), s_rowDivider);
			}

			return new Rect(outer.x, outer.y + ROW_PADDING, outer.width, EditorGUIUtility.singleLineHeight);
		}

		private static void DrawColorCells(Rect[] cells, SerializedProperty block, int outlinedIndex)
		{
			for (var i = 0; i < 3; i++) {
				var color = block.FindPropertyRelative(s_colorFields[i]);
				EditorGUI.BeginChangeCheck();
				var value = EditorGUI.ColorField(cells[i], GUIContent.none, color.colorValue, true, true, false);
				if (EditorGUI.EndChangeCheck()) {
					color.colorValue = value;
				}

				if (i == outlinedIndex && Event.current.type == EventType.Repaint) {
					DrawOutline(cells[i], s_cellOutline);
				}
			}
		}

		private static void Columns(Rect rect, out Rect flags, out Rect blend, out Rect[] cells, out Rect menu)
		{
			menu = new Rect(rect.xMax - MENU_WIDTH, rect.y, MENU_WIDTH, rect.height);

			var flagsWidth = Mathf.Clamp(rect.width * 0.28f, 90f, 170f);
			flags = new Rect(rect.x, rect.y, flagsWidth, rect.height);
			blend = new Rect(flags.xMax + SPACING, rect.y, BLEND_WIDTH, rect.height);

			var x = blend.xMax + SPACING;
			var cellWidth = (menu.x - SPACING - x - SPACING * 2f) / 3f;
			cells = new Rect[3];
			for (var i = 0; i < 3; i++) {
				cells[i] = new Rect(x + i * (cellWidth + SPACING), rect.y, cellWidth, rect.height);
			}
		}

		private static void DrawOutline(Rect rect, Color color)
		{
			EditorGUI.DrawRect(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, 1f), color);
			EditorGUI.DrawRect(new Rect(rect.x - 1f, rect.yMax, rect.width + 2f, 1f), color);
			EditorGUI.DrawRect(new Rect(rect.x - 1f, rect.y, 1f, rect.height), color);
			EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, 1f, rect.height), color);
		}

		/// <summary>
		/// Move / duplicate / remove menu for a row of an array. Runs deferred, outside the draw loop.
		/// </summary>
		private static void ShowRowMenu(SerializedProperty array, int index, string noun)
		{
			var serialized = array.serializedObject;
			var path = array.propertyPath;
			var count = array.arraySize;
			var menu = new GenericMenu();

			void Edit(System.Action<SerializedProperty> change)
			{
				serialized.Update();
				change(serialized.FindProperty(path));
				serialized.ApplyModifiedProperties();
			}

			if (index > 0) {
				menu.AddItem(new GUIContent("Move Up"), false, () => Edit(a => a.MoveArrayElement(index, index - 1)));
			} else {
				menu.AddDisabledItem(new GUIContent("Move Up"));
			}

			if (index < count - 1) {
				menu.AddItem(new GUIContent("Move Down"), false, () => Edit(a => a.MoveArrayElement(index, index + 1)));
			} else {
				menu.AddDisabledItem(new GUIContent("Move Down"));
			}

			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent("Duplicate"), false, () => Edit(a => a.InsertArrayElementAtIndex(index)));
			menu.AddItem(new GUIContent($"Remove {noun}"), false, () => Edit(a => a.DeleteArrayElementAtIndex(index)));
			menu.ShowAsContext();
		}

		// ---- Flags ----

		private static string FlagsLabel(LeafElementFlags flags)
		{
			if (flags == LeafElementFlags.None) {
				return "Always";
			}

			var parts = new List<string>(3);
			for (var i = 0; i < s_flagOrder.Length; i++) {
				if ((flags & s_flagOrder[i]) != 0) {
					parts.Add(s_flagOrder[i].ToString());
				}
			}

			return string.Join(" + ", parts);
		}

		private static void FlagsDropdown(Rect rect, SerializedProperty flags)
		{
			var value = (LeafElementFlags) flags.intValue;
			if (!EditorGUI.DropdownButton(rect, new GUIContent(FlagsLabel(value), "Applies while all of these are set"), FocusType.Keyboard)) {
				return;
			}

			var serialized = flags.serializedObject;
			var path = flags.propertyPath;
			var menu = new GenericMenu();

			for (var i = 0; i < s_flagOrder.Length; i++) {
				var flag = s_flagOrder[i];
				menu.AddItem(new GUIContent(flag.ToString()), (value & flag) != 0, () => {
					serialized.Update();
					serialized.FindProperty(path).intValue ^= (int) flag;
					serialized.ApplyModifiedProperties();
				});
			}

			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent("Clear (always applies)"), value == LeafElementFlags.None, () => {
				serialized.Update();
				serialized.FindProperty(path).intValue = 0;
				serialized.ApplyModifiedProperties();
			});

			menu.DropDown(rect);
		}

		private static void ShowAddLayerMenu(Rect rect, SerializedProperty colors, SerializedProperty layers)
		{
			var serialized = layers.serializedObject;
			var layersPath = layers.propertyPath;
			var colorsPath = colors.propertyPath;
			var menu = new GenericMenu();

			void Preset(string label, LeafElementFlags flags)
			{
				menu.AddItem(new GUIContent(label), false, () => AddLayer(serialized, layersPath, colorsPath, flags));
			}

			Preset("Selected", LeafElementFlags.Selected);
			Preset("On", LeafElementFlags.On);
			Preset("Selected + On", LeafElementFlags.Selected | LeafElementFlags.On);
			menu.AddSeparator(string.Empty);
			Preset("Disabled", LeafElementFlags.Disabled);
			Preset("Disabled + On", LeafElementFlags.Disabled | LeafElementFlags.On);
			Preset("Disabled + Selected", LeafElementFlags.Disabled | LeafElementFlags.Selected);
			menu.AddSeparator(string.Empty);
			Preset("Always (no flags)", LeafElementFlags.None);

			menu.DropDown(rect);
		}

		private static void AddLayer(SerializedObject serialized, string layersPath, string colorsPath, LeafElementFlags flags)
		{
			serialized.Update();
			var layers = serialized.FindProperty(layersPath);
			var baseColors = serialized.FindProperty(colorsPath);

			// Keep Disabled layers last so they win: insert other layers before the first Disabled one
			var insertAt = layers.arraySize;
			if ((flags & LeafElementFlags.Disabled) == 0) {
				for (var i = 0; i < layers.arraySize; i++) {
					var existing = (LeafElementFlags) layers.GetArrayElementAtIndex(i).FindPropertyRelative("m_flags").intValue;
					if ((existing & LeafElementFlags.Disabled) != 0) {
						insertAt = i;
						break;
					}
				}
			}

			if (insertAt == layers.arraySize) {
				layers.arraySize++;
			} else {
				layers.InsertArrayElementAtIndex(insertAt);
			}

			var layer = layers.GetArrayElementAtIndex(insertAt);
			layer.FindPropertyRelative("m_flags").intValue = (int) flags;
			layer.FindPropertyRelative("m_blend").enumValueIndex = (int) LeafColorBlend.Override;

			// Start from the base colors so the new layer is a tweak, not a blank slate
			var layerColors = layer.FindPropertyRelative("m_colors");
			for (var i = 0; i < s_colorFields.Length; i++) {
				layerColors.FindPropertyRelative(s_colorFields[i]).colorValue = baseColors.FindPropertyRelative(s_colorFields[i]).colorValue;
			}

			serialized.ApplyModifiedProperties();
		}

		// ---- Adding targets ----

		private void DrawAddTargetButton()
		{
			var rect = EditorGUILayout.GetControlRect();
			if (!EditorGUI.DropdownButton(rect, new GUIContent("+ Add Color Target"), FocusType.Keyboard)) {
				return;
			}

			var used = new HashSet<Object>();
			for (var i = 0; i < m_targets.arraySize; i++) {
				used.Add(m_targets.GetArrayElementAtIndex(i).FindPropertyRelative("m_target").objectReferenceValue);
			}

			var menu = new GenericMenu();
			var graphics = ColorsAnimator.GetComponentsInChildren<Graphic>(true);
			for (var i = 0; i < graphics.Length; i++) {
				var graphic = graphics[i];
				if (used.Contains(graphic)) {
					continue;
				}

				var path = AnimationUtility.CalculateTransformPath(graphic.transform, ColorsAnimator.transform);
				var label = string.IsNullOrEmpty(path) ? graphic.name : path;
				menu.AddItem(new GUIContent($"{label.Replace('/', '∕')} ({graphic.GetType().Name})"), false, () => AddTarget(graphic));
			}

			if (menu.GetItemCount() > 0) {
				menu.AddSeparator(string.Empty);
			}

			menu.AddItem(new GUIContent("Empty target"), false, () => AddTarget(null));
			menu.DropDown(rect);
		}

		private void AddTarget(Graphic graphic)
		{
			serializedObject.Update();
			var index = m_targets.arraySize;
			m_targets.arraySize++;

			var element = m_targets.GetArrayElementAtIndex(index);
			element.FindPropertyRelative("m_target").objectReferenceValue = graphic;
			element.FindPropertyRelative("m_fadeDuration").floatValue = 0.1f;
			element.FindPropertyRelative("m_layers").arraySize = 0;

			var colors = element.FindPropertyRelative("m_colors");
			colors.FindPropertyRelative("Normal").colorValue = Color.white;
			colors.FindPropertyRelative("Highlighted").colorValue = new Color32(245, 245, 245, 255);
			colors.FindPropertyRelative("Pressed").colorValue = new Color32(200, 200, 200, 255);
			element.isExpanded = true;

			serializedObject.ApplyModifiedProperties();
		}

		// ---- Active targets ----

		private void DrawActiveTargets()
		{
			EditorGUILayout.LabelField(new GUIContent("Active Targets", "GameObjects shown only while all of their flags are set"), EditorStyles.boldLabel);

			var state = PreviewState;
			for (var i = 0; i < m_activeTargets.arraySize; i++) {
				var element = m_activeTargets.GetArrayElementAtIndex(i);
				var targetProp = element.FindPropertyRelative("m_target");
				var flagsProp = element.FindPropertyRelative("m_flags");
				var isShown = state.HasAll((LeafElementFlags) flagsProp.intValue);

				var rect = RowRect(false);
				var menuRect = new Rect(rect.xMax - MENU_WIDTH, rect.y, MENU_WIDTH, rect.height);
				var dotRect = new Rect(menuRect.x - SPACING - 8f, rect.y + 5f, 8f, 8f);
				var flagsWidth = Mathf.Clamp(rect.width * 0.35f, 100f, 170f);
				var flagsRect = new Rect(dotRect.x - SPACING - flagsWidth, rect.y, flagsWidth, rect.height);
				var objectRect = new Rect(rect.x, rect.y, flagsRect.x - SPACING - rect.x, rect.height);

				EditorGUI.PropertyField(objectRect, targetProp, GUIContent.none);
				FlagsDropdown(flagsRect, flagsProp);

				if (Event.current.type == EventType.Repaint) {
					EditorGUI.DrawRect(dotRect, isShown ? s_cellOutline : s_hiddenDot);
				}
				GUI.Label(dotRect, new GUIContent(string.Empty, isShown ? "Shown in the previewed state" : "Hidden in the previewed state"));

				var clickedMenu = GUI.Button(menuRect, new GUIContent("⋮", "Options"), MenuButton);
				var rightClicked = Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition);
				if (clickedMenu || rightClicked) {
					ShowRowMenu(m_activeTargets, i, "active target");
					if (rightClicked) {
						Event.current.Use();
					}
				}
			}

			var addRect = EditorGUILayout.GetControlRect();
			addRect.width = Mathf.Min(addRect.width, 170f);
			if (GUI.Button(addRect, new GUIContent("+ Add Active Target", "Show a GameObject only in some states, e.g. a checkmark while On"), EditorStyles.miniButton)) {
				var index = m_activeTargets.arraySize;
				m_activeTargets.arraySize++;
				var element = m_activeTargets.GetArrayElementAtIndex(index);
				element.FindPropertyRelative("m_target").objectReferenceValue = null;
				element.FindPropertyRelative("m_flags").intValue = (int) LeafElementFlags.On;
			}
		}

		// ---- Evaluation mirrors LeafColorTarget.Evaluate, from serialized data ----

		private static int WinningLayer(SerializedProperty layers, LeafElementState state)
		{
			var winner = -1;
			for (var i = 0; i < layers.arraySize; i++) {
				var flags = (LeafElementFlags) layers.GetArrayElementAtIndex(i).FindPropertyRelative("m_flags").intValue;
				if (state.HasAll(flags)) {
					winner = i;
				}
			}

			return winner;
		}

		private static Color Evaluate(SerializedProperty colors, SerializedProperty layers, LeafElementState state)
		{
			var field = s_colorFields[(int) state.Mode];
			var color = colors.FindPropertyRelative(field).colorValue;

			for (var i = 0; i < layers.arraySize; i++) {
				var layer = layers.GetArrayElementAtIndex(i);
				var flags = (LeafElementFlags) layer.FindPropertyRelative("m_flags").intValue;
				if (!state.HasAll(flags)) {
					continue;
				}

				var layerColor = layer.FindPropertyRelative("m_colors").FindPropertyRelative(field).colorValue;
				var multiply = layer.FindPropertyRelative("m_blend").enumValueIndex == (int) LeafColorBlend.Multiply;
				color = multiply ? color * layerColor : layerColor;
			}

			return color;
		}
	}
}
