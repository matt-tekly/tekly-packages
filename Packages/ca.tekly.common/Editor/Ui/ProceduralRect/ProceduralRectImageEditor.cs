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
		private SerializedProperty m_tiled;
		private SerializedProperty m_tileFactor;

		private AnimBool m_showFilled;

		private GUIContent m_spriteTypeContent = new GUIContent("Image Type");
		private GUIContent m_clockwiseContent = new GUIContent("Clockwise");

		private int m_selectedId;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_type = serializedObject.FindProperty("m_Type");
			m_fillMethod = serializedObject.FindProperty("m_FillMethod");
			m_fillOrigin = serializedObject.FindProperty("m_FillOrigin");
			m_fillClockwise = serializedObject.FindProperty("m_FillClockwise");
			m_fillAmount = serializedObject.FindProperty("m_FillAmount");
			m_sprite = serializedObject.FindProperty("m_Sprite");
			m_tiled = serializedObject.FindProperty("m_tiled");
			m_tileFactor = serializedObject.FindProperty("m_tileFactor");
			
			var typeEnum = (Image.Type) m_type.enumValueIndex;

			m_showFilled = new AnimBool(!m_type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);
			m_showFilled.valueChanged.AddListener(Repaint);

			m_borderWidth = serializedObject.FindProperty("m_borderWidth");
			m_falloffDistance = serializedObject.FindProperty("m_falloffDistance");
			m_falloffPower = serializedObject.FindProperty("m_falloffPower");
			
			m_modifierType = serializedObject.FindProperty("m_modifierType");
			m_radius = serializedObject.FindProperty("m_radius");
			m_freeRadius = serializedObject.FindProperty("m_freeRadius");
			m_edge = serializedObject.FindProperty("m_edge");
			
			CheckForShaderChannels();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			EditorGUILayout.PropertyField(m_sprite);
			EditorGUILayout.PropertyField(m_Color);

			RaycastControlsGUI();
			UpdateProceduralRectTypeGUI();
			EditorGUILayout.Space();
			
			EditorGUILayout.PropertyField(m_modifierType);
			
			switch ((ModifierType) m_modifierType.enumValueIndex) {
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
					using (EditorGuiExt.Horizontal()) {
						EditorGUILayout.PrefixLabel("Radius");
						EditorGUIUtility.labelWidth = 20;

						using (EditorGuiExt.Vertical()) {
							using (EditorGuiExt.Horizontal()) {
								EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("x"), new GUIContent("\u256D"));
								EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("y"), GUIContent.none);
								EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("y"), new GUIContent("\u256E"), GUILayout.Width(16));
							}

							using (EditorGuiExt.Horizontal()) {
								EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("w"), new GUIContent("\u2570"));
								EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("z"), GUIContent.none);
								EditorGUILayout.PropertyField(m_freeRadius.FindPropertyRelative("z"), new GUIContent("\u256F"), GUILayout.Width(16));
							}
						}
					}
					
					EditorGUIUtility.labelWidth = 0;
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
				using (EditorGuiExt.EnabledBlock(m_tiled.boolValue)){
					using var _ = EditorGuiExt.LabelWidth(18);
					EditorGUILayout.PropertyField(m_tileFactor.FindPropertyRelative("x"), new GUIContent("\u2194"));
					EditorGUILayout.PropertyField(m_tileFactor.FindPropertyRelative("y"), new GUIContent("\u2195"));
				}
			}

			serializedObject.ApplyModifiedProperties();
		}
		
		private void UpdateProceduralRectTypeGUI()
		{
			if (m_type.hasMultipleDifferentValues) {
				int idx = Convert.ToInt32(EditorGUILayout.EnumPopup(m_spriteTypeContent, (ProceduralRectType) (-1)));
				if (idx != -1) {
					m_type.enumValueIndex = idx;
				}
			} else {
				m_type.enumValueIndex = Convert.ToInt32(EditorGUILayout.EnumPopup(m_spriteTypeContent,
					(ProceduralRectType) m_type.enumValueIndex));
			}

			++EditorGUI.indentLevel;
			{
				Image.Type typeEnum = (Image.Type) m_type.enumValueIndex;

				m_showFilled.target = (!m_type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);

				if (EditorGUILayout.BeginFadeGroup(m_showFilled.faded)) {
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(m_fillMethod);
					if (EditorGUI.EndChangeCheck()) {
						m_fillOrigin.intValue = 0;
					}

					switch ((Image.FillMethod) m_fillMethod.enumValueIndex) {
						case Image.FillMethod.Horizontal:
							m_fillOrigin.intValue =
								(int) (Image.OriginHorizontal) EditorGUILayout.EnumPopup("Fill Origin",
									(Image.OriginHorizontal) m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Vertical:
							m_fillOrigin.intValue =
								(int) (Image.OriginVertical) EditorGUILayout.EnumPopup("Fill Origin",
									(Image.OriginVertical) m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Radial90:
							m_fillOrigin.intValue =
								(int) (Image.Origin90) EditorGUILayout.EnumPopup("Fill Origin",
									(Image.Origin90) m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Radial180:
							m_fillOrigin.intValue =
								(int) (Image.Origin180) EditorGUILayout.EnumPopup("Fill Origin",
									(Image.Origin180) m_fillOrigin.intValue);
							break;
						case Image.FillMethod.Radial360:
							m_fillOrigin.intValue =
								(int) (Image.Origin360) EditorGUILayout.EnumPopup("Fill Origin",
									(Image.Origin360) m_fillOrigin.intValue);
							break;
					}

					EditorGUILayout.PropertyField(m_fillAmount);
					if ((Image.FillMethod) m_fillMethod.enumValueIndex > Image.FillMethod.Vertical) {
						EditorGUILayout.PropertyField(m_fillClockwise, m_clockwiseContent);
					}
				}

				EditorGUILayout.EndFadeGroup();
			}
			--EditorGUI.indentLevel;
		}

		private void CheckForShaderChannels()
		{
			var proceduralRect = target as ProceduralRectImage;
			var canvas = proceduralRect.canvas;
			
			if (canvas != null && (canvas.additionalShaderChannels | ProceduralRectImage.NEEDED_SHADER_CHANNELS) != canvas.additionalShaderChannels) {
				canvas.additionalShaderChannels |= ProceduralRectImage.NEEDED_SHADER_CHANNELS;
			}
		}
	}
}