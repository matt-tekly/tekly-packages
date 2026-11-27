using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.Common.Ui.Styled
{
	/// <summary>
	/// Inspector for StyledRect, built with UI Toolkit: the shape, then every layer in one list ordered front
	/// to back. A layer is a one-line summary that unfolds to the fields its kind uses.
	/// </summary>
	[CustomEditor(typeof(StyledRect), true)]
	[CanEditMultipleObjects]
	public class StyledRectEditor : Editor
	{
		public const string LAYER_ROW_CLASS = "styled-rect-layer";

		private const string PAINT_COLOR = nameof(StyledLayer.Paint) + "." + nameof(StyledPaint.Color);
		private const string PAINT_COLOR2 = nameof(StyledLayer.Paint) + "." + nameof(StyledPaint.Color2);
		private const string PAINT_ANGLE = nameof(StyledLayer.Paint) + "." + nameof(StyledPaint.Angle);
		private const string STROKE = nameof(StyledLayer.StrokeStyle) + ".";

		/// <summary>A field of a layer shown when it is unfolded: its path inside the layer, and a label when the field's own name does not fit.</summary>
		private readonly struct LayerField
		{
			public readonly string Path;
			public readonly string Label;
			public readonly string Tooltip;

			public LayerField(string path, string label = null, string tooltip = null)
			{
				Path = path;
				Label = label;
				Tooltip = tooltip;
			}
		}

		// Every field a layer row can show. A row builds all of them once and shows the ones its kind uses.
		private static readonly LayerField s_kind = new LayerField(nameof(StyledLayer.Kind));
		private static readonly LayerField s_width = new LayerField(nameof(StyledLayer.Width));
		private static readonly LayerField s_align = new LayerField(STROKE + nameof(StyledStrokeStyle.Align));
		private static readonly LayerField s_offset = new LayerField(nameof(StyledLayer.EdgeOffset), "Offset",
			"Extra distance from the shape's edge, in the direction of the alignment.");
		private static readonly LayerField s_inset = new LayerField(nameof(StyledLayer.EdgeOffset), "Inset",
			"How far the fill's edge is pulled in from the shape's edge.");
		private static readonly LayerField s_dashSpacing = new LayerField(STROKE + nameof(StyledStrokeStyle.DashSpacing));
		private static readonly LayerField s_dashLength = new LayerField(STROKE + nameof(StyledStrokeStyle.DashLength));
		private static readonly LayerField s_dashSpeed = new LayerField(STROKE + nameof(StyledStrokeStyle.DashSpeed));
		private static readonly LayerField s_dashRoundCaps = new LayerField(STROKE + nameof(StyledStrokeStyle.DashRoundCaps), "Round Caps",
			"Round the ends of each dash. The caps fit inside the dash length.");
		private static readonly LayerField s_shadowOffset = new LayerField(nameof(StyledLayer.ShadowOffset), "Offset",
			"How far the shadow is moved from the shape. Y is up, so (0, -6) is below the shape.");
		private static readonly LayerField s_blur = new LayerField(nameof(StyledLayer.Blur));
		private static readonly LayerField s_spread = new LayerField(nameof(StyledLayer.EdgeOffset), "Spread",
			"Grows a shadow, or pushes an inner shadow further in, before blurring.");
		private static readonly LayerField s_showBehind = new LayerField(nameof(StyledLayer.ShowBehind));
		private static readonly LayerField s_paint = new LayerField(nameof(StyledLayer.Paint));
		private static readonly LayerField s_textured = new LayerField(nameof(StyledLayer.Textured), "Use Texture", "Multiply this layer by the rect's sprite.");
		private static readonly LayerField s_ignoreTint = new LayerField(nameof(StyledLayer.IgnoreTint));
		private static readonly LayerField s_ignoreReveal = new LayerField(nameof(StyledLayer.IgnoreReveal));
		private static readonly LayerField s_highlight = new LayerField(PAINT_COLOR, "Highlight", "Where the edge faces the light.");
		private static readonly LayerField s_shadow = new LayerField(PAINT_COLOR2, "Shadow", "Where the edge faces away from the light.");
		private static readonly LayerField s_lightAngle = new LayerField(PAINT_ANGLE, "Light Angle",
			"Where the light comes from. As in CSS: 0 is straight above and it turns clockwise, so 315 is top-left.");

		private static readonly LayerField[] s_allFields = {
			s_kind, s_width, s_align, s_offset, s_dashSpacing, s_dashLength, s_dashSpeed, s_dashRoundCaps, s_inset, s_shadowOffset, s_blur, s_spread, s_showBehind, s_paint, s_textured, s_highlight, s_shadow, s_lightAngle, s_ignoreTint, s_ignoreReveal
		};

		// The fields each layer kind shows when unfolded.
		private static readonly LayerField[] s_fillFields = { s_kind, s_paint, s_inset, s_blur, s_textured, s_ignoreTint, s_ignoreReveal };
		private static readonly LayerField[] s_strokeFields = {
			s_kind, s_width, s_align, s_offset, s_paint, s_dashSpacing, s_dashLength, s_dashSpeed, s_dashRoundCaps,
			s_textured, s_ignoreTint, s_ignoreReveal
		};

		// Shown only on a stroke that has a dash spacing.
		private static readonly LayerField[] s_dashDetailFields = { s_dashLength, s_dashSpeed, s_dashRoundCaps };

		private static readonly LayerField[] s_shadowFields = { s_kind, s_shadowOffset, s_blur, s_spread, s_showBehind, s_paint, s_textured, s_ignoreTint, s_ignoreReveal };
		private static readonly LayerField[] s_innerShadowFields = { s_kind, s_shadowOffset, s_blur, s_spread, s_paint, s_textured, s_ignoreTint, s_ignoreReveal };
		private static readonly LayerField[] s_bevelFields = { s_kind, s_width, s_blur, s_highlight, s_shadow, s_lightAngle, s_ignoreTint, s_ignoreReveal };

		private static bool s_advancedExpanded;

		private SerializedProperty m_layers;

		public override VisualElement CreateInspectorGUI()
		{
			m_layers = serializedObject.FindProperty("m_layers");

			var root = new VisualElement();
			root.Add(new PropertyField(serializedObject.FindProperty("m_Color"), "Tint") { tooltip = "Multiplies every layer." });

			root.Add(CreateHeading("Shape"));
			root.Add(CreateShape());

			root.Add(CreateHeading("Reveal"));
			root.Add(CreateReveal());

			root.Add(CreateHeading("Texture"));
			root.Add(CreateTexture());

			root.Add(CreateHeading("Layers", "Front to back: the first layer is drawn on top."));
			root.Add(CreateLayers());

			root.Add(CreateAdvanced());

			return root;
		}

		private static Label CreateHeading(string text, string tooltip = null)
		{
			var heading = new Label(text) { tooltip = tooltip };
			heading.style.unityFontStyleAndWeight = FontStyle.Bold;
			heading.style.marginTop = 8f;
			heading.style.marginBottom = 2f;
			return heading;
		}

		private VisualElement CreateShape()
		{
			var radiusMode = serializedObject.FindProperty("m_radiusMode");

			var shape = new VisualElement();
			var radius = new PropertyField(serializedObject.FindProperty("m_radius"));
			var cornerRadii = CreateCornerGrid();

			shape.Add(new PropertyField(radiusMode));
			shape.Add(radius);
			shape.Add(cornerRadii);
			shape.Add(new PropertyField(serializedObject.FindProperty("m_roundedCorners")));
			shape.Add(new PropertyField(serializedObject.FindProperty("m_cornerSmoothing")));
			shape.Add(CreateWobble());

			void Refresh()
			{
				var mixed = radiusMode.hasMultipleDifferentValues;
				var mode = (StyledRadiusMode)radiusMode.enumValueIndex;

				SetVisible(radius, !mixed && mode == StyledRadiusMode.Uniform);
				SetVisible(cornerRadii, !mixed && mode == StyledRadiusMode.PerCorner);
			}

			shape.TrackPropertyValue(radiusMode, _ => Refresh());
			Refresh();

			return shape;
		}

		/// <summary>The shape's wobble: the amount, and once there is one, the lobes' length and seed.</summary>
		private VisualElement CreateWobble()
		{
			var amount = serializedObject.FindProperty("m_wobbleAmount");

			var wobble = new VisualElement();
			wobble.Add(new PropertyField(amount, "Wobble"));

			var details = new VisualElement();
			details.style.marginLeft = 15f;
			details.Add(new PropertyField(serializedObject.FindProperty("m_wobbleLength"), "Lobe Length"));
			details.Add(new PropertyField(serializedObject.FindProperty("m_wobbleSeed"), "Seed"));
			wobble.Add(details);

			void Refresh()
			{
				SetVisible(details, amount.hasMultipleDifferentValues || amount.floatValue > 0f);
			}

			wobble.TrackPropertyValue(amount, _ => Refresh());
			Refresh();

			return wobble;
		}

		/// <summary>The four corner radii laid out the way the corners sit: top-left and top-right above bottom-left and bottom-right.</summary>
		private VisualElement CreateCornerGrid()
		{
			var cornerRadii = serializedObject.FindProperty("m_cornerRadii");

			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;

			var label = new Label("Corner Radii");
			label.AddToClassList(PropertyField.labelUssClassName);
			label.style.minWidth = 120f;
			label.style.paddingLeft = 3f;
			row.Add(label);

			var grid = new VisualElement();
			grid.style.flexGrow = 1f;
			row.Add(grid);

			grid.Add(CreateCornerRow(cornerRadii, "x", "╭", "y", "╮"));
			grid.Add(CreateCornerRow(cornerRadii, "w", "╰", "z", "╯"));

			return row;
		}

		private static VisualElement CreateCornerRow(SerializedProperty cornerRadii, string left, string leftLabel, string right, string rightLabel)
		{
			var row = new VisualElement();
			row.style.flexDirection = FlexDirection.Row;
			row.Add(CreateCornerField(cornerRadii.FindPropertyRelative(left), leftLabel));
			row.Add(CreateCornerField(cornerRadii.FindPropertyRelative(right), rightLabel));
			return row;
		}

		private static FloatField CreateCornerField(SerializedProperty property, string label)
		{
			var field = new FloatField(label);
			field.style.flexGrow = 1f;
			field.style.flexBasis = 0f;
			field.labelElement.style.minWidth = 16f;
			field.labelElement.style.width = 16f;
			field.BindProperty(property);
			return field;
		}

		/// <summary>The reveal, and once one is set, its amount and the settings its kind uses.</summary>
		private VisualElement CreateReveal()
		{
			var method = serializedObject.FindProperty("m_reveal");

			var reveal = new VisualElement();
			reveal.Add(new PropertyField(method, "Reveal"));

			var amount = new PropertyField(serializedObject.FindProperty("m_revealAmount"), "Amount");
			var cap = new PropertyField(serializedObject.FindProperty("m_revealCap"), "Cap");
			var start = new PropertyField(serializedObject.FindProperty("m_radialStart"), "Start");
			var clockwise = new PropertyField(serializedObject.FindProperty("m_radialClockwise"), "Clockwise");

			reveal.Add(amount);
			reveal.Add(cap);
			reveal.Add(start);
			reveal.Add(clockwise);

			void Refresh()
			{
				var mixed = method.hasMultipleDifferentValues;
				var value = (StyledReveal)method.intValue;
				var radial = !mixed && value == StyledReveal.Radial;
				var linear = !mixed && value != StyledReveal.None && value != StyledReveal.Radial;

				SetVisible(amount, mixed || value != StyledReveal.None);
				SetVisible(cap, linear);
				SetVisible(start, radial);
				SetVisible(clockwise, radial);
			}

			reveal.TrackPropertyValue(method, _ => Refresh());
			Refresh();

			return reveal;
		}

		/// <summary>The sprite, and once one is set, how it is placed. Warns when a Tile sprite cannot tile.</summary>
		private VisualElement CreateTexture()
		{
			var sprite = serializedObject.FindProperty("m_sprite");
			var mode = serializedObject.FindProperty("m_textureMode");

			var texture = new VisualElement();
			texture.Add(new PropertyField(sprite));

			var scroll = new PropertyField(serializedObject.FindProperty("m_textureScroll"), "Scroll");

			var settings = new VisualElement();
			settings.Add(new PropertyField(mode, "Mode"));
			settings.Add(new PropertyField(serializedObject.FindProperty("m_textureScale"), "Scale"));
			settings.Add(new PropertyField(serializedObject.FindProperty("m_textureOffset"), "Offset"));
			settings.Add(new PropertyField(serializedObject.FindProperty("m_textureRotation"), "Rotation"));
			settings.Add(scroll);
			settings.Add(new HelpBox("The texture multiplies the layers that have Use Texture on.", HelpBoxMessageType.None));
			texture.Add(settings);

			var warning = new HelpBox("Tile needs a sprite that uses its whole, non-atlased texture with Wrap Mode set to Repeat. Cover works with any sprite.", HelpBoxMessageType.Warning);
			texture.Add(warning);

			void Refresh()
			{
				var hasSprite = sprite.hasMultipleDifferentValues || sprite.objectReferenceValue != null;
				var isTile = !mode.hasMultipleDifferentValues && (StyledTextureMode)mode.intValue == StyledTextureMode.Tile;

				SetVisible(settings, hasSprite);
				SetVisible(scroll, mode.hasMultipleDifferentValues || isTile);
				SetVisible(warning, isTile && !sprite.hasMultipleDifferentValues && !CanTile(sprite.objectReferenceValue as Sprite));
			}

			texture.TrackPropertyValue(sprite, _ => Refresh());
			texture.TrackPropertyValue(mode, _ => Refresh());
			Refresh();

			return texture;
		}

		private static bool CanTile(Sprite sprite)
		{
			if (sprite == null || sprite.texture == null) {
				return true;
			}

			var usesWholeTexture = sprite.rect.width >= sprite.texture.width && sprite.rect.height >= sprite.texture.height;
			return !sprite.packed && usesWholeTexture && sprite.texture.wrapMode == TextureWrapMode.Repeat;
		}

		private VisualElement CreateLayers()
		{
			if (m_layers.hasMultipleDifferentValues) {
				return new HelpBox("Layers differ between the selected objects.", HelpBoxMessageType.None);
			}

			var list = new ListView {
				bindingPath = m_layers.propertyPath,
				showBoundCollectionSize = false,
				showFoldoutHeader = false,
				showAddRemoveFooter = true,
				showBorder = true,
				reorderable = true,
				reorderMode = ListViewReorderMode.Animated,
				selectionType = SelectionType.Single,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				makeItem = () => new LayerRow(),
				bindItem = (element, index) => ((LayerRow)element).Bind(m_layers.GetArrayElementAtIndex(index)),
				unbindItem = (element, _) => ((LayerRow)element).Release(),
				overridingAddButtonBehavior = (_, button) => ShowAddMenu(button)
			};

			return list;
		}

		private VisualElement CreateAdvanced()
		{
			var advanced = new Foldout { text = "Advanced", value = s_advancedExpanded };
			advanced.style.marginTop = 8f;
			advanced.RegisterValueChangedCallback(evt => {
				if (evt.target == advanced) {
					s_advancedExpanded = evt.newValue;
				}
			});

			advanced.Add(new PropertyField(serializedObject.FindProperty("m_blendMode")));
			advanced.Add(new PropertyField(serializedObject.FindProperty("m_Material")));
			advanced.Add(new PropertyField(serializedObject.FindProperty("m_RaycastTarget")));
			advanced.Add(new PropertyField(serializedObject.FindProperty("m_raycastUsesShape")));
			advanced.Add(new PropertyField(serializedObject.FindProperty("m_RaycastPadding")));
			advanced.Add(new PropertyField(serializedObject.FindProperty("m_Maskable")));

			return advanced;
		}

		private void ShowAddMenu(VisualElement button)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Fill"), false, () => AddLayer(StyledLayerKind.Fill));
			menu.AddItem(new GUIContent("Stroke"), false, () => AddLayer(StyledLayerKind.Stroke));
			menu.AddItem(new GUIContent("Shadow"), false, () => AddLayer(StyledLayerKind.Shadow));
			menu.AddItem(new GUIContent("Inner Shadow"), false, () => AddLayer(StyledLayerKind.InnerShadow));
			menu.AddItem(new GUIContent("Bevel"), false, () => AddLayer(StyledLayerKind.Bevel));
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Gloss"), false, AddGloss);
			menu.DropDown(button.worldBound);
		}

		/// <summary>
		/// Adds a layer through the component's own methods, so each kind lands where it belongs in the order
		/// (a shadow at the back, an inner shadow or bevel just above the fill) with every field set.
		/// </summary>
		private void AddLayer(StyledLayerKind kind)
		{
			foreach (var item in targets) {
				var rect = (StyledRect)item;
				Undo.RecordObject(rect, "Add Layer");

				switch (kind) {
					case StyledLayerKind.Stroke:
						rect.AddStroke(4, Color.white);
						break;
					case StyledLayerKind.Shadow:
						rect.AddDropShadow(new Vector2(0, -6), 10, new Color(0, 0, 0, 0.35f));
						break;
					case StyledLayerKind.InnerShadow:
						rect.AddInnerShadow(new Vector2(0, -4), 6, new Color(0, 0, 0, 0.3f));
						break;
					case StyledLayerKind.Bevel:
						rect.AddBevel(6, 2, new Color(1, 1, 1, 0.5f), new Color(0, 0, 0, 0.3f));
						break;
					default:
						rect.AddLayer(StyledLayer.Fill(Color.white));
						break;
				}

				EditorUtility.SetDirty(rect);
				PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
			}

			serializedObject.Update();
		}

		/// <summary>Gloss is a preset: it adds an ordinary fill set up as a highlight.</summary>
		private void AddGloss()
		{
			foreach (var item in targets) {
				var rect = (StyledRect)item;
				Undo.RecordObject(rect, "Add Gloss");
				rect.AddGloss(new Color(1, 1, 1, 0.45f), 4, 1);
				EditorUtility.SetDirty(rect);
				PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
			}

			serializedObject.Update();
		}

		private static LayerField[] GetFields(StyledLayerKind kind)
		{
			return kind switch {
				StyledLayerKind.Stroke => s_strokeFields,
				StyledLayerKind.Shadow => s_shadowFields,
				StyledLayerKind.InnerShadow => s_innerShadowFields,
				StyledLayerKind.Bevel => s_bevelFields,
				_ => s_fillFields
			};
		}

		/// <summary>The one-line description of a layer shown in its header.</summary>
		private static string Summarize(SerializedProperty layer)
		{
			var gradient = (StyledGradient)layer.FindPropertyRelative(nameof(StyledLayer.Paint)).FindPropertyRelative(nameof(StyledPaint.Gradient)).intValue;
			var paint = gradient == StyledGradient.None ? "" : $"   {gradient.ToString().ToLowerInvariant()} gradient";

			switch ((StyledLayerKind)layer.FindPropertyRelative(nameof(StyledLayer.Kind)).intValue) {
				case StyledLayerKind.Stroke: {
					var width = layer.FindPropertyRelative(nameof(StyledLayer.Width)).floatValue;
					var align = (StyledStrokeAlign)layer.FindPropertyRelative(STROKE + nameof(StyledStrokeStyle.Align)).intValue;
					var dashes = layer.FindPropertyRelative(STROKE + nameof(StyledStrokeStyle.DashSpacing)).floatValue > 0f ? "   dashed" : "";
					return $"Stroke   {width:0.##} {align.ToString().ToLowerInvariant()}{dashes}{paint}";
				}
				case StyledLayerKind.Shadow:
					return $"Shadow   {SummarizeShadow(layer)}{paint}";
				case StyledLayerKind.InnerShadow:
					return $"Inner Shadow   {SummarizeShadow(layer)}{paint}";
				case StyledLayerKind.Bevel:
					return $"Bevel   {layer.FindPropertyRelative(nameof(StyledLayer.Width)).floatValue:0.##}";
				default: {
					var inset = layer.FindPropertyRelative(nameof(StyledLayer.EdgeOffset)).floatValue;
					return inset == 0f ? $"Fill{paint}" : $"Fill   inset {inset:0.##}{paint}";
				}
			}
		}

		private static string SummarizeShadow(SerializedProperty layer)
		{
			var offset = layer.FindPropertyRelative(nameof(StyledLayer.ShadowOffset)).vector2Value;
			var blur = layer.FindPropertyRelative(nameof(StyledLayer.Blur)).floatValue;
			var spread = layer.FindPropertyRelative(nameof(StyledLayer.EdgeOffset)).floatValue;
			var text = $"{offset.x:0.##}, {offset.y:0.##}   blur {blur:0.##}";

			return spread == 0f ? text : $"{text}   spread {spread:0.##}";
		}

		private static void SetVisible(VisualElement element, bool visible)
		{
			element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// One layer in the list: a header (unfold arrow, enabled toggle, summary, color swatch) and a body with
		/// the fields its kind uses. The list reuses rows, so a row is bound to a layer and released again.
		/// </summary>
		private sealed class LayerRow : VisualElement
		{
			private readonly Toggle m_arrow;
			private readonly Toggle m_enabled;
			private readonly Label m_summary;
			private readonly ColorField m_swatch;
			private readonly VisualElement m_body;
			private readonly PropertyField[] m_fields = new PropertyField[s_allFields.Length];

			private SerializedProperty m_layer;

			public LayerRow()
			{
				AddToClassList(LAYER_ROW_CLASS);
				style.paddingTop = 1f;
				style.paddingBottom = 1f;

				var header = new VisualElement();
				header.style.flexDirection = FlexDirection.Row;
				header.style.alignItems = Align.Center;
				Add(header);

				// A toggle with the foldout's class draws Unity's standard unfold arrow.
				m_arrow = new Toggle();
				m_arrow.AddToClassList(Foldout.toggleUssClassName);
				m_arrow.style.marginLeft = 0f;
				m_arrow.style.marginRight = 2f;
				m_arrow.style.flexShrink = 0f;
				m_arrow.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));
				header.Add(m_arrow);

				m_enabled = new Toggle { tooltip = "Draw this layer." };
				m_enabled.style.marginRight = 4f;
				header.Add(m_enabled);

				m_summary = new Label();
				m_summary.style.flexGrow = 1f;
				m_summary.style.flexShrink = 1f;
				m_summary.style.overflow = Overflow.Hidden;
				m_summary.RegisterCallback<ClickEvent>(_ => SetExpanded(m_layer != null && !m_layer.isExpanded));
				header.Add(m_summary);

				m_swatch = new ColorField();
				m_swatch.style.width = 56f;
				m_swatch.style.flexShrink = 0f;
				header.Add(m_swatch);

				m_body = new VisualElement();
				m_body.style.marginLeft = 18f;
				m_body.style.marginTop = 2f;
				m_body.style.marginBottom = 4f;
				Add(m_body);

				for (var i = 0; i < s_allFields.Length; i++) {
					m_fields[i] = new PropertyField();
					m_body.Add(m_fields[i]);
				}
			}

			public void Bind(SerializedProperty layer)
			{
				Release();
				m_layer = layer.Copy();

				m_enabled.BindProperty(layer.FindPropertyRelative(nameof(StyledLayer.Enabled)));
				m_swatch.BindProperty(layer.FindPropertyRelative(PAINT_COLOR));

				for (var i = 0; i < s_allFields.Length; i++) {
					var field = s_allFields[i];
					var property = layer.FindPropertyRelative(field.Path);

					m_fields[i].label = field.Label ?? property.displayName;
					m_fields[i].tooltip = field.Tooltip ?? property.tooltip;
					m_fields[i].BindProperty(property);
				}

				// Any change inside the layer can change its summary, and a new kind changes which fields show.
				this.TrackPropertyValue(m_layer, _ => Refresh());
				Refresh();
			}

			/// <summary>Drops the row's bindings so it can be reused for another layer.</summary>
			public void Release()
			{
				if (m_layer == null) {
					return;
				}

				this.Unbind();
				m_layer = null;
			}

			private void SetExpanded(bool expanded)
			{
				if (m_layer == null) {
					return;
				}

				m_layer.isExpanded = expanded;
				Refresh();
			}

			private void Refresh()
			{
				if (m_layer == null) {
					return;
				}

				var kind = (StyledLayerKind)m_layer.FindPropertyRelative(nameof(StyledLayer.Kind)).intValue;
				var enabled = m_layer.FindPropertyRelative(nameof(StyledLayer.Enabled)).boolValue;
				var shown = GetFields(kind);

				m_arrow.SetValueWithoutNotify(m_layer.isExpanded);
				m_summary.text = Summarize(m_layer);
				m_summary.style.opacity = enabled ? 1f : 0.5f;

				SetVisible(m_body, m_layer.isExpanded);
				m_body.SetEnabled(enabled);

				var dashed = m_layer.FindPropertyRelative(STROKE + nameof(StyledStrokeStyle.DashSpacing)).floatValue > 0f;

				for (var i = 0; i < s_allFields.Length; i++) {
					var visible = Array.IndexOf(shown, s_allFields[i]) >= 0;

					if (visible && !dashed && Array.IndexOf(s_dashDetailFields, s_allFields[i]) >= 0) {
						visible = false;
					}

					SetVisible(m_fields[i], visible);
				}
			}
		}
	}
}
