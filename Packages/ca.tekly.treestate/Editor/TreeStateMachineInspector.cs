// ============================================================================
// Copyright 2021 Matt King
// ============================================================================
using UnityEditor;

namespace Tekly.TreeState
{
	[CustomEditor(typeof(TreeStateMachine))]
	public class TreeStateMachineInspector : TreeStateInspector
	{
		private SerializedProperty m_defaultState;
        
		protected override void OnEnable()
		{
			base.OnEnable();
			m_defaultState = serializedObject.FindProperty("DefaultState");
		}

		protected override void DrawProperties()
		{
			EditorGUILayout.PropertyField(LoadType);
			EditorGUILayout.PropertyField(m_defaultState);
		}
	}
}