using System.Collections.Generic;
using System.Linq;
using Tekly.Lofi.Core;
using UnityEngine;

namespace Tekly.Lofi.Emitters
{
	/// <summary>
	/// An Emitter represents an object that will play audio using LofiClips
	/// </summary>
	public class LofiEmitter : MonoBehaviour
	{
		public float Volume { get; set; } = 1f;
		public float Pitch { get; set; } = 1f;

		public bool IsPlaying {
			get {
				for (var index = 0; index < m_runners.Count; index++) {
					var runner = m_runners[index];
					if (!runner.IsComplete) {
						return true;
					}
				}

				return false;
			}
		}

		[SerializeField] private AudioSource m_audioSourceTemplate;
		private List<LofiClipRunner> m_runners = new List<LofiClipRunner>();
		private AudioSourcePool m_audioSourcePool;
		
		private bool m_initialized;

		public LofiAudioSource GetAudioSource()
		{
			Init();
			return m_audioSourcePool.Claim();
		}

		public void FreeAudioSource(LofiAudioSource audioSource)
		{
			Init();
			m_audioSourcePool.Free(audioSource);
		}

		internal AudioSource CreateAudioSource()
		{
			var instance = gameObject.AddComponent<AudioSource>();

			if (m_audioSourceTemplate != null) {
				CopyProperties(m_audioSourceTemplate, instance);
			}
			
			return instance;
		}
		
		private void Init()
		{
			if (m_initialized) {
				return;
			}
			
			m_audioSourcePool = new AudioSourcePool(this);
			m_initialized = true;
		}
		
		private void OnDestroy()
		{
			if (!m_initialized) {
				return;
			}

			for (var index = m_runners.Count - 1; index >= 0; index--)
			{
				var runner = m_runners[index];
				runner.Stop();
				runner.Dispose();
			}
			
			m_runners.Clear();
		}
		
		public int Play(string id)
		{
			if (Lofi.Core.Lofi.Instance.TryGetClip(id, out var clip)) {
				return Play(clip);
			}
			
			return Constants.INVALID_ID;
		}
		
		public int Play(LofiClip clip)
		{
			Init();
			
			if (clip.CanRun) {
				var runner = clip.CreateRunner(this);
				m_runners.Add(runner);
				
				return runner.Id;
			}

			return Constants.INVALID_ID;
		}
		
		public int Play(LofiClip clip, LofiClipRunnerData runnerData)
		{
			Init();
			
			if (clip.CanRun) {
				var runner = clip.CreateRunner(this, runnerData);
				m_runners.Add(runner);

				return runner.Id;
			}
			
			return Constants.INVALID_ID;
		}

		public void Stop(int runnerId)
		{
			var runner = GetRunner(runnerId);
			runner?.Stop();
		}
		
		public void FadeIn(int runnerId, float duration)
		{
			var runner = GetRunner(runnerId);
			runner?.FadeIn(duration);
		}
		
		public void FadeOut(int runnerId, float duration)
		{
			var runner = GetRunner(runnerId);
			runner?.FadeOut(duration);
		}
		
		public void FadeToDuration(int runnerId, float volume, float duration)
		{
			var runner = GetRunner(runnerId);
			runner?.FadeToDuration(volume, duration);
		}
		
		public void FadeToSpeed(int runnerId, float volume, float speed)
		{
			var runner = GetRunner(runnerId);
			runner?.FadeToSpeed(volume, speed);
		}

		public float GetTimeRemaining(int runnerId)
		{
			var runner = GetRunner(runnerId);
			return runner?.TimeRemaining ?? 0;
		}

		private void Update()
		{
			for (var index = m_runners.Count - 1; index >= 0; index--) {
				var runner = m_runners[index];
				runner.Update();
				
				if (runner.IsComplete) {
					m_runners.RemoveAt(index);
					runner.Dispose();
				}
			}
		}

		private LofiClipRunner GetRunner(int id)
		{
			var runner = m_runners.FirstOrDefault(x => x.Id == id);

			if (runner == null) {
				Debug.LogWarning($"Failed to find runner with id: [{id}]");
			}

			return runner;
		}

		private static void CopyProperties(AudioSource source, AudioSource destination)
		{
			destination.bypassEffects = source.bypassEffects;
			destination.bypassListenerEffects = source.bypassListenerEffects;
			destination.bypassReverbZones = source.bypassReverbZones;
			destination.priority = source.priority;
			
			destination.panStereo = source.panStereo;
			destination.spatialBlend = source.spatialBlend;
			
			destination.reverbZoneMix = source.reverbZoneMix;
			destination.dopplerLevel = source.dopplerLevel;
			
			destination.rolloffMode = source.rolloffMode;
			destination.minDistance = source.minDistance;
			destination.maxDistance = source.maxDistance;
			destination.spatialize = source.spatialize;
			destination.spatializePostEffects = source.spatializePostEffects;
			
			destination.spread = source.spread;
			
			destination.SetCustomCurve(AudioSourceCurveType.CustomRolloff, source.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
			destination.SetCustomCurve(AudioSourceCurveType.SpatialBlend, source.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
			destination.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, source.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
			destination.SetCustomCurve(AudioSourceCurveType.Spread, source.GetCustomCurve(AudioSourceCurveType.Spread));
		}

		private class AudioSourcePool
		{
			private readonly LofiEmitter m_emitter;
		
			private readonly List<LofiAudioSource> m_all = new List<LofiAudioSource>();
			private readonly Stack<LofiAudioSource> m_free = new Stack<LofiAudioSource>();
		
			public AudioSourcePool(LofiEmitter emitter)
			{
				m_emitter = emitter;
			}

			public LofiAudioSource Claim()
			{
				if (!m_free.TryPop(out var audioSource)) {
					audioSource = new LofiAudioSource(m_emitter);
					m_all.Add(audioSource);
				}	
			
				return audioSource;
			}

			public void Free(LofiAudioSource audioSource)
			{
				m_free.Push(audioSource);
				audioSource.Free();
			}
		}
	}
}