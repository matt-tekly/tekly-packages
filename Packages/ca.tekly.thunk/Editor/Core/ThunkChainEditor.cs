using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkChain))]
	[CanEditMultipleObjects]
	public class ThunkChainEditor : ThunkSoundEditor
	{
		private const int SET_START = 0;
		private const int SET_LOOP = 1;
		private const int SET_END = 2;

		protected override bool CanRelease => true;

		private SerializedProperty m_start;
		private SerializedProperty m_loop;
		private SerializedProperty m_end;
		private SerializedProperty m_endCrossfade;

		private GUIContent m_startLabel;
		private GUIContent m_loopLabel;
		private GUIContent m_endLabel;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_start = serializedObject.FindProperty("m_start");
			m_loop = serializedObject.FindProperty("m_loop");
			m_end = serializedObject.FindProperty("m_end");
			m_endCrossfade = serializedObject.FindProperty("m_endCrossfade");

			m_startLabel = new GUIContent("Start", m_start.tooltip);
			m_loopLabel = new GUIContent("Loop", m_loop.tooltip);
			m_endLabel = new GUIContent("End", m_end.tooltip);
		}

		protected override void DrawContent()
		{
			ClipSets.Draw(m_start, m_startLabel);
			EditorGUILayout.Space(2f);
			ClipSets.Draw(m_loop, m_loopLabel);
			EditorGUILayout.Space(2f);
			ClipSets.Draw(m_end, m_endLabel);

			// There is nothing to crossfade into without an end
			if (m_end.FindPropertyRelative("Clips").arraySize > 0) {
				EditorGUILayout.PropertyField(m_endCrossfade);
			}
		}

		protected override string GetPreviewProblem()
		{
			return ((ThunkChain) target).Loop.Count == 0 ? "A chain needs a loop. Add at least one clip to the Loop set." : null;
		}

		protected override void Preview()
		{
			var chain = (ThunkChain) target;
			var start = Record.Select(SET_START, chain.Start);
			var loop = Record.Select(SET_LOOP, chain.Loop);

			ThunkPreview.PlayChain(start, loop, chain.Volume.Get(), chain.Pitch.Get(), chain.MixerGroup);
		}

		protected override void Release()
		{
			var chain = (ThunkChain) target;
			var end = Record.Select(SET_END, chain.End);

			ThunkPreview.ReleaseChain(end, chain.EndCrossfade);
		}
	}
}
