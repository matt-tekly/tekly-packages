#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk.Music
{
	[CustomEditor(typeof(ThunkTrackPlayer))]
	[CanEditMultipleObjects]
	public sealed class ThunkTrackPlayerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (!Application.isPlaying) {
				return;
			}

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Play")) {
					foreach (var t in targets) {
						var player = (ThunkTrackPlayer)t;
						player.Play();
					}
				}

				if (GUILayout.Button("Pop")) {
					foreach (var t in targets) {
						var player = (ThunkTrackPlayer)t;
						player.Pop();
					}
				}
			}
		}
	}
}
#endif