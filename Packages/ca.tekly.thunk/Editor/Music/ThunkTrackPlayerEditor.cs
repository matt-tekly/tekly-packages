using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkTrackPlayer))]
	[CanEditMultipleObjects]
	public class ThunkTrackPlayerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space();

			EditorGUI.BeginDisabledGroup(!Application.isPlaying);

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Play")) {
					foreach (var targetObject in targets) {
						var player = targetObject as ThunkTrackPlayer;

						if (player != null) {
							player.Play();
						}
					}
				}

				if (GUILayout.Button("Stop")) {
					foreach (var targetObject in targets) {
						var player = targetObject as ThunkTrackPlayer;

						if (player != null) {
							player.Stop();
						}
					}
				}
			}

			EditorGUI.EndDisabledGroup();
		}
	}
}
