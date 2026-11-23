using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk
{
	/// <summary>
	/// Auditions sounds in edit mode. It plays through its own hidden AudioSources and never touches ThunkRuntime.
	/// </summary>
	public static class ThunkPreview
	{
		public static bool IsPlaying {
			get {
				for (var i = 0; i < s_sources.Length; i++) {
					if (s_sources[i] != null && s_sources[i].isPlaying) {
						return true;
					}
				}

				for (var i = 0; i < s_timelineUsed; i++) {
					if (s_timelineSources[i] != null && s_timelineSources[i].isPlaying) {
						return true;
					}
				}

				return false;
			}
		}

		private const int SOURCE_START = 0;
		private const int SOURCE_LOOP = 1;
		private const int SOURCE_END = 2;

		// Scheduling slightly ahead gives the audio thread time to line both sources up
		private const double SCHEDULE_LEAD = 0.03d;

		private static readonly AudioSource[] s_sources = new AudioSource[3];

		private static readonly List<AudioSource> s_timelineSources = new List<AudioSource>();

		private static GameObject s_object;
		private static int s_timelineUsed;
		private static bool s_fading;
		private static double s_fadeBegin;
		private static float s_fadeDuration;
		private static float s_fadeVolume;

		public static void Play(AudioClip clip, float volume, float pitch, bool loop, AudioMixerGroup group)
		{
			if (clip == null) {
				return;
			}

			Stop();

			var source = Configure(SOURCE_START, clip, volume, pitch, loop, group);
			source.Play();
		}

		public static void PlayChain(AudioClip start, AudioClip loop, float volume, float pitch, AudioMixerGroup group)
		{
			if (loop == null) {
				return;
			}

			Stop();

			if (start == null) {
				Configure(SOURCE_LOOP, loop, volume, pitch, true, group).Play();
				return;
			}

			var startSource = Configure(SOURCE_START, start, volume, pitch, false, group);
			var loopSource = Configure(SOURCE_LOOP, loop, volume, pitch, true, group);

			var begin = AudioSettings.dspTime + SCHEDULE_LEAD;

			startSource.PlayScheduled(begin);
			loopSource.PlayScheduled(begin + start.samples / (start.frequency * (double) Mathf.Max(Mathf.Abs(pitch), 0.01f)));
		}

		public static void BeginTimeline()
		{
			Stop();
			s_timelineUsed = 0;
		}

		public static void AddTimelineEntry(AudioClip clip, float delay, float volume, float pitch, AudioMixerGroup group)
		{
			if (clip == null) {
				return;
			}

			var source = GetTimelineSource(s_timelineUsed);
			s_timelineUsed++;

			source.Stop();
			source.clip = clip;
			source.volume = volume;
			source.pitch = pitch;
			source.loop = false;
			source.outputAudioMixerGroup = group;

			if (delay <= 0f) {
				source.Play();
			} else {
				source.PlayScheduled(AudioSettings.dspTime + delay);
			}
		}

		public static void ReleaseChain(AudioClip end, float crossfade)
		{
			if (end == null) {
				Stop();
				return;
			}

			var startSource = s_sources[SOURCE_START];
			var loopSource = s_sources[SOURCE_LOOP];

			// Nothing is playing, so there is nothing to release and the end clip would play by itself
			if (!IsPlaying || startSource == null && loopSource == null) {
				return;
			}

			var reference = loopSource != null ? loopSource : startSource;
			var endSource = Configure(SOURCE_END, end, reference.volume, reference.pitch, false, reference.outputAudioMixerGroup);
			endSource.Play();

			if (crossfade <= 0f) {
				StopSource(SOURCE_START);
				StopSource(SOURCE_LOOP);
				return;
			}

			s_fadeVolume = reference.volume;
			s_fadeDuration = crossfade;
			s_fadeBegin = EditorApplication.timeSinceStartup;

			if (!s_fading) {
				s_fading = true;
				EditorApplication.update += OnUpdate;
			}
		}

		public static void Stop()
		{
			CancelFade();

			for (var i = 0; i < s_sources.Length; i++) {
				StopSource(i);
			}

			for (var i = 0; i < s_timelineSources.Count; i++) {
				if (s_timelineSources[i] != null) {
					s_timelineSources[i].Stop();
				}
			}
		}

		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
			AssemblyReloadEvents.beforeAssemblyReload += Cleanup;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingEditMode) {
				Cleanup();
			}
		}

		private static void OnUpdate()
		{
			var ratio = Mathf.Clamp01((float) ((EditorApplication.timeSinceStartup - s_fadeBegin) / s_fadeDuration));

			if (ratio >= 1f) {
				CancelFade();
				StopSource(SOURCE_START);
				StopSource(SOURCE_LOOP);
				return;
			}

			var volume = s_fadeVolume * (1f - ratio);

			for (var i = SOURCE_START; i <= SOURCE_LOOP; i++) {
				if (s_sources[i] != null) {
					s_sources[i].volume = volume;
				}
			}
		}

		private static void CancelFade()
		{
			if (!s_fading) {
				return;
			}

			s_fading = false;
			EditorApplication.update -= OnUpdate;
		}

		private static void StopSource(int index)
		{
			if (s_sources[index] != null) {
				s_sources[index].Stop();
			}
		}

		private static AudioSource Configure(int index, AudioClip clip, float volume, float pitch, bool loop, AudioMixerGroup group)
		{
			var source = GetSource(index);

			source.Stop();
			source.clip = clip;
			source.volume = volume;
			source.pitch = pitch;
			source.loop = loop;
			source.outputAudioMixerGroup = group;

			return source;
		}

		private static AudioSource GetSource(int index)
		{
			if (s_object == null) {
				s_object = new GameObject("[Thunk] Preview") { hideFlags = HideFlags.HideAndDontSave };
			}

			if (s_sources[index] == null) {
				var source = s_object.AddComponent<AudioSource>();
				source.playOnAwake = false;
				source.spatialBlend = 0f;
				s_sources[index] = source;
			}

			return s_sources[index];
		}

		private static AudioSource GetTimelineSource(int index)
		{
			if (s_object == null) {
				s_object = new GameObject("[Thunk] Preview") { hideFlags = HideFlags.HideAndDontSave };
			}

			if (index < s_timelineSources.Count && s_timelineSources[index] != null) {
				return s_timelineSources[index];
			}

			var source = s_object.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = 0f;

			if (index < s_timelineSources.Count) {
				s_timelineSources[index] = source;
			} else {
				s_timelineSources.Add(source);
			}

			return source;
		}

		private static void Cleanup()
		{
			CancelFade();

			if (s_object != null) {
				Object.DestroyImmediate(s_object);
			}

			s_object = null;
			s_timelineSources.Clear();
			s_timelineUsed = 0;

			for (var i = 0; i < s_sources.Length; i++) {
				s_sources[i] = null;
			}
		}
	}
}
