using System.Collections.Generic;
using System.Linq;
using Tekly.Lofi.Core;
using UnityEngine;

namespace Tekly.Lofi.Emitters
{
	public class LofiEmitter : MonoBehaviour
	{
		public float Volume { get; set; } = 1f;
		public float Pitch { get; set; } = 1f;

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
		
		private void Init()
		{
			if (m_initialized) {
				return;
			}
			
			m_audioSourcePool = new AudioSourcePool(this);
			m_initialized = true;
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
			
			var runner = clip.CreateRunner(this, runnerData);
			m_runners.Add(runner);
			
			return runner.Id;
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
			return m_runners.FirstOrDefault(x => x.Id == id);
		}
	}

	public class AudioSourcePool
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