using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkTimeline))]
	[CanEditMultipleObjects]
	public class ThunkTimelineEditor : Editor
	{
		// Kept so exhaustive and non-repeating selection behaves as it does at runtime across presses
		private readonly ThunkSoundRecord m_record = new ThunkSoundRecord();

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var timeline = target as ThunkTimeline;

			if (timeline == null) {
				return;
			}

			EditorGUILayout.Space();

			if (!HasClips(timeline)) {
				EditorGUILayout.HelpBox("A timeline needs at least one entry with clips to preview.", MessageType.Info);
				return;
			}

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Preview")) {
					Preview(timeline);
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

		private static bool HasClips(ThunkTimeline timeline)
		{
			var entries = timeline.Entries;

			if (entries == null) {
				return false;
			}

			for (var i = 0; i < entries.Length; i++) {
				if (entries[i].Clips.Count > 0) {
					return true;
				}
			}

			return false;
		}

		private void Preview(ThunkTimeline timeline)
		{
			var volume = timeline.Volume.Get();
			var pitch = timeline.Pitch.Get();
			var entries = timeline.Entries;

			ThunkPreview.BeginTimeline();

			for (var i = 0; i < entries.Length; i++) {
				ref var entry = ref entries[i];
				var clip = m_record.Select(i, entry.Clips);

				ThunkPreview.AddTimelineEntry(
					clip,
					entry.Delay,
					volume * Mathf.Clamp01(1f + entry.VolumeOffset),
					pitch * Mathf.Pow(2f, entry.Semitones / 12f),
					timeline.MixerGroup);
			}
		}
	}
}
