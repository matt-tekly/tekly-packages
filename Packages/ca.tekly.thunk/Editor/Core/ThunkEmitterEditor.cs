using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk.Core
{
	[CustomEditor(typeof(ThunkEmitter))]
	[CanEditMultipleObjects]
	public class ThunkEmitterEditor : Editor
	{
		private SerializedProperty m_audioSourceTemplateProp;
		private SerializedProperty m_volumeProp;
		private SerializedProperty m_pitchProp;
		private SerializedProperty m_ignoreListenerPause;

		private bool m_requiresRepaint;

		private const float StateColumnWidth = 90f;
		private const float TimeColumnWidth = 150f;
		private const float RowHeight = 18f;

		public override bool RequiresConstantRepaint() => m_requiresRepaint;

		private void OnEnable()
		{
			m_audioSourceTemplateProp = serializedObject.FindProperty("m_audioSourceTemplate");
			m_volumeProp = serializedObject.FindProperty("m_volume");
			m_pitchProp = serializedObject.FindProperty("m_pitch");
			m_ignoreListenerPause = serializedObject.FindProperty("m_ignoreListenerPause");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_audioSourceTemplateProp);

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_volumeProp);
			EditorGUILayout.PropertyField(m_pitchProp);
			EditorGUILayout.PropertyField(m_ignoreListenerPause);

			var pitchOrVolumeChanged = EditorGUI.EndChangeCheck();

			serializedObject.ApplyModifiedProperties();

			if (pitchOrVolumeChanged) {
				foreach (var targetObject in targets) {
					var emitter = targetObject as ThunkEmitter;
					emitter.UpdatePitchAndVolume();

					if (!Application.isPlaying) {
						EditorUtility.SetDirty(emitter);
					}
				}
			}

			if (Application.isPlaying) {
				m_requiresRepaint = false;
				foreach (var targetObject in targets) {
					var emitter = targetObject as ThunkEmitter;
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);

					DrawTableRow("Clip", "State", "Time", true);
					foreach (var thunkClipInstance in emitter.Instances) {
						var clipLength = thunkClipInstance.PlayingAudioClip?.length ?? 0;
						var clipTime = thunkClipInstance.Time;
						var time = $"Time: {clipTime:F3}/{clipLength:F3}";
						DrawTableRow(thunkClipInstance.DebuggerDisplay, thunkClipInstance.State.ToString(), time, true);
					}

					EditorGUILayout.EndVertical();

					m_requiresRepaint = true;
				}

				Repaint();
			}
		}

		private void DrawTableRow(string clip, string state, string time, bool isHeader)
		{
			var rowRect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));

			// Some padding
			rowRect.xMin += 4f;
			rowRect.xMax -= 4f;

			var timeRect = new Rect(rowRect.xMax - TimeColumnWidth, rowRect.y, TimeColumnWidth, rowRect.height);
			var stateRect = new Rect(timeRect.x - StateColumnWidth, rowRect.y, StateColumnWidth, rowRect.height);
			var clipRect = new Rect(rowRect.x, rowRect.y, stateRect.x - rowRect.x, rowRect.height);

			var style = isHeader ? EditorStyles.boldLabel : EditorStyles.label;

			EditorGUI.LabelField(clipRect, clip, style);
			EditorGUI.LabelField(stateRect, state, style);

			// Right-align time column
			using (new EditorGUIUtility.IconSizeScope(Vector2.zero)) {
				var rightAlign = new GUIStyle(style) { alignment = TextAnchor.MiddleRight };
				EditorGUI.LabelField(timeRect, time, rightAlign);
			}
		}
	}
}