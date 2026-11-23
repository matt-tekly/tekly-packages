using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkClip))]
	[CanEditMultipleObjects]
	public class ThunkClipEditor : ThunkSoundEditor
	{
		private static readonly GUIContent s_clipsLabel = new GUIContent("Clips", "One of these is picked each time the sound plays.");

		private SerializedProperty m_clips;
		private SerializedProperty m_loop;
		private SerializedProperty m_randomizeStartTime;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_clips = serializedObject.FindProperty("m_clips");
			m_loop = serializedObject.FindProperty("m_loop");
			m_randomizeStartTime = serializedObject.FindProperty("m_randomizeStartTime");
		}

		protected override void DrawContent()
		{
			ClipSets.Draw(m_clips, s_clipsLabel);

			EditorGUILayout.PropertyField(m_loop);
			EditorGUILayout.PropertyField(m_randomizeStartTime);
		}

		protected override string GetPreviewProblem()
		{
			return ((ThunkClip) target).Clips.Count == 0 ? "This sound has no clips. Add one to preview it." : null;
		}

		protected override void Preview()
		{
			var clip = (ThunkClip) target;
			var audioClip = Record.Select(0, clip.Clips);

			if (audioClip != null) {
				ThunkPreview.Play(audioClip, clip.Volume.Get(), clip.Pitch.Get(), clip.Loop, clip.MixerGroup);
			}
		}
	}
}
