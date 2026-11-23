using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tekly.Thunk
{
	[CustomEditor(typeof(ThunkTimeline))]
	[CanEditMultipleObjects]
	public class ThunkTimelineEditor : ThunkSoundEditor
	{
		private const float RULER_HEIGHT = 16f;
		private const float ROW_HEIGHT = 12f;
		private const float GUTTER_WIDTH = 18f;
		private const float PADDING = 4f;
		private const float TICK_SPACING = 56f;
		private const float MIN_BAR_WIDTH = 2f;
		private const float MIN_DURATION = 0.1f;

		private static readonly float[] s_tickSteps = { 0.05f, 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 30f, 60f };
		private static readonly GUIContent s_clipsLabel = new GUIContent("Clips", "One of these is picked each time the timeline plays.");

		private static readonly Color s_barColor = new Color(0.30f, 0.62f, 0.95f);
		private static readonly Color s_emptyColor = new Color(0.92f, 0.55f, 0.20f);

		private readonly List<float> m_starts = new List<float>();
		private readonly List<float> m_lengths = new List<float>();

		private SerializedProperty m_entries;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_entries = serializedObject.FindProperty("m_entries");
		}

		protected override void DrawContent()
		{
			DrawStrip();

			var remove = -1;

			for (var i = 0; i < m_entries.arraySize; i++) {
				var entry = m_entries.GetArrayElementAtIndex(i);

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
					using (new EditorGUILayout.HorizontalScope()) {
						GUILayout.Label("Entry " + (i + 1), EditorStyles.boldLabel);
						GUILayout.FlexibleSpace();

						if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(60f))) {
							remove = i;
						}
					}

					EditorGUILayout.PropertyField(entry.FindPropertyRelative("Delay"));
					EditorGUILayout.PropertyField(entry.FindPropertyRelative("VolumeOffset"));
					EditorGUILayout.PropertyField(entry.FindPropertyRelative("Semitones"));

					EditorGUILayout.Space(2f);
					ClipSets.Draw(entry.FindPropertyRelative("Clips"), s_clipsLabel);
				}
			}

			if (remove >= 0) {
				m_entries.DeleteArrayElementAtIndex(remove);
			}

			if (GUILayout.Button("Add Entry")) {
				AddEntry();
			}
		}

		protected override string GetPreviewProblem()
		{
			var entries = ((ThunkTimeline) target).Entries;

			if (entries != null) {
				for (var i = 0; i < entries.Length; i++) {
					if (entries[i].Clips.Count > 0) {
						return null;
					}
				}
			}

			return "A timeline needs at least one entry with clips to preview.";
		}

		protected override void Preview()
		{
			var timeline = (ThunkTimeline) target;
			var volume = timeline.Volume.Get();
			var pitch = timeline.Pitch.Get();
			var entries = timeline.Entries;

			ThunkPreview.BeginTimeline();

			for (var i = 0; i < entries.Length; i++) {
				ref var entry = ref entries[i];
				var clip = Record.Select(i, entry.Clips);

				ThunkPreview.AddTimelineEntry(
					clip,
					entry.Delay,
					volume * Mathf.Clamp01(1f + entry.VolumeOffset),
					pitch * Mathf.Pow(2f, entry.Semitones / 12f),
					timeline.MixerGroup);
			}
		}

		private void AddEntry()
		{
			var index = m_entries.arraySize;
			m_entries.arraySize++;

			// Growing the array copies its last element. The new entry keeps that delay as a starting point and
			// is otherwise empty.
			var entry = m_entries.GetArrayElementAtIndex(index);
			entry.FindPropertyRelative("VolumeOffset").floatValue = 0f;
			entry.FindPropertyRelative("Semitones").floatValue = 0f;
			entry.FindPropertyRelative("Clips.Clips").arraySize = 0;
			entry.FindPropertyRelative("Clips.Mode").enumValueIndex = 0;
		}

		/// <summary>
		/// Draws each entry as a bar that starts at its delay and is as long as its longest clip at the entry's
		/// pitch, so overlaps and gaps can be seen. The sound's own pitch range is not included. An entry with no
		/// clips is drawn as a marker in another color.
		/// </summary>
		private void DrawStrip()
		{
			var count = m_entries.arraySize;

			if (count == 0) {
				return;
			}

			var duration = Measure();
			var rect = GUILayoutUtility.GetRect(0f, RULER_HEIGHT + count * ROW_HEIGHT + PADDING, GUILayout.ExpandWidth(true));

			if (Event.current.type != EventType.Repaint) {
				return;
			}

			var skin = EditorGUIUtility.isProSkin;
			var background = skin ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.72f, 0.72f, 0.72f);
			var line = skin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.12f);

			EditorGUI.DrawRect(rect, background);

			var left = rect.x + GUTTER_WIDTH;
			var width = rect.width - GUTTER_WIDTH - PADDING;
			var step = PickStep(duration, width);

			for (var time = 0f; time <= duration + step * 0.01f; time += step) {
				var x = left + time / duration * width;

				EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), line);

				// A label that would run off the end is left out
				if (x + 36f <= rect.xMax) {
					GUI.Label(new Rect(x + 2f, rect.y, 36f, RULER_HEIGHT), time.ToString("0.##") + "s", EditorStyles.miniLabel);
				}
			}

			for (var i = 0; i < count; i++) {
				var y = rect.y + RULER_HEIGHT + i * ROW_HEIGHT;
				var x = left + m_starts[i] / duration * width;
				var hasClips = m_lengths[i] > 0f;
				var barWidth = hasClips ? Mathf.Max(MIN_BAR_WIDTH, m_lengths[i] / duration * width) : MIN_BAR_WIDTH;

				GUI.Label(new Rect(rect.x + 2f, y - 2f, GUTTER_WIDTH, ROW_HEIGHT + 4f), (i + 1).ToString(), EditorStyles.miniLabel);
				EditorGUI.DrawRect(new Rect(x, y + 1f, barWidth, ROW_HEIGHT - 2f), hasClips ? s_barColor : s_emptyColor);
			}
		}

		/// <summary>
		/// Works out where each entry starts and how long it is. Returns how long the whole timeline is.
		/// </summary>
		private float Measure()
		{
			m_starts.Clear();
			m_lengths.Clear();

			var duration = MIN_DURATION;

			for (var i = 0; i < m_entries.arraySize; i++) {
				var entry = m_entries.GetArrayElementAtIndex(i);
				var clips = entry.FindPropertyRelative("Clips.Clips");
				var start = Mathf.Max(0f, entry.FindPropertyRelative("Delay").floatValue);
				var longest = 0f;

				for (var clipIndex = 0; clipIndex < clips.arraySize; clipIndex++) {
					if (clips.GetArrayElementAtIndex(clipIndex).objectReferenceValue is AudioClip clip) {
						longest = Mathf.Max(longest, clip.length);
					}
				}

				var length = longest / Mathf.Pow(2f, entry.FindPropertyRelative("Semitones").floatValue / 12f);

				m_starts.Add(start);
				m_lengths.Add(length);
				duration = Mathf.Max(duration, start + length);
			}

			return duration;
		}

		/// <summary>
		/// The smallest gap between ruler marks that keeps them from crowding each other.
		/// </summary>
		private static float PickStep(float duration, float width)
		{
			var maxTicks = Mathf.Max(1f, width / TICK_SPACING);

			for (var i = 0; i < s_tickSteps.Length; i++) {
				if (duration / s_tickSteps[i] <= maxTicks) {
					return s_tickSteps[i];
				}
			}

			return s_tickSteps[s_tickSteps.Length - 1];
		}
	}
}
