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

		private bool m_warnedNoAudioClip;
		
		public ThunkClipState(ThunkClip clip)
		{
			m_clip = clip;
			m_clips.Add(clip);
			Reset();
		}

		public virtual ThunkClipInstance Play(ThunkEmitter emitter, float? pitch = null, float? volume = null, float? delay = null, float? startTime = null)
		{
			// Unscaled: rate limiting is about audible repetition, and must keep working while timeScale is 0
			if (Time.unscaledTime < m_nextPlayTime) {
				return null;
			}

			// A capacity of 0 or less means no limit
			var atCapacity = m_clip.InstanceCapacity > 0 && m_instances.Count >= m_clip.InstanceCapacity;
			
			if (atCapacity && m_clip.CapacityBehaviour == ThunkClipCapacityBehaviour.IgnoreNew) {
				return null;
			}

			// Selected before evicting so a clip with no AudioClips doesn't stop what's already playing
			var audioClip = GetClip();
			
			if (audioClip == null) {
				WarnNoAudioClip();
				return null;
			}
			
			if (atCapacity) {
				switch (m_clip.CapacityBehaviour) {
					case ThunkClipCapacityBehaviour.Unbound:
						break;
					case ThunkClipCapacityBehaviour.ReplaceOldest:
						// Dispose removes the instance from m_instances
						m_instances[0].Dispose();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var request = new ThunkClipRequest {
				Source = this,
				AudioClip = audioClip,
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

		/// <summary>
		/// Selects the next AudioClip to play. Returns null if the ThunkClip has no AudioClips.
		/// An empty slot in the Clips array also returns null.
		/// </summary>
		public virtual AudioClip GetClip()
		{
			if (m_randomSelector.Size == 0 || m_randomSelector.Size != ClipCount) {
				return null;
			}
			
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
				if (m_randomSelector.Size != ClipCount) {
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
			m_warnedNoAudioClip = false;
			
			// RandomSelector64 requires at least one entry; GetClip returns null while it is empty
			var count = ClipCount;
			m_randomSelector = count > 0 ? new RandomSelector64(count, m_clip.RandomMode) : default;
		}

		private int ClipCount => m_clip.Clips?.Length ?? 0;

		private void WarnNoAudioClip()
		{
			if (m_warnedNoAudioClip) {
				return;
			}
			
			m_warnedNoAudioClip = true;
			UnityEngine.Debug.LogWarning($"[Thunk] ThunkClip [{Name}] has no AudioClip to play (Clips is empty or has an empty slot)", m_clip);
		}
		
		protected virtual ThunkClipInstance CreateInstance(ThunkEmitter emitter, ThunkClipRequest request)
		{
			m_nextPlayTime = Time.unscaledTime + m_clip.MinimumTimeBetweenPlays;
			var instance = new ThunkClipInstance(emitter, request);
			
			m_instances.Add(instance);
			return instance;
		}
	}
}