using Tekly.Lofi.Core;
using UnityEngine;

namespace Tekly.Lofi.Emitters
{
	public class LofiAudioSource
	{
		private readonly LofiEmitter m_emitter;
		private readonly AudioSource m_audioSource;
		private readonly Transform m_transform;

		private float m_volume = 1f;
		private float m_pitch = 1f;
		private bool m_playing;

		private LofiMixerGroup m_mixerGroup;
		
		public bool IsPlaying => m_playing && (m_audioSource.isPlaying || Core.Lofi.Instance.Paused);
		
		public bool Loop {
			get => m_audioSource.loop;
			set => m_audioSource.loop = value;
		}
		
		public float Volume {
			get => m_volume;
			set {
				m_volume = value;
				m_audioSource.volume = m_volume * m_emitter.Volume;
			}
		}

		public float Pitch {
			get => m_pitch;
			set {
				m_pitch = value;
				m_audioSource.pitch = m_pitch * m_emitter.Pitch;
			}
		}

		public Vector3 Position {
			get => m_transform.position;
			set => m_transform.position = value;
		}

		public AudioClip Clip {
			get => m_audioSource.clip;
			set => m_audioSource.clip = value;
		}
		
		public LofiMixerGroup MixerGroup {
			get => m_mixerGroup;
			set {
				m_mixerGroup = value;
				if (m_mixerGroup != null) {
					m_audioSource.outputAudioMixerGroup = m_mixerGroup.MixerGroup;
					m_audioSource.ignoreListenerPause = m_mixerGroup.IgnoreListenerPause;
				} else {
					m_audioSource.outputAudioMixerGroup = null;
					m_audioSource.ignoreListenerPause = false;
				}
			}
		}

		public LofiAudioSource(LofiEmitter emitter)
		{
			m_emitter = emitter;
			m_audioSource = emitter.CreateAudioSource();
			m_transform = m_audioSource.transform;
		}

		public void Play()
		{
			m_playing = true;
			m_audioSource.Play();
		}
		
		public void PlayDelayed(float delay)
		{
			m_playing = true;
			m_audioSource.PlayDelayed(delay);
		}
		
		public void Free()
		{
			Clip = null;
			MixerGroup = null;
			Loop = false;
			
			m_audioSource.Stop();
			
			m_volume = 1;
			m_pitch = 1;
			m_playing = false;
		}

		public void Stop()
		{
			m_audioSource.Stop();
			m_playing = false;
		}
	}
}