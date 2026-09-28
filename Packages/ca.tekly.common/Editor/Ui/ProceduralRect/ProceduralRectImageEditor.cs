using System;
using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.ProceduralRect
{
	[CustomEditor(typeof(ProceduralRectImage), true)]
	[CanEditMultipleObjects]
	public class ProceduralRectImageEditor : ImageEditor
	{
		private enum ProceduralRectType
		{
			Simple = 0,
			Filled = 3
		}

		private const string SHADER_NAME = "UI/Procedural Rect Image";

		private static readonly GUIContent s_imageTypeContent = new GUIContent("Image Type");
		private static readonly GUIContent s_clockwiseContent = new GUIContent("Clockwise");
		private static readonly GUIContent s_raycastShapeContent = new GUIContent("Raycast Uses Shape",
			"Ignore raycasts outside the rounded/bulged shape instead of using the full rect.");

		private SerializedProperty m_borderWidth;
		private SerializedProperty m_falloffDistance;
		private SerializedProperty m_falloffPower;

		private SerializedProperty m_modifierType;
		private SerializedProperty m_radius;
		private SerializedProperty m_freeRadius;
		private SerializedProperty m_edge;

		private SerializedProperty m_fillMethod;
		private SerializedProperty m_fillOrigin;
		private SerializedProperty m_fillAmount;
		private SerializedProperty m_fillClockwise;
		private SerializedProperty m_type;
		private SerializedProperty m_sprite;
		private SerializedProperty m_material;
		private SerializedProperty m_maskable;
		private SerializedProperty m_tiled;
		private SerializedProperty m_tileFactor;

		private SerializedProperty m_edgeBulge;
		private SerializedProperty m_subdivisionsPerEdge;
		private SerializedProperty m_raycastUsesShape;

		private AnimBool m_showFilled;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_type = serializedObject.FindProperty("m_Type");
			m_fillMethod = serializedObject.FindProperty("m_FillMethod");
			m_fillOrigin = serializedObject.FindProperty("m_FillOrigin");
			m_fillClockwise = serializedObject.FindProperty("m_FillClockwise");
			m_fillAmount = serializedObject.FindProperty("m_FillAmount");
			m_sprite = serializedObject.FindProperty("m_Sprite");
			m_material = serializedObject.FindProperty("m_Material");
			m_maskable = serializedObject.FindProperty("m_Maskable");
			m_tiled = serializedObject.FindProperty("m_tiled");
			m_tileFactor = serializedObject.FindProperty("m_tileFactor");

			var typeEnum = (Image.Type)m_type.enumValueIndex;

			m_showFilled = new AnimBool(!m_type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);
			m_showFilled.valueChanged.AddListener(Repaint);

			m_borderWidth = serializedObject.FindProperty("m_borderWidth");
			m_falloffDistance = serializedObject.FindProperty("m_falloffDistance");
			m_falloffPower = serializedObject.FindProperty("m_falloffPower");

			m_modifierType = serializedObject.FindProperty("m_modifierType");
			m_radius = serializedObject.FindProperty("m_radius");
			m_freeRadius = serializedObject.FindProperty("m_freeRadius");
			m_edge = serializedObject.FindProperty("m_edge");

			m_edgeBulge = serializedObject.FindProperty("m_edgeBulge");
			m_subdivisionsPerEdge = serializedObject.FindProperty("m_subdivisionsPerEdge");
			m_raycastUsesShape = serializedObject.FindProperty("m_raycastUsesShape");

			CheckForShaderChannels();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (m_showFilled != null) {
				m_showFilled.valueChanged.RemoveListener(Repaint);
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_sprite);
			EditorGUILayout.PropertyField(m_Color);
			EditorGUILayout.PropertyField(m_material);
			MaterialWarningGUI();

			RaycastControlsGUI();
			EditorGUILayout.PropertyField(m_raycastUsesShape, s_raycastShapeContent);
			EditorGUILayout.PropertyField(m_maskable);

			UpdateProceduralRectTypeGUI();
			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(m_modifierType);

			switch ((ModifierType)m_modifierType.enumValueIndex) {
				case ModifierType.Round:
					break;
				case ModifierType.Uniform:
					EditorGUILayout.PropertyField(m_radius);
					break;
				case ModifierType.OneEdge:
					EditorGUILayout.PropertyField(m_edge);
					EditorGUILayout.PropertyField(m_radius);
					break;
				case ModifierType.Free:
					FreeRadiusGUI();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(m_borderWidth);

			using (EditorGuiExt.Horizontal()) {
				EditorGUILayout.PrefixLabel("Falloff");

				using var _ = EditorGuiExt.LabelWidth(60);
				EditorGUILayout.PropertyField(m_falloffDistance, new GUIContent("Distance"));
				EditorGUILayout.PropertyField(m_falloffPower, new GUIContent("Power"));
			}

			using (EditorGuiExt.Horizontal()) {
				EditorGUILayout.PrefixLabel("Tiled");
				EditorGUILayout.PropertyField(m_tiled, GUIContent.none, GUILayout.Width(16));
				using (EditorGuiExt.EnabledBlock(m_tiled.boolValue)) {
					using var _ = EditorGuiExt.LabelWidth(18);
					EditorGUILayout.PropertyField(m_tileFactor.FindPropertyRelative("x"), new GUIContent("↔"));
					EditorGUILayout.PropertyField(m_tileFactor.FindPropertyRelative("y"), new GUIContent("↕"));
				}
			}

			TilingWarningGUI();

			EditorGUILayout.PropertyField(m_edgeBulge);
			var hasBulge = m_edgeBulge.vector2Value.sqrMagnitude > 0;
			using (EditorGuiExt.EnabledBlock(hasBulge)) {
				EditorGUILayout.PropertyField(m_subdivisionsPerEdge);
			}

			if (hasBulge && !m_type.hasMultipleDifferentValues && (Image.Type)m_type.enumValueIndex == Image.Type.Filled) {
				EditorGUILayout.HelpBox("Edge Bulge replaces the mesh, so Filled settings are ignored while it is non-zero.", MessageType.Warning);
			}

			if (hasBulge && m_raycastUsesShape.boolValue) {
				EditorGUILayout.HelpBox("Raycasts outside the RectTransform are rejected before the shape test. Use a negative Raycast Padding to make bulged areas clickable.", MessageType.Info);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void FreeRadiusGUI()
		{
			using (EditorGuiExt.Horizontal()) {
				EditorGUILayout.PrefixLabel("Radius");

				using var _ = EditorGuiExt.LabelWidth(20);
				using (EditorGuiExt.Vertical()) {
					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("x"), new GUIContent("╭"));
						EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("y"), GUIContent.none);
						EditorGUILayout.LabelField("╮", GUILayout.Width(16));
					}

					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("w"), new GUIContent("╰"));
						EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("z"), GUIContent.none);
						EditorGUILayout.LabelField("╯", GUILayout.Width(16));
					}
				}
			}
		}

		private void MaterialWarningGUI()
		{
			if (m_material.hasMultipleDifferentValues) {
				return;
			}

			var material = m_material.objectReferenceValue as Material;
			if (material != null && material.shader != null && material.shader.name != SHADER_NAME) {
				EditorGUILayout.HelpBox($"The material's shader is not '{SHADER_NAME}', so the procedural shape will not render.", MessageType.Warning);
			}
		}

		private void TilingWarningGUI()
		{
			if (!m_tiled.boolValue || m_sprite.hasMultipleDifferentValues) {
				return;
			}

			var sprite = m_sprite.objectReferenceValue as Sprite;
			if (sprite == null || sprite == EmptySprite.Get() || sprite.texture == null) {
				return;
			}

			var fullTexture = sprite.rect.width >= sprite.texture.width && sprite.rect.height >= sprite.texture.height;
			if (sprite.packed || !fullTexture || sprite.texture.wrapMode != TextureWrapMode.Repeat) {
				EditorGUILayout.HelpBox("Tiling needs a sprite that uses its whole, non-atlased texture with Wrap Mode = Repeat.", MessageType.Warning);
			}
		}

		private void UpdateProceduralRectTypeGUI()
		{
			if (m_type.hasMultipleDifferentValues) {
				var idx = Convert.ToInt32(EditorGUILayout.EnumPopup(s_imageTypeContent, (ProceduralRectType)(-1)));
				if (idx != -1) {
					m_type.enumValueIndex = idx;
				}
			} else {
				m_type.enumValueIndex = Convert.ToInt32(EditorGUILayout.EnumPopup(s_imageTypeContent,
					(ProceduralRectType)m_type.enumValueIndex));
			}

			++EditorGUI.indentLevel;
			{
				var typeEnum = (Image.Type)m_type.enumValueIndex;

				m_showFilled.target = !m_type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled;

				if (EditorGUILayout.BeginFadeGroup(m_showFilled.faded)) {
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(m_fillMethod);
					if (EditorGUI.EndChangeCheck()) {
						m_fillOrigin.intValue = 0;
					}

					switch ((Image.FillMethod)m_fillMethod.enumValueIndex) {
						case Image.FillMethod.Horizontal:
							m_fillOrigin.intValue =
								(int)(Image.OriginHorizontal)EditorGUILayout.EnumPopup("Fill Origin",
									(Image.OriginHorizontal)m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Vertical:
							m_fillOrigin.intValue =
								(int)(Image.OriginVertical)EditorGUILayout.EnumPopup("Fill Origin",
									(Image.OriginVertical)m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Radial90:
							m_fillOrigin.intValue =
								(int)(Image.Origin90)EditorGUILayout.EnumPopup("Fill Origin",
									(Image.Origin90)m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Radial180:
							m_fillOrigin.intValue =
								(int)(Image.Origin180)EditorGUILayout.EnumPopup("Fill Origin",
									(Image.Origin180)m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Radial360:
							m_fillOrigin.intValue =
								(int)(Image.Origin360)EditorGUILayout.EnumPopup("Fill Origin",
									(Image.Origin360)m_fillOrigin.intValue);
							break;
					}

					EditorGUILayout.PropertyField(m_fillAmount);
					if ((Image.FillMethod)m_fillMethod.enumValueIndex > Image.FillMethod.Vertical) {
						EditorGUILayout.PropertyField(m_fillClockwise, s_clockwiseContent);
					}
				}

				EditorGUILayout.EndFadeGroup();
			}
			--EditorGUI.indentLevel;
		}

		private void CheckForShaderChannels()
		{
			foreach (var obj in targets) {
				var proceduralRect = obj as ProceduralRectImage;
				if (proceduralRect == null || proceduralRect.canvas == null) {
					continue;
				}

				EnsureShaderChannels(proceduralRect.canvas);
				EnsureShaderChannels(proceduralRect.canvas.rootCanvas);
			}
		}

		private static void EnsureShaderChannels(Canvas canvas)
		{
			if (canvas == null) {
				return;
			}

			var needed = ProceduralRectImage.NEEDED_SHADER_CHANNELS;
			if ((canvas.additionalShaderChannels & needed) == needed) {
				return;
			}

			Undo.RecordObject(canvas, "Enable Procedural Rect Shader Channels");
			canvas.additionalShaderChannels |= needed;
			EditorUtility.SetDirty(canvas);
		}
	}
}
