using UnityEngine;

namespace Tekly.Thunk.Core
{
	public class ThunkAudioSource
	{
		public bool IsPlaying => m_playing && (m_audioSource.isPlaying || Thunk.Instance.Paused);
		public float TimeRemaining => m_audioSource.clip.length - m_audioSource.time;
		
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
		
		private readonly ThunkEmitter m_emitter;
		private readonly AudioSource m_audioSource;
		private readonly Transform m_transform;

		private float m_volume = 1f;
		private float m_pitch = 1f;
		private bool m_playing;
		
		public ThunkAudioSource(ThunkEmitter emitter)
		{
			m_emitter = emitter;
			m_audioSource = emitter.CreateAudioSource();
			m_transform = m_audioSource.transform;
		}

		public void Play(ThunkClipRequest request)
		{
			Clip = request.Source.GetClip();
			
			if (request.Pitch != null) {
				Pitch = request.Pitch.Value;
			} else {
				Pitch = request.Source.Pitch;
			}
			
			if (request.Volume != null) {
				Volume = request.Volume.Value;
			} else {
				Volume = request.Source.Volume;
			}

			Loop = request.Source.Loop;
			m_audioSource.outputAudioMixerGroup = request.Source.MixerGroup;
			
			m_playing = true;
			
			if (request.Delay != null) {
				m_audioSource.PlayDelayed(request.Delay.Value);	
			} else {
				m_audioSource.Play();
			}
		}
		
		public void PlayDelayed(float delay)
		{
			m_playing = true;
			m_audioSource.PlayDelayed(delay);
		}
		
		public void Free()
		{
			m_audioSource.Stop();
			
			Clip = null;
			Loop = false;
			
			m_volume = 1;
			m_pitch = 1;
			
			m_audioSource.volume = 1;
			m_audioSource.pitch = 1;
			
			m_playing = false;
			
			m_audioSource.outputAudioMixerGroup = null;
		}

		public void Stop()
		{
			m_audioSource.Stop();
			m_playing = false;
		}
	}
}