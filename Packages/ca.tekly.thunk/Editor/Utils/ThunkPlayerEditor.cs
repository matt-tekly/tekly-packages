#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk.Utils
{
	[CustomEditor(typeof(ThunkPlayer))]
	public sealed class ThunkPlayerInspector : Editor
	{
		private SerializedProperty m_emitter;
		private SerializedProperty m_clip;

		private void OnEnable()
		{
			m_emitter = serializedObject.FindProperty("m_emitter");
			m_clip = serializedObject.FindProperty("m_clip");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_emitter);
			EditorGUILayout.PropertyField(m_clip);

			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space();

			if (Application.isPlaying) {
				using (new EditorGUILayout.HorizontalScope()) {
					var player = (ThunkPlayer)target;
					if (GUILayout.Button("Play")) {
						player.Play();
					}

					if (GUILayout.Button("Stop")) {
						player.Stop();
					}
				}
			}
		}
	}
}
#endif