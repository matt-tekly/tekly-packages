using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Tekly.Common.Ui.Fancy
{
	[CustomEditor(typeof(FancyRect), true)]
	[CanEditMultipleObjects]
	public class FancyRectEditor : GraphicEditor
	{
		private static readonly GUIContent s_tintContent = new GUIContent("Tint", "Multiplies every layer.");
		private static readonly GUIContent s_bulgeContent = new GUIContent("Bulge", "Positive bulges outward, negative curves inward.");
		private static readonly GUIContent s_raycastShapeContent = new GUIContent("Raycast Uses Shape",
			"Ignore raycasts outside the shape instead of using the full rect.");

		private SerializedProperty m_radiusMode;
		private SerializedProperty m_radius;
		private SerializedProperty m_cornerRadii;
		private SerializedProperty m_cornerType;
		private SerializedProperty m_cornerTypes;
		private SerializedProperty m_bulge;
		private SerializedProperty m_containBulge;
		private SerializedProperty m_bevel;
		private SerializedProperty m_gloss;
		private SerializedProperty m_revealMethod;
		private SerializedProperty m_revealOrigin;
		private SerializedProperty m_revealAmount;
		private SerializedProperty m_revealClockwise;

		private static readonly GUIContent[] s_horizontalOrigins = { new GUIContent("Left"), new GUIContent("Right") };
		private static readonly RevealOrigin[] s_horizontalOriginValues = { RevealOrigin.Left, RevealOrigin.Right };
		private static readonly GUIContent[] s_verticalOrigins = { new GUIContent("Bottom"), new GUIContent("Top") };
		private static readonly RevealOrigin[] s_verticalOriginValues = { RevealOrigin.Bottom, RevealOrigin.Top };
		private static readonly GUIContent[] s_radialOrigins = { new GUIContent("Top"), new GUIContent("Right"), new GUIContent("Bottom"), new GUIContent("Left") };
		private static readonly RevealOrigin[] s_radialOriginValues = { RevealOrigin.Top, RevealOrigin.Right, RevealOrigin.Bottom, RevealOrigin.Left };
		private SerializedProperty m_fillEnabled;
		private SerializedProperty m_fill;
		private SerializedProperty m_outlines;
		private SerializedProperty m_shadows;
		private SerializedProperty m_raycastUsesShape;
		private SerializedProperty m_blendMode;
		private SerializedProperty m_sprite;
		private SerializedProperty m_textureMode;
		private SerializedProperty m_textureScale;
		private SerializedProperty m_textureOffset;
		private SerializedProperty m_textureRotation;
		private SerializedProperty m_textureScroll;
		private SerializedProperty m_maskable;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_radiusMode = serializedObject.FindProperty("m_radiusMode");
			m_radius = serializedObject.FindProperty("m_radius");
			m_cornerRadii = serializedObject.FindProperty("m_cornerRadii");
			m_cornerType = serializedObject.FindProperty("m_cornerType");
			m_cornerTypes = serializedObject.FindProperty("m_cornerTypes");
			m_bulge = serializedObject.FindProperty("m_bulge");
			m_containBulge = serializedObject.FindProperty("m_containBulge");
			m_bevel = serializedObject.FindProperty("m_bevel");
			m_gloss = serializedObject.FindProperty("m_gloss");
			m_revealMethod = serializedObject.FindProperty("m_revealMethod");
			m_revealOrigin = serializedObject.FindProperty("m_revealOrigin");
			m_revealAmount = serializedObject.FindProperty("m_revealAmount");
			m_revealClockwise = serializedObject.FindProperty("m_revealClockwise");
			m_fillEnabled = serializedObject.FindProperty("m_fillEnabled");
			m_fill = serializedObject.FindProperty("m_fill");
			m_outlines = serializedObject.FindProperty("m_outlines");
			m_shadows = serializedObject.FindProperty("m_shadows");
			m_raycastUsesShape = serializedObject.FindProperty("m_raycastUsesShape");
			m_blendMode = serializedObject.FindProperty("m_blendMode");
			m_sprite = serializedObject.FindProperty("m_sprite");
			m_textureMode = serializedObject.FindProperty("m_textureMode");
			m_textureScale = serializedObject.FindProperty("m_textureScale");
			m_textureOffset = serializedObject.FindProperty("m_textureOffset");
			m_textureRotation = serializedObject.FindProperty("m_textureRotation");
			m_textureScroll = serializedObject.FindProperty("m_textureScroll");
			m_maskable = serializedObject.FindProperty("m_Maskable");

			EnsureShaderChannels();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_Color, s_tintContent);
			EditorGUILayout.PropertyField(m_Material);
			MaterialWarningGUI();
			BlendModeGUI();

			RaycastControlsGUI();
			EditorGUILayout.PropertyField(m_raycastUsesShape, s_raycastShapeContent);

			if (m_maskable != null) {
				EditorGUILayout.PropertyField(m_maskable);
			}

			EditorGUILayout.Space();
			ShapeGUI();

			EditorGUILayout.Space();
			TextureGUI();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Fill", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_fillEnabled, new GUIContent("Enabled"));
			using (EditorGuiExt.EnabledBlock(m_fillEnabled.boolValue || m_fillEnabled.hasMultipleDifferentValues)) {
				EditorGUILayout.PropertyField(m_fill, new GUIContent("Paint"));
			}

			EditorGUILayout.Space();
			BevelGUI();

			EditorGUILayout.Space();
			GlossGUI();

			EditorGUILayout.Space();
			LayerListGUI(m_outlines, "Outlines", "Outline", DrawOutline, InitOutline);

			EditorGUILayout.Space();
			LayerListGUI(m_shadows, "Shadows", "Shadow", DrawShadow, InitShadow);

			EditorGUILayout.Space();
			RevealGUI();

			serializedObject.ApplyModifiedProperties();
		}

		private void ShapeGUI()
		{
			EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_radiusMode);

			var mode = (RadiusMode)m_radiusMode.enumValueIndex;

			if (m_radiusMode.hasMultipleDifferentValues) {
				return;
			}

			switch (mode) {
				case RadiusMode.Uniform:
					EditorGUILayout.PropertyField(m_radius);
					EditorGUILayout.PropertyField(m_cornerType);
					break;
				case RadiusMode.Pill:
					EditorGUILayout.PropertyField(m_cornerType);
					break;
				case RadiusMode.PerCorner:
					CornerGridGUI();
					break;
			}

			EdgeGridGUI(m_bulge, s_bulgeContent);

			using (EditorGuiExt.EnabledBlock(m_bulge.hasMultipleDifferentValues || m_bulge.vector4Value != Vector4.zero)) {
				EditorGUILayout.PropertyField(m_containBulge, new GUIContent("Contain Bulge", m_containBulge.tooltip));
			}
		}

		private void CornerGridGUI()
		{
			var topLeft = m_cornerTypes.FindPropertyRelative(nameof(CornerTypes.TopLeft));
			var topRight = m_cornerTypes.FindPropertyRelative(nameof(CornerTypes.TopRight));
			var bottomRight = m_cornerTypes.FindPropertyRelative(nameof(CornerTypes.BottomRight));
			var bottomLeft = m_cornerTypes.FindPropertyRelative(nameof(CornerTypes.BottomLeft));

			using (EditorGuiExt.Horizontal()) {
				EditorGUILayout.PrefixLabel("Corners");

				using var _ = EditorGuiExt.LabelWidth(16);
				using (EditorGuiExt.Vertical()) {
					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PropertyField(m_cornerRadii.FindPropertyRelative("x"), new GUIContent("╭"));
						EditorGUILayout.PropertyField(topLeft, GUIContent.none);
						EditorGUILayout.PropertyField(m_cornerRadii.FindPropertyRelative("y"), new GUIContent("╮"));
						EditorGUILayout.PropertyField(topRight, GUIContent.none);
					}

					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PropertyField(m_cornerRadii.FindPropertyRelative("w"), new GUIContent("╰"));
						EditorGUILayout.PropertyField(bottomLeft, GUIContent.none);
						EditorGUILayout.PropertyField(m_cornerRadii.FindPropertyRelative("z"), new GUIContent("╯"));
						EditorGUILayout.PropertyField(bottomRight, GUIContent.none);
					}
				}
			}
		}

		private static void EdgeGridGUI(SerializedProperty edges, GUIContent label)
		{
			using (EditorGuiExt.Horizontal()) {
				EditorGUILayout.PrefixLabel(label);

				using var _ = EditorGuiExt.LabelWidth(14);
				using (EditorGuiExt.Vertical()) {
					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PropertyField(edges.FindPropertyRelative("x"), new GUIContent("T"));
						EditorGUILayout.PropertyField(edges.FindPropertyRelative("z"), new GUIContent("B"));
					}

					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PropertyField(edges.FindPropertyRelative("w"), new GUIContent("L"));
						EditorGUILayout.PropertyField(edges.FindPropertyRelative("y"), new GUIContent("R"));
					}
				}
			}
		}

		private delegate void ElementGUI(SerializedProperty element);

		private void LayerListGUI(SerializedProperty list, string title, string elementName, ElementGUI drawElement, ElementGUI initElement)
		{
			using (EditorGuiExt.Horizontal()) {
				EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

				using (EditorGuiExt.EnabledBlock(!list.hasMultipleDifferentValues && list.arraySize < FancyRect.MAX_LAYERS_PER_LIST)) {
					if (GUILayout.Button("Add " + elementName, GUILayout.Width(100))) {
						list.InsertArrayElementAtIndex(list.arraySize);
						initElement(list.GetArrayElementAtIndex(list.arraySize - 1));
					}
				}
			}

			if (list.hasMultipleDifferentValues) {
				EditorGUILayout.HelpBox($"{title} differ between the selected objects.", MessageType.None);
				return;
			}

			var removeIndex = -1;

			for (var i = 0; i < list.arraySize; i++) {
				var element = list.GetArrayElementAtIndex(i);

				using (EditorGuiExt.SmallContainer()) {
					using (EditorGuiExt.Horizontal()) {
						var enabled = element.FindPropertyRelative("Enabled");
						EditorGUILayout.PropertyField(enabled, GUIContent.none, GUILayout.Width(16));
						EditorGUILayout.LabelField($"{elementName} {i + 1}", EditorStyles.miniBoldLabel);

						using (EditorGuiExt.EnabledBlock(i > 0)) {
							if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22))) {
								list.MoveArrayElement(i, i - 1);
							}
						}

						using (EditorGuiExt.EnabledBlock(i < list.arraySize - 1)) {
							if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(22))) {
								list.MoveArrayElement(i, i + 1);
							}
						}

						if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(22))) {
							removeIndex = i;
						}
					}

					using (EditorGuiExt.EnabledBlock(element.FindPropertyRelative("Enabled").boolValue)) {
						drawElement(element);
					}
				}
			}

			if (removeIndex >= 0) {
				list.DeleteArrayElementAtIndex(removeIndex);
			}
		}

		private static void DrawOutline(SerializedProperty outline)
		{
			EditorGUILayout.PropertyField(outline.FindPropertyRelative(nameof(ShapeOutline.Width)));
			EditorGUILayout.PropertyField(outline.FindPropertyRelative(nameof(ShapeOutline.Alignment)));
			EditorGUILayout.PropertyField(outline.FindPropertyRelative(nameof(ShapeOutline.Offset)));
			EditorGUILayout.PropertyField(outline.FindPropertyRelative(nameof(ShapeOutline.Paint)));
			EditorGUILayout.PropertyField(outline.FindPropertyRelative(nameof(ShapeOutline.UseTexture)));
		}

		private static void DrawShadow(SerializedProperty shadow)
		{
			var inset = shadow.FindPropertyRelative(nameof(ShapeShadow.Inset));
			EditorGUILayout.PropertyField(inset);
			EditorGUILayout.PropertyField(shadow.FindPropertyRelative(nameof(ShapeShadow.Offset)));

			// Inner shadows are drawn inside the real shape, so tucking doesn't apply.
			if (inset.hasMultipleDifferentValues || !inset.boolValue) {
				EditorGUILayout.PropertyField(shadow.FindPropertyRelative(nameof(ShapeShadow.Tuck)));
			}

			EditorGUILayout.PropertyField(shadow.FindPropertyRelative(nameof(ShapeShadow.Spread)));
			EditorGUILayout.PropertyField(shadow.FindPropertyRelative(nameof(ShapeShadow.Softness)));
			EditorGUILayout.PropertyField(shadow.FindPropertyRelative(nameof(ShapeShadow.Paint)));
			EditorGUILayout.PropertyField(shadow.FindPropertyRelative(nameof(ShapeShadow.UseTexture)));
		}

		private static void InitOutline(SerializedProperty outline)
		{
			var defaults = ShapeOutline.Default;
			outline.FindPropertyRelative(nameof(ShapeOutline.Enabled)).boolValue = defaults.Enabled;
			outline.FindPropertyRelative(nameof(ShapeOutline.Width)).floatValue = defaults.Width;
			outline.FindPropertyRelative(nameof(ShapeOutline.Offset)).floatValue = defaults.Offset;
			outline.FindPropertyRelative(nameof(ShapeOutline.Alignment)).enumValueIndex = (int)defaults.Alignment;
			InitPaint(outline.FindPropertyRelative(nameof(ShapeOutline.Paint)), defaults.Paint);
			outline.FindPropertyRelative(nameof(ShapeOutline.UseTexture)).boolValue = false;
		}

		private static void InitShadow(SerializedProperty shadow)
		{
			var defaults = ShapeShadow.DefaultDrop;
			shadow.FindPropertyRelative(nameof(ShapeShadow.Enabled)).boolValue = defaults.Enabled;
			shadow.FindPropertyRelative(nameof(ShapeShadow.Inset)).boolValue = defaults.Inset;
			shadow.FindPropertyRelative(nameof(ShapeShadow.Offset)).vector2Value = defaults.Offset;
			shadow.FindPropertyRelative(nameof(ShapeShadow.Tuck)).boolValue = defaults.Tuck;
			shadow.FindPropertyRelative(nameof(ShapeShadow.Spread)).floatValue = defaults.Spread;
			shadow.FindPropertyRelative(nameof(ShapeShadow.Softness)).floatValue = defaults.Softness;
			InitPaint(shadow.FindPropertyRelative(nameof(ShapeShadow.Paint)), defaults.Paint);
			shadow.FindPropertyRelative(nameof(ShapeShadow.UseTexture)).boolValue = false;
		}

		private static void InitPaint(SerializedProperty paint, ShapePaint defaults)
		{
			paint.FindPropertyRelative(nameof(ShapePaint.Color)).colorValue = defaults.Color;
			paint.FindPropertyRelative(nameof(ShapePaint.Gradient)).enumValueIndex = (int)defaults.Gradient;
			paint.FindPropertyRelative(nameof(ShapePaint.Color2)).colorValue = defaults.Color2;
			paint.FindPropertyRelative(nameof(ShapePaint.Angle)).floatValue = defaults.Angle;
		}

		/// <summary>
		/// Header with an Enabled toggle. Turning a never-configured effect on fills in sensible defaults, since a
		/// zeroed struct (the state of existing data) would otherwise be invisible.
		/// </summary>
		private static bool EffectHeaderGUI(string title, SerializedProperty effect, bool isUnconfigured, System.Action<SerializedProperty> applyDefaults)
		{
			var enabled = effect.FindPropertyRelative("Enabled");

			using (EditorGuiExt.Horizontal()) {
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(enabled, GUIContent.none, GUILayout.Width(16));
				if (EditorGUI.EndChangeCheck() && enabled.boolValue && isUnconfigured) {
					applyDefaults(effect);
				}

				EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			}

			return enabled.hasMultipleDifferentValues || enabled.boolValue;
		}

		private void BevelGUI()
		{
			var width = m_bevel.FindPropertyRelative(nameof(ShapeBevel.Width));
			var show = EffectHeaderGUI("Bevel", m_bevel, !width.hasMultipleDifferentValues && width.floatValue <= 0f, ApplyBevelDefaults);

			if (!show) {
				return;
			}

			using (new EditorGUI.IndentLevelScope()) {
				EditorGUILayout.PropertyField(width);
				EditorGUILayout.PropertyField(m_bevel.FindPropertyRelative(nameof(ShapeBevel.Softness)));
				EditorGUILayout.PropertyField(m_bevel.FindPropertyRelative(nameof(ShapeBevel.LightAngle)));
				EditorGUILayout.PropertyField(m_bevel.FindPropertyRelative(nameof(ShapeBevel.Highlight)));
				EditorGUILayout.PropertyField(m_bevel.FindPropertyRelative(nameof(ShapeBevel.Shadow)));
			}
		}

		private static void ApplyBevelDefaults(SerializedProperty bevel)
		{
			var defaults = ShapeBevel.Default;
			bevel.FindPropertyRelative(nameof(ShapeBevel.Width)).floatValue = defaults.Width;
			bevel.FindPropertyRelative(nameof(ShapeBevel.Softness)).floatValue = defaults.Softness;
			bevel.FindPropertyRelative(nameof(ShapeBevel.LightAngle)).floatValue = defaults.LightAngle;
			bevel.FindPropertyRelative(nameof(ShapeBevel.Highlight)).colorValue = defaults.Highlight;
			bevel.FindPropertyRelative(nameof(ShapeBevel.Shadow)).colorValue = defaults.Shadow;
		}

		private void GlossGUI()
		{
			var height = m_gloss.FindPropertyRelative(nameof(ShapeGloss.Height));
			var show = EffectHeaderGUI("Gloss", m_gloss, !height.hasMultipleDifferentValues && height.floatValue <= 0f, ApplyGlossDefaults);

			if (!show) {
				return;
			}

			using (new EditorGUI.IndentLevelScope()) {
				EditorGUILayout.PropertyField(m_gloss.FindPropertyRelative(nameof(ShapeGloss.Color)));
				EditorGUILayout.PropertyField(m_gloss.FindPropertyRelative(nameof(ShapeGloss.Inset)));
				EditorGUILayout.PropertyField(height);
				EditorGUILayout.PropertyField(m_gloss.FindPropertyRelative(nameof(ShapeGloss.Fade)));
				EditorGUILayout.PropertyField(m_gloss.FindPropertyRelative(nameof(ShapeGloss.Softness)));
			}
		}

		private static void ApplyGlossDefaults(SerializedProperty gloss)
		{
			var defaults = ShapeGloss.Default;
			gloss.FindPropertyRelative(nameof(ShapeGloss.Color)).colorValue = defaults.Color;
			gloss.FindPropertyRelative(nameof(ShapeGloss.Inset)).floatValue = defaults.Inset;
			gloss.FindPropertyRelative(nameof(ShapeGloss.Height)).floatValue = defaults.Height;
			gloss.FindPropertyRelative(nameof(ShapeGloss.Fade)).floatValue = defaults.Fade;
			gloss.FindPropertyRelative(nameof(ShapeGloss.Softness)).floatValue = defaults.Softness;
		}

		private void RevealGUI()
		{
			EditorGUILayout.LabelField("Reveal", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_revealMethod, new GUIContent("Method", m_revealMethod.tooltip));

			if (m_revealMethod.hasMultipleDifferentValues) {
				return;
			}

			var method = (RevealMethod)m_revealMethod.intValue;
			if (method == RevealMethod.None) {
				return;
			}

			using (new EditorGUI.IndentLevelScope()) {
				switch (method) {
					case RevealMethod.Horizontal:
						OriginPopupGUI(s_horizontalOrigins, s_horizontalOriginValues);
						break;
					case RevealMethod.Vertical:
						OriginPopupGUI(s_verticalOrigins, s_verticalOriginValues);
						break;
					default:
						OriginPopupGUI(s_radialOrigins, s_radialOriginValues);
						EditorGUILayout.PropertyField(m_revealClockwise, new GUIContent("Clockwise"));
						break;
				}

				EditorGUILayout.PropertyField(m_revealAmount, new GUIContent("Amount"));
			}
		}

		/// <summary>Origin popup limited to the choices that apply to the current method; snaps invalid values.</summary>
		private void OriginPopupGUI(GUIContent[] labels, RevealOrigin[] values)
		{
			var current = System.Array.IndexOf(values, (RevealOrigin)m_revealOrigin.intValue);

			if (current < 0 && !m_revealOrigin.hasMultipleDifferentValues) {
				current = 0;
				m_revealOrigin.intValue = (int)values[0];
			}

			EditorGUI.showMixedValue = m_revealOrigin.hasMultipleDifferentValues;
			EditorGUI.BeginChangeCheck();
			var selected = EditorGUILayout.Popup(new GUIContent("Origin"), current < 0 ? 0 : current, labels);
			if (EditorGUI.EndChangeCheck()) {
				m_revealOrigin.intValue = (int)values[selected];
			}

			EditorGUI.showMixedValue = false;
		}

		private void TextureGUI()
		{
			EditorGUILayout.LabelField("Texture", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_sprite);

			var hasSprite = m_sprite.hasMultipleDifferentValues || m_sprite.objectReferenceValue != null;
			if (!hasSprite) {
				return;
			}

			EditorGUILayout.PropertyField(m_textureMode, new GUIContent("Mode"));
			EditorGUILayout.PropertyField(m_textureScale, new GUIContent("Scale"));
			EditorGUILayout.PropertyField(m_textureOffset, new GUIContent("Offset"));
			EditorGUILayout.PropertyField(m_textureRotation, new GUIContent("Rotation"));

			var isTile = !m_textureMode.hasMultipleDifferentValues && (ShapeTextureMode)m_textureMode.intValue == ShapeTextureMode.Tile;
			using (EditorGuiExt.EnabledBlock(isTile)) {
				EditorGUILayout.PropertyField(m_textureScroll, new GUIContent("Scroll", "Tiles per second. Tile mode only; uses game time, so it stops when Time.timeScale is 0."));
			}

			var scroll = m_textureScroll.vector2Value;
			if (!isTile && scroll != Vector2.zero && !m_textureScroll.hasMultipleDifferentValues) {
				EditorGUILayout.HelpBox("Scroll only applies in Tile mode.", MessageType.None);
			} else if (Mathf.Abs(scroll.x) > FancyRect.MAX_TEXTURE_SCROLL || Mathf.Abs(scroll.y) > FancyRect.MAX_TEXTURE_SCROLL) {
				EditorGUILayout.HelpBox($"Scroll is limited to \u00B1{FancyRect.MAX_TEXTURE_SCROLL:0.##} tiles per second.", MessageType.Info);
			}
			EditorGUILayout.HelpBox("The texture multiplies the fill. Outlines and shadows only use it when their Use Texture is on.", MessageType.None);

			TileWarningGUI();
		}

		private void TileWarningGUI()
		{
			if (m_textureMode.hasMultipleDifferentValues || m_sprite.hasMultipleDifferentValues) {
				return;
			}

			if ((ShapeTextureMode)m_textureMode.intValue != ShapeTextureMode.Tile) {
				return;
			}

			var sprite = m_sprite.objectReferenceValue as Sprite;
			if (sprite == null || sprite.texture == null) {
				return;
			}

			var usesWholeTexture = sprite.rect.width >= sprite.texture.width && sprite.rect.height >= sprite.texture.height;
			if (sprite.packed || !usesWholeTexture || sprite.texture.wrapMode != TextureWrapMode.Repeat) {
				EditorGUILayout.HelpBox("Tile needs a sprite that uses its whole, non-atlased texture with Wrap Mode = Repeat. Use Stretch or Cover for atlased sprites.", MessageType.Warning);
			}
		}

		private void BlendModeGUI()
		{
			var hasCustomMaterial = !m_Material.hasMultipleDifferentValues && m_Material.objectReferenceValue != null;

			using (EditorGuiExt.EnabledBlock(!hasCustomMaterial)) {
				EditorGUILayout.PropertyField(m_blendMode);
			}

			if (hasCustomMaterial) {
				EditorGUILayout.HelpBox("Blend Mode is ignored while a custom Material is assigned. Use FancyRect.ApplyBlendMode to set it up on that material.", MessageType.Info);
			} else if (!m_blendMode.hasMultipleDifferentValues && m_blendMode.enumValueIndex != (int)ShapeBlendMode.Normal) {
				EditorGUILayout.HelpBox("Blends with everything already drawn behind it, and draws in a separate batch from Normal elements.", MessageType.None);
			}
		}

		private void MaterialWarningGUI()
		{
			if (m_Material.hasMultipleDifferentValues) {
				return;
			}

			var material = m_Material.objectReferenceValue as Material;
			if (material != null && material.shader != null && material.shader.name != FancyRect.SHADER_NAME) {
				EditorGUILayout.HelpBox($"The material's shader is not '{FancyRect.SHADER_NAME}', so the shape will not render.", MessageType.Warning);
			}
		}

		private void EnsureShaderChannels()
		{
			foreach (var obj in targets) {
				var shape = obj as FancyRect;
				if (shape == null || shape.canvas == null) {
					continue;
				}

				FancyRectUtility.EnableShaderChannels(shape.canvas);
			}
		}
	}
}
