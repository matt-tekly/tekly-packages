using System;
using Tekly.Common.Gui;
using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.Common.Ui.Effects
{
    [CustomEditor(typeof(GradientEffect))]
    [CanEditMultipleObjects]
    public class GradientEffectEditor : Editor
    {
	    private SerializedProperty m_gradientType;

	    private SerializedProperty m_colorUpperLeft;
	    private SerializedProperty m_colorUpperRight;
	    private SerializedProperty m_colorLowerRight;
	    private SerializedProperty m_colorLowerLeft;

	    private SerializedProperty m_gradient;

	    private SerializedProperty m_intensity;
	    private SerializedProperty m_angle;

	    private GradientEffect m_target;

	    private void OnEnable()
	    {
		    m_gradientType = serializedObject.FindProperty("m_gradientType");

		    m_colorUpperLeft = serializedObject.FindProperty("m_colorUpperLeft");
		    m_colorUpperRight = serializedObject.FindProperty("m_colorUpperRight");
		    m_colorLowerRight = serializedObject.FindProperty("m_colorLowerRight");
		    m_colorLowerLeft = serializedObject.FindProperty("m_colorLowerLeft");

		    m_gradient = serializedObject.FindProperty("m_gradient");

		    m_intensity = serializedObject.FindProperty("m_intensity");
		    m_angle = serializedObject.FindProperty("m_angle");
		    
		    m_target = target as GradientEffect;
	    }

	    public override void OnInspectorGUI()
	    {
		    serializedObject.Update();
		    
		    EditorGUILayout.PropertyField(m_gradientType, new GUIContent("Type"));

		    switch (m_target.GradientType) {
			    case GradientType.TwoColors:
				    TwoColorGui();
				    break;
			    case GradientType.Corners:
				    CornerColorGui();
				    break;
			    case GradientType.Gradient:
				    EditorGUILayout.PropertyField(m_gradient, GUIContent.none);
				    break;
			    default:
				    throw new ArgumentOutOfRangeException();
		    }

		    EditorGUILayout.PropertyField(m_intensity);
		    EditorGUILayout.PropertyField(m_angle);

		    serializedObject.ApplyModifiedProperties();
	    }

	    private void TwoColorGui()
	    {
		    using (EditorGuiExt.Horizontal()) {
			    m_colorUpperLeft.colorValue = EditorGUILayout.ColorField(m_colorUpperLeft.colorValue);
			    m_colorUpperRight.colorValue = m_colorUpperLeft.colorValue;
			    m_colorLowerLeft.colorValue = EditorGUILayout.ColorField(m_colorLowerLeft.colorValue);
			    m_colorLowerRight.colorValue = m_colorLowerLeft.colorValue;
		    }
	    }

	    private void CornerColorGui()
	    {
		    using (EditorGuiExt.Horizontal()) {
			    m_colorUpperLeft.colorValue = EditorGUILayout.ColorField(m_colorUpperLeft.colorValue);
			    m_colorUpperRight.colorValue = EditorGUILayout.ColorField(m_colorUpperRight.colorValue);
		    }

		    EditorGUILayout.Space();
		    
		    using (EditorGuiExt.Horizontal()) {
			    m_colorLowerLeft.colorValue = EditorGUILayout.ColorField(m_colorLowerLeft.colorValue);
			    m_colorLowerRight.colorValue = EditorGUILayout.ColorField(m_colorLowerRight.colorValue);
		    }
	    }
    }
}