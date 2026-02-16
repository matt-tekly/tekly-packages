using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Thunk.Core
{
	/// <summary>
	/// An Emitter represents an object that will play audio using ThunkClips
	/// </summary>
	public class ThunkEmitter : MonoBehaviour
	{
		// TODO: This only works at the time a clip is starting
		public float Volume { get; set; } = 1f;
		
		// TODO: This only works at the time a clip is starting
		public float Pitch { get; set; } = 1f;
		
		[SerializeField] private AudioSource m_audioSourceTemplate;
		
		private readonly List<ThunkClipInstance> m_instances = new List<ThunkClipInstance>();
		private AudioSourcePool m_audioSourcePool;
		
		private bool m_initialized;

		public ThunkAudioSource GetAudioSource()
		{
			Init();
			return m_audioSourcePool.Get();
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

			for (var index = m_instances.Count - 1; index >= 0; index--)
			{
				var runner = m_instances[index];
				runner.OnEmitterStopped();
			}
			
			m_instances.Clear();
		}
		
		public int Play(ThunkClip clip)
		{
			Init();

			var instance = clip.State.Play(this);

			if (instance == null) {
				return Thunk.INVALID_ID;
			}

			m_instances.Add(instance);
			return instance.Id;
		}
		
		public void Stop(int instanceId)
		{
			for (var index = 0; index < m_instances.Count; index++) {
				var instance = m_instances[index];
				
				if (instance.Id == instanceId) {
					instance.OnEmitterStopped();
					m_instances.RemoveAt(index);
					m_audioSourcePool.Return(instance.AudioSource);
					return;
				}
			}
		}
		
		public void SetPaused(int instanceId, bool paused)
		{
			if (TryGetInstance(instanceId, out var instance)) {
				instance.AudioSource.Paused = paused;
			}
		}

		public bool TryGetInstance(int instanceId, out ThunkClipInstance instance)
		{
			for (var index = 0; index < m_instances.Count; index++) {
				var target = m_instances[index];
				
				if (target.Id == instanceId) {
					instance = target;
					return true;
				}
			}
			
			instance = null;
			return false;
		}

		public void ClipInstanceDisposed(ThunkClipInstance thunkClipInstance)
		{
			for (var index = 0; index < m_instances.Count; index++) {
				var instance = m_instances[index];
				
				if (instance.Id == thunkClipInstance.Id) {
					m_instances.RemoveAt(index);
					m_audioSourcePool.Return(instance.AudioSource);
					return;
				}
			}
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
			private readonly ThunkEmitter m_emitter;
			private readonly Stack<ThunkAudioSource> m_free = new Stack<ThunkAudioSource>();

			public AudioSourcePool(ThunkEmitter emitter)
			{
				m_emitter = emitter;
			}

			public ThunkAudioSource Get()
			{
				if (!m_free.TryPop(out var audioSource)) {
					audioSource = new ThunkAudioSource(m_emitter);
				}

				return audioSource;
			}

			public void Return(ThunkAudioSource audioSource)
			{
				m_free.Push(audioSource);
				audioSource.Free();
			}
		}

	
	}
}