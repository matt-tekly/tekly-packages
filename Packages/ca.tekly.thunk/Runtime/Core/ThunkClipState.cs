using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk.Core
{
	/// <summary>
	/// Manages the shared state for a ThunkClip. This is what controls random clip selection and maximum playing
	/// instances of the clip.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class ThunkClipState
	{
		public float GeneratePitch => m_clip.Pitch.Get();
		public float GenerateVolume => m_clip.Volume.Get();
		public bool IsLooping => m_clip.Loop;
		
		public AudioMixerGroup MixerGroup => m_clip.MixerGroup;
		public ThunkClip Clip => m_clip;
		public string Name => m_clip.GetNameSafe();

		private string DebuggerDisplay => Name;

		/// <summary>
		/// The loaded copy of the ThunkClip whose settings are used. The same clip can be loaded more than once
		/// (e.g. it is in multiple AssetBundles); all copies share this state and this switches to another loaded
		/// copy when the current one is unloaded.
		/// </summary>
		protected ThunkClip m_clip;
		protected readonly List<ThunkClipInstance> m_instances = new List<ThunkClipInstance>();

		private readonly List<ThunkClip> m_clips = new List<ThunkClip>();
		
		protected float m_nextPlayTime;
		protected RandomSelector64 m_randomSelector;
		
		public ThunkClipState(ThunkClip clip)
		{
			m_clip = clip;
			m_clips.Add(clip);
			Reset();
		}

		public virtual ThunkClipInstance Play(ThunkEmitter emitter, float? pitch = null, float? volume = null, float? delay = null, float? startTime = null)
		{
			if (Time.time < m_nextPlayTime) {
				return null;
			}
			
			if (m_instances.Count >= m_clip.InstanceCapacity) {
				switch (m_clip.CapacityBehaviour) {
					case ThunkClipCapacityBehaviour.Unbound:
						break;
					case ThunkClipCapacityBehaviour.ReplaceOldest:
						// Dispose removes the instance from m_instances
						m_instances[0].Dispose();
						break;
					case ThunkClipCapacityBehaviour.IgnoreNew:
						return null;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var request = new ThunkClipRequest {
				Source = this,
				Pitch = pitch,
				Volume = volume,
				Delay = delay,
				StartTime = startTime
			};
			
			return CreateInstance(emitter, request);
		}

		public ThunkClipInstance GetInstance(int id)
		{
			for (var index = 0; index < m_instances.Count; index++) {
				var instance = m_instances[index];
				if (instance.Id == id) {
					return instance;
				}
			}

			return null;
		}

		public virtual AudioClip GetClip()
		{
			return m_clip.Clips[m_randomSelector.Select()];
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			// Iterating backwards so instances can remove themselves (via Dispose) while ticking
			for (var index = m_instances.Count - 1; index >= 0; index--) {
				if (index >= m_instances.Count) {
					continue;
				}

				var instance = m_instances[index];
				instance.Tick(deltaTime, unscaledDeltaTime);
				
				if (!instance.IsDisposed && !instance.IsPlaying) {
					instance.Dispose();
				}
			}
		}

		/// <summary>
		/// Registers a loaded copy of the clip. Called for every in memory instance of a ThunkClip.
		/// </summary>
		internal void AddClip(ThunkClip clip)
		{
			for (var index = 0; index < m_clips.Count; index++) {
				if (ReferenceEquals(m_clips[index], clip)) {
					return;
				}
			}

			m_clips.Add(clip);

			// Every previously registered copy was destroyed without unregistering
			if (m_clip == null) {
				m_clip = clip;
				Reset();
			}
		}

		/// <summary>
		/// Unregisters a copy of the clip that is being unloaded. Returns false if the copy was never registered.
		/// </summary>
		internal bool RemoveClip(ThunkClip clip)
		{
			var removed = false;

			// Also prunes copies that were destroyed without being unregistered
			for (var index = m_clips.Count - 1; index >= 0; index--) {
				var registered = m_clips[index];

				if (ReferenceEquals(registered, clip)) {
					m_clips.RemoveAt(index);
					removed = true;
				} else if (registered == null) {
					m_clips.RemoveAt(index);
				}
			}

			if (m_clips.Count > 0 && (ReferenceEquals(m_clip, clip) || m_clip == null)) {
				m_clip = m_clips[0];

				// Copies should match, but a bundle built at a different time could have a different clip list
				if (m_randomSelector.Size != m_clip.Clips.Length) {
					Reset();
				}
			}

			return removed;
		}

		internal bool HasClips => m_clips.Count > 0;

		/// <summary>
		/// Stops every instance of this clip. Used when the last loaded copy of the clip is unloaded.
		/// </summary>
		internal void DisposeInstances()
		{
			for (var index = m_instances.Count - 1; index >= 0; index--) {
				if (index < m_instances.Count) {
					m_instances[index].Dispose();
				}
			}
		}

		/// <summary>
		/// Called by ThunkClipInstance.Dispose so the instance stops counting towards capacity immediately
		/// </summary>
		internal void InstanceDisposed(ThunkClipInstance instance)
		{
			m_instances.Remove(instance);
		}

		public void Reset()
		{
			m_randomSelector = new RandomSelector64(m_clip.Clips.Length, m_clip.RandomMode);
		}
		
		protected virtual ThunkClipInstance CreateInstance(ThunkEmitter emitter, ThunkClipRequest request)
		{
			m_nextPlayTime = Time.time + m_clip.MinimumTimeBetweenPlays;
			var instance = new ThunkClipInstance(emitter, request);
			
			m_instances.Add(instance);
			return instance;
		}
	}
}