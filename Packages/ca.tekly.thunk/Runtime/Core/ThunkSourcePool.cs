using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// The AudioSources made from one template, kept for reuse. Each is a copy of the template's whole GameObject,
	/// so filters and anything else next to the template's AudioSource come along with it. The pool with no
	/// template holds plain AudioSources for sounds that are not positioned.
	/// </summary>
	internal sealed class ThunkSourcePool
	{
		public readonly AudioSource Template;
		public readonly bool HasTemplate;

		/// <summary>
		/// How many of the pool's sources are playing.
		/// </summary>
		public int Rented => m_rented;

		/// <summary>
		/// How many of the pool's sources are waiting to be used.
		/// </summary>
		public int Free => m_free.Count;

		/// <summary>
		/// True once the template the pool was made from has been destroyed. Unity null check.
		/// </summary>
		public bool IsDead => HasTemplate && Template == null;

		private readonly Stack<ThunkSource> m_free = new Stack<ThunkSource>();
		private readonly ThunkRuntime m_runtime;
		private int m_rented;

		public ThunkSourcePool(ThunkRuntime runtime, AudioSource template)
		{
			m_runtime = runtime;
			Template = template;
			HasTemplate = !ReferenceEquals(template, null);
		}

		public ThunkSource Rent()
		{
			m_rented++;

			ThunkSource source;

			do {
				if (!m_free.TryPop(out source)) {
					return Create();
				}

				// Unity null check: a waiting source is dropped if something destroyed its AudioSource
			} while (source.Audio == null);

#if UNITY_EDITOR
			// Recopied so a template's 3D settings can be tuned while the game runs. A newly made source is already up to date.
			if (HasTemplate && Template != null) {
				ThunkSpatialTemplate.Copy(Template, source.Audio);
			}
#endif

			return source;
		}

		public void Return(ThunkSource source)
		{
			m_rented--;

			var audio = source.Audio;

			// Unity null check: the AudioSource is gone if something destroyed the pooled object
			if (audio == null) {
				return;
			}

			// Nothing will ask for a copy of a template that no longer exists
			if (IsDead) {
				ThunkRuntime.DestroyObject(audio.gameObject);
				return;
			}

			audio.Stop();
			audio.clip = null;
			audio.loop = false;
			audio.outputAudioMixerGroup = null;

			source.Gain = 1f;
			source.PitchScale = 1f;
			source.StartDsp = 0d;
			source.FadeDuration = 0f;
			m_free.Push(source);
		}

		/// <summary>
		/// Destroys the sources that are waiting to be used.
		/// </summary>
		public void Clear()
		{
			while (m_free.TryPop(out var source)) {
				if (source.Audio != null) {
					ThunkRuntime.DestroyObject(source.Audio.gameObject);
				}
			}
		}

		private ThunkSource Create()
		{
			AudioSource audio;

			// Unity null check: a pool that outlives its template falls back to plain AudioSources
			if (HasTemplate && Template != null) {
				// Instantiating a component copies its whole GameObject and returns the matching component
				audio = Object.Instantiate(Template, m_runtime.GetRoot());
				audio.name = Template.name;
			} else {
				var gameObject = new GameObject("Source");
				gameObject.transform.SetParent(m_runtime.GetRoot(), false);
				audio = gameObject.AddComponent<AudioSource>();
			}

			// The sound decides what plays and when, not the template
			audio.playOnAwake = false;
			audio.Stop();
			audio.clip = null;
			audio.loop = false;
			audio.mute = false;
			audio.enabled = true;
			audio.gameObject.SetActive(true);

			return new ThunkSource(audio, this);
		}
	}
}
