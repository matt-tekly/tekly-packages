using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkPlayer))]
	[CanEditMultipleObjects]
	public class ThunkPlayerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space();

			EditorGUI.BeginDisabledGroup(!Application.isPlaying);

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Play")) {
					foreach (var targetObject in targets) {
						var player = targetObject as ThunkPlayer;

						if (player != null) {
							player.Play();
						}
					}
				}

				if (GUILayout.Button("Stop")) {
					foreach (var targetObject in targets) {
						var player = targetObject as ThunkPlayer;

						if (player != null) {
							player.Stop();
						}
					}
				}

				if (GUILayout.Button("Release")) {
					foreach (var targetObject in targets) {
						var player = targetObject as ThunkPlayer;

						if (player != null) {
							player.Release();
						}
					}
				}
			}

			EditorGUI.EndDisabledGroup();
		}
	}
}
