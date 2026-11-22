using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk
{
	/// <summary>
	/// Auditions a clip in edit mode. It plays through its own hidden AudioSource and never touches ThunkRuntime.
	/// </summary>
	public static class ThunkPreview
	{
		public static bool IsPlaying => s_source != null && s_source.isPlaying;

		private static GameObject s_object;
		private static AudioSource s_source;

		public static void Play(AudioClip clip, float volume, float pitch, bool loop, AudioMixerGroup group)
		{
			if (clip == null) {
				return;
			}

			EnsureSource();

			s_source.Stop();
			s_source.clip = clip;
			s_source.volume = volume;
			s_source.pitch = pitch;
			s_source.loop = loop;
			s_source.outputAudioMixerGroup = group;
			s_source.Play();
		}

		public static void Stop()
		{
			if (s_source != null) {
				s_source.Stop();
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

		private static void EnsureSource()
		{
			if (s_source != null) {
				return;
			}

			s_object = new GameObject("[Thunk] Preview") { hideFlags = HideFlags.HideAndDontSave };
			s_source = s_object.AddComponent<AudioSource>();
			s_source.playOnAwake = false;
			s_source.spatialBlend = 0f;
		}

		private static void Cleanup()
		{
			if (s_object != null) {
				Object.DestroyImmediate(s_object);
			}

			s_object = null;
			s_source = null;
		}
	}
}
