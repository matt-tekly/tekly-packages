using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkTrack))]
	public class ThunkTrackEditor : Editor
	{
		public override bool RequiresConstantRepaint()
		{
			return Application.isPlaying;
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (!Application.isPlaying) {
				return;
			}

			var track = target as ThunkTrack;
			var runtime = ThunkRuntime.Instance;

			if (track == null || runtime == null) {
				return;
			}

			var state = runtime.GetTrack(track);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Playing", EditorStyles.boldLabel);

			if (state.Count == 0) {
				EditorGUILayout.LabelField("Nothing");
				return;
			}

			for (var i = 0; i < state.Count; i++) {
				var sound = state.GetSound(i);
				var soundName = sound != null ? sound.name : "(none)";
				var label = i == 0 ? "Main" : $"Interrupt {i}";

				if (i == state.Count - 1) {
					label += " (audible)";
				}

				EditorGUILayout.LabelField(label, $"{soundName}  {GetStatus(state.GetHandle(i))}");
			}
		}

		private static string GetStatus(ThunkHandle handle)
		{
			if (handle.IsNone) {
				return "waiting";
			}

			if (handle.IsPaused) {
				return "paused";
			}

			return handle.IsPlaying ? "playing" : "ended";
		}
	}
}
