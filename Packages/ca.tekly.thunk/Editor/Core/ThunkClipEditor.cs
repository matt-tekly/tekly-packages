using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkClip))]
	[CanEditMultipleObjects]
	public class ThunkClipEditor : Editor
	{
		// Kept so exhaustive and non-repeating selection behaves as it does at runtime across presses
		private readonly ThunkSoundRecord m_record = new ThunkSoundRecord();

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var clip = target as ThunkClip;

			if (clip == null) {
				return;
			}

			EditorGUILayout.Space();

			if (clip.Clips.Count == 0) {
				EditorGUILayout.HelpBox("This sound has no clips to preview.", MessageType.Info);
				return;
			}

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Preview")) {
					Preview(clip);
				}

				if (GUILayout.Button("Stop")) {
					ThunkPreview.Stop();
				}
			}
		}

		private void OnDisable()
		{
			ThunkPreview.Stop();
		}

		private void Preview(ThunkClip clip)
		{
			var audioClip = m_record.Select(0, clip.Clips);

			if (audioClip == null) {
				return;
			}

			ThunkPreview.Play(audioClip, clip.Volume.Get(), clip.Pitch.Get(), clip.Loop, clip.MixerGroup);
		}
	}
}
