using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkChain))]
	[CanEditMultipleObjects]
	public class ThunkChainEditor : Editor
	{
		private const int SET_START = 0;
		private const int SET_LOOP = 1;
		private const int SET_END = 2;

		// Kept between presses so that exhaustive and non-repeating selection behave as they do at runtime
		private readonly ThunkSoundRecord m_record = new ThunkSoundRecord();

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var chain = target as ThunkChain;

			if (chain == null) {
				return;
			}

			EditorGUILayout.Space();

			if (chain.Loop.Count == 0) {
				EditorGUILayout.HelpBox("A chain needs a loop. Add at least one clip to the Loop set.", MessageType.Warning);
				return;
			}

			using (new EditorGUILayout.HorizontalScope()) {
				if (GUILayout.Button("Preview")) {
					Preview(chain);
				}

				if (GUILayout.Button("Release")) {
					var end = m_record.Select(SET_END, chain.End);
					ThunkPreview.ReleaseChain(end, chain.EndCrossfade);
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

		private void Preview(ThunkChain chain)
		{
			var start = m_record.Select(SET_START, chain.Start);
			var loop = m_record.Select(SET_LOOP, chain.Loop);

			ThunkPreview.PlayChain(start, loop, chain.Volume.Get(), chain.Pitch.Get(), chain.MixerGroup);
		}
	}
}
