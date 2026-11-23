using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkEmitter))]
	[CanEditMultipleObjects]
	public class ThunkEmitterEditor : Editor
	{
		private const float STATE_COLUMN_WIDTH = 90f;
		private const float TIME_COLUMN_WIDTH = 110f;
		private const float ROW_HEIGHT = 18f;

		private static GUIStyle s_rightHeaderStyle;
		private static GUIStyle s_rightLabelStyle;

		private SerializedProperty m_volume;
		private SerializedProperty m_pitch;
		private SerializedProperty m_spatialTemplate;
		private SerializedProperty m_playWhilePaused;

		public override bool RequiresConstantRepaint()
		{
			return Application.isPlaying;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_volume);
			EditorGUILayout.PropertyField(m_pitch);
			EditorGUILayout.PropertyField(m_spatialTemplate);
			ThunkSoundEditor.DrawTemplateProblem(m_spatialTemplate);
			EditorGUILayout.PropertyField(m_playWhilePaused);

			serializedObject.ApplyModifiedProperties();

			if (!Application.isPlaying) {
				return;
			}

			var runtime = ThunkRuntime.Instance;

			if (runtime == null) {
				return;
			}

			EditorGUILayout.Space();

			foreach (var targetObject in targets) {
				var emitter = targetObject as ThunkEmitter;

				if (emitter == null) {
					continue;
				}

				DrawVoices(runtime, emitter);
			}
		}

		private void OnEnable()
		{
			m_volume = serializedObject.FindProperty("m_volume");
			m_pitch = serializedObject.FindProperty("m_pitch");
			m_spatialTemplate = serializedObject.FindProperty("m_spatialTemplate");
			m_playWhilePaused = serializedObject.FindProperty("m_playWhilePaused");
		}

		private static void DrawVoices(ThunkRuntime runtime, ThunkEmitter emitter)
		{
			EnsureStyles();

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
				DrawRow("Sound", "State", "Time", EditorStyles.boldLabel, s_rightHeaderStyle);

				var voices = runtime.ActiveVoices;

				for (var i = 0; i < voices.Count; i++) {
					var voice = voices[i];

					if (voice == null || !ReferenceEquals(voice.Emitter, emitter)) {
						continue;
					}

					var soundName = voice.Sound != null ? voice.Sound.name : "(none)";
					DrawRow(soundName, voice.State.ToString(), GetTimeText(voice), EditorStyles.label, s_rightLabelStyle);
				}
			}
		}

		private static string GetTimeText(ThunkVoice voice)
		{
			if (voice.Sources.Count == 0) {
				return "-";
			}

			var audio = voice.Sources[0].Audio;

			if (audio == null || audio.clip == null) {
				return "-";
			}

			return $"{audio.time:0.00}/{audio.clip.length:0.00}";
		}

		private static void DrawRow(string sound, string state, string time, GUIStyle style, GUIStyle timeStyle)
		{
			var row = GUILayoutUtility.GetRect(0f, ROW_HEIGHT, GUILayout.ExpandWidth(true));
			row.xMin += 4f;
			row.xMax -= 4f;

			var timeRect = new Rect(row.xMax - TIME_COLUMN_WIDTH, row.y, TIME_COLUMN_WIDTH, row.height);
			var stateRect = new Rect(timeRect.x - STATE_COLUMN_WIDTH, row.y, STATE_COLUMN_WIDTH, row.height);
			var soundRect = new Rect(row.x, row.y, stateRect.x - row.x, row.height);

			EditorGUI.LabelField(soundRect, sound, style);
			EditorGUI.LabelField(stateRect, state, style);
			EditorGUI.LabelField(timeRect, time, timeStyle);
		}

		private static void EnsureStyles()
		{
			if (s_rightLabelStyle != null) {
				return;
			}

			s_rightLabelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
			s_rightHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleRight };
		}
	}
}
