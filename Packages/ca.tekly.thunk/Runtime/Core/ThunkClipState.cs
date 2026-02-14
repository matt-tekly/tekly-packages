using System;
using System.Collections.Generic;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk.Core
{
	public class ThunkClipState
	{
		public float Pitch => m_clip.Pitch.Get();
		public float Volume => m_clip.Volume.Get();
		public bool Loop => m_clip.Loop;
		public AudioMixerGroup MixerGroup => m_clip.MixerGroup;

		private readonly ThunkClip m_clip;
		private readonly List<ThunkClipInstance> m_instances = new List<ThunkClipInstance>();
		
		protected int m_nextId;
		protected float m_nextPlayTime;
		protected RandomSelector64 m_randomSelector;
		
		public ThunkClipState(ThunkClip clip)
		{
			m_clip = clip;
			m_randomSelector = new RandomSelector64(clip.Clips.Length, clip.RandomMode);
		}

		public virtual ThunkClipInstance Play(ThunkEmitter emitter)
		{
			if (Time.time < m_nextPlayTime) {
				return null;
			}
			
			if (m_instances.Count >= m_clip.InstanceCapacity) {
				switch (m_clip.CapacityBehaviour) {
					case ThunkClipCapacityBehaviour.Unbound:
						break;
					case ThunkClipCapacityBehaviour.ReplaceOldest:
						var instance = m_instances[0];
						instance.Dispose();
						m_instances.RemoveAt(0);
						break;
					case ThunkClipCapacityBehaviour.IgnoreNew:
						return null;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			
			return CreateInstance(emitter);
		}

		public virtual AudioClip GetClip()
		{
			return m_clip.Clips[m_randomSelector.Select()];
		}

		public void Tick()
		{
			for (var index = m_instances.Count - 1; index >= 0; index--) {
				var instance = m_instances[index];
				if (!instance.IsPlaying) {
					m_instances.RemoveAt(index);
					instance.Dispose();
				}
			}
		}
		
		protected virtual ThunkClipInstance CreateInstance(ThunkEmitter emitter)
		{
			m_nextPlayTime += m_clip.MinimumTimeBetweenPlays;
			var instance = new ThunkClipInstance(m_nextId++, emitter, new ThunkClipRequest {
				Source = this
			});
			
			m_instances.Add(instance);
			return instance;
		}
	}
	
	public class ThunkClipStateManager
	{
		private readonly Dictionary<ulong, ThunkClipState> m_states = new Dictionary<ulong, ThunkClipState>();

		public void Tick()
		{
			foreach (var thunkClipState in m_states) {
				thunkClipState.Value.Tick();
			}
		}
		
		public ThunkClipState GetOrCreate(ThunkClip clip)
		{
			if (clip == null) {
				return null;
			}

			if (!m_states.TryGetValue(clip.UniqueId, out var state) || state == null) {
				state = clip.CreateState();
				m_states[clip.UniqueId] = state;
			}

			return state;
		}

		public bool TryGet(ThunkClip clip, out ThunkClipState state)
		{
			return m_states.TryGetValue(clip.UniqueId, out state);
		}

		public void Unregister(ThunkClip clip)
		{
			m_states.Remove(clip.UniqueId);
		}

		public void Dispose()
		{
			m_states.Clear();
		}
	}
}