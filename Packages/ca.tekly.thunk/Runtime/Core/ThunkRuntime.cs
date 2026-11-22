using System;
using System.Collections.Generic;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace Tekly.Thunk
{
	/// <summary>
	/// Owns everything that outlives a single emitter or asset while the game runs: the playing voices, the shared
	/// state of each sound and track, and the emitter used for sounds played with no position. Nothing outside the
	/// package talks to it directly: sounds, emitters, tracks and handles forward to it.
	/// </summary>
	internal sealed class ThunkRuntime : Singleton<ThunkRuntime>, IDisposable
	{
		public IReadOnlyList<ThunkVoice> ActiveVoices => m_active;

		private readonly TkLogger m_logger = TkLogger.Get<ThunkRuntime>();

		private readonly List<ThunkVoice> m_voices = new List<ThunkVoice>();
		private readonly Stack<ThunkVoice> m_freeVoices = new Stack<ThunkVoice>();

		// Voices being ticked. Ending a voice nulls its slot, the nulls are removed after the next tick, so voices
		// can start and end while the list is being walked.
		private readonly List<ThunkVoice> m_active = new List<ThunkVoice>();
		private bool m_activeHasGaps;

		private readonly ThunkStateMap<ThunkSoundRecord> m_records = new ThunkStateMap<ThunkSoundRecord>();
		private readonly ThunkStateMap<ThunkTrackState> m_tracks = new ThunkStateMap<ThunkTrackState>();

		// Tracks whose duck level is changing
		private readonly List<ThunkTrackState> m_fadingTracks = new List<ThunkTrackState>();

		private ThunkEmitter m_defaultEmitter;
		private long m_playCount;
		private bool m_applicationPaused;

		public ThunkRuntime()
		{
			LifeCycle.Instance.Update += OnUpdate;
			LifeCycle.Instance.Pause += OnPause;
		}

		public void Dispose()
		{
			LifeCycle.Instance.Update -= OnUpdate;
			LifeCycle.Instance.Pause -= OnPause;

			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice != null) {
					// Tracks are going away too, so they don't get to react
					voice.Track = null;
					Free(voice, true);
				}
			}

			m_active.Clear();
			m_records.Clear();
			m_tracks.Clear();
			m_fadingTracks.Clear();

			if (m_defaultEmitter != null) {
				if (Application.isPlaying) {
					Object.Destroy(m_defaultEmitter.gameObject);
				} else {
					Object.DestroyImmediate(m_defaultEmitter.gameObject);
				}
			}
		}

		public static bool TryResolve(ThunkHandle handle, out ThunkVoice voice)
		{
			var runtime = Instance;

			if (runtime == null) {
				voice = null;
				return false;
			}

			return runtime.TryGetVoice(handle, out voice);
		}

		public bool TryGetVoice(ThunkHandle handle, out ThunkVoice voice)
		{
			if (handle.Generation != 0 && (uint) handle.Index < (uint) m_voices.Count) {
				voice = m_voices[handle.Index];

				if (voice.Active && voice.Generation == handle.Generation) {
					return true;
				}
			}

			voice = null;
			return false;
		}

		public ThunkSoundRecord GetRecord(ThunkSound sound)
		{
			return m_records.Get(sound);
		}

		public ThunkTrackState GetTrack(ThunkTrack track)
		{
			return m_tracks.Get(track);
		}

		/// <summary>
		/// Plays a sound on an emitter, or with no position if the emitter is null. A negative fadeIn uses the
		/// sound's own fade in time.
		/// </summary>
		public ThunkHandle Play(ThunkSound sound, ThunkEmitter emitter, float fadeIn = -1f, ThunkTrackState track = null, AudioMixerGroup mixerGroup = null, float trackGain = 1f)
		{
			if (sound == null) {
				m_logger.Warning("Play was called with a null sound");
				return ThunkHandle.None;
			}

			var record = m_records.Get(sound);
			var now = Time.unscaledTimeAsDouble;

			if (record.IsOnCooldown(now, sound.Cooldown)) {
				Rejected(sound, "it is on cooldown");
				return ThunkHandle.None;
			}

			if (record.IsFull(sound.MaxInstances)) {
				var stolen = sound.Stealing != ThunkStealing.None ? FindVoiceToSteal(record, sound.Stealing) : null;

				if (stolen == null) {
					Rejected(sound, "it is at its max instances");
					return ThunkHandle.None;
				}

				Free(stolen, true);
			}

			// A sound played with no emitter has no position, whatever its spatial template says
			AudioSource spatialTemplate = null;

			if (emitter == null) {
				emitter = GetDefaultEmitter();
			} else if (sound.SpatialTemplate != null) {
				spatialTemplate = sound.SpatialTemplate;
			} else {
				spatialTemplate = emitter.SpatialTemplate;
			}

			if (fadeIn < 0f) {
				fadeIn = sound.FadeIn;
			}

			var voice = AllocateVoice();
			voice.Sound = sound;
			voice.Record = record;
			voice.Emitter = emitter;
			voice.Track = track;
			voice.MixerGroup = mixerGroup != null ? mixerGroup : sound.MixerGroup;
			voice.State = ThunkVoiceState.Playing;
			voice.Unscaled = sound.PlayWhilePaused;
			voice.BaseVolume = sound.Volume.Get();
			voice.BasePitch = sound.Pitch.Get();
			voice.UserVolume = 1f;
			voice.UserPitch = 1f;
			voice.Fade = fadeIn > 0f ? 0f : 1f;
			voice.TrackGain = trackGain;
			voice.SpatialTemplate = spatialTemplate;

			if (!sound.Begin(voice, record)) {
				ReturnSources(voice);
				voice.Reset();
				m_freeVoices.Push(voice);

				Rejected(sound, "it has no clips");
				return ThunkHandle.None;
			}

			if (fadeIn > 0f) {
				voice.StartFade(1f, fadeIn, sound.FadeEase);
			}

			record.LiveCount++;
			record.LastPlayTime = now;

			voice.Active = true;
			voice.Order = m_playCount++;
			voice.ActiveIndex = m_active.Count;
			m_active.Add(voice);

			return voice.Handle;
		}

		/// <summary>
		/// Stops a voice, fading it out first if a fade is given. A stopping voice can't be paused or resumed.
		/// </summary>
		public void StopVoice(ThunkVoice voice, float fade)
		{
			// The track hears about it now, not when the fade ends, so what it brings back fades in alongside
			var track = voice.Track;
			var handle = voice.Handle;
			voice.Track = null;

			// A paused voice is silent, there is nothing to fade
			if (fade <= 0f || voice.State == ThunkVoiceState.Paused) {
				// Freed before the track reacts, so the voice no longer counts against its sound's limits
				Free(voice, true);
			} else {
				voice.State = ThunkVoiceState.Stopping;
				voice.StartFade(0f, fade, voice.Sound.FadeEase);
			}

			track?.VoiceEnded(this, handle, fade);
		}

		public void PauseVoice(ThunkVoice voice, float fade)
		{
			if (voice.State != ThunkVoiceState.Playing) {
				return;
			}

			if (fade <= 0f) {
				voice.StartFade(0f, 0f, voice.Sound.FadeEase);
				voice.Sound.PauseSources(voice);
				voice.State = ThunkVoiceState.Paused;
				return;
			}

			voice.State = ThunkVoiceState.Pausing;
			voice.StartFade(0f, fade, voice.Sound.FadeEase);
		}

		public void ResumeVoice(ThunkVoice voice, float fade)
		{
			if (!voice.IsPaused) {
				return;
			}

			if (voice.State == ThunkVoiceState.Paused) {
				voice.Sound.ResumeSources(voice);
			}

			voice.State = ThunkVoiceState.Playing;
			voice.StartFade(1f, fade, voice.Sound.FadeEase);
		}

		public void StopEmitter(ThunkEmitter emitter, float fade)
		{
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice != null && ReferenceEquals(voice.Emitter, emitter)) {
					StopVoice(voice, fade);
				}
			}
		}

		public void ApplyEmitter(ThunkEmitter emitter)
		{
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice != null && ReferenceEquals(voice.Emitter, emitter)) {
					voice.Apply();
				}
			}
		}

		/// <summary>
		/// Ends every voice on an emitter that is being destroyed. Its AudioSources are destroyed with it, so they
		/// are not handed back.
		/// </summary>
		public void EmitterDestroyed(ThunkEmitter emitter)
		{
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice != null && ReferenceEquals(voice.Emitter, emitter)) {
					Free(voice, false);
				}
			}
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			// Voices started during the tick are appended and ticked in the same pass
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice == null) {
					continue;
				}

				if (voice.IsFading && voice.TickFade(voice.Unscaled ? unscaledDeltaTime : deltaTime)) {
					if (voice.State == ThunkVoiceState.Stopping) {
						Free(voice, true);
						continue;
					}

					if (voice.State == ThunkVoiceState.Pausing) {
						voice.Sound.PauseSources(voice);
						voice.State = ThunkVoiceState.Paused;
					}
				}

				if (voice.HasSourceFades && voice.State != ThunkVoiceState.Paused) {
					voice.TickSourceFades(voice.Unscaled ? unscaledDeltaTime : deltaTime);
				}

				if (voice.State != ThunkVoiceState.Paused && voice.IsFinished(m_applicationPaused)) {
					Free(voice, true);
				}
			}

			RemoveGaps();

			// Ducking runs on unscaled time so it works while the game is paused
			for (var index = m_fadingTracks.Count - 1; index >= 0; index--) {
				if (!m_fadingTracks[index].TickDuck(this, unscaledDeltaTime)) {
					m_fadingTracks.RemoveAt(index);
				}
			}
		}

		/// <summary>
		/// Has the track's duck level ticked until it reports it has settled.
		/// </summary>
		public void TickTrack(ThunkTrackState track)
		{
			if (!m_fadingTracks.Contains(track)) {
				m_fadingTracks.Add(track);
			}
		}

		private void OnUpdate()
		{
			Tick(Time.deltaTime, Time.unscaledDeltaTime);
		}

		private void OnPause(bool paused)
		{
			m_applicationPaused = paused;
		}

		private ThunkVoice AllocateVoice()
		{
			if (m_freeVoices.TryPop(out var voice)) {
				return voice;
			}

			voice = new ThunkVoice(m_voices.Count);
			m_voices.Add(voice);

			return voice;
		}

		private void Free(ThunkVoice voice, bool returnSources)
		{
			if (!voice.Active) {
				return;
			}

			var handle = voice.Handle;

			voice.Active = false;
			voice.Generation++;
			voice.Record.LiveCount--;

			m_active[voice.ActiveIndex] = null;
			m_activeHasGaps = true;

			if (returnSources) {
				ReturnSources(voice);
			}

			var track = voice.Track;

			voice.Reset();
			m_freeVoices.Push(voice);

			// Last, as the track may start or resume other voices in response
			track?.VoiceEnded(this, handle, -1f);
		}

		private static void ReturnSources(ThunkVoice voice)
		{
			for (var index = 0; index < voice.Sources.Count; index++) {
				voice.Emitter.ReturnSource(voice.Sources[index]);
			}

			voice.Sources.Clear();
		}

		private ThunkVoice FindVoiceToSteal(ThunkSoundRecord record, ThunkStealing stealing)
		{
			ThunkVoice best = null;

			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice == null || voice.Record != record) {
					continue;
				}

				if (best == null) {
					best = voice;
				} else if (stealing == ThunkStealing.Oldest) {
					if (voice.Order < best.Order) {
						best = voice;
					}
				} else if (voice.Loudness < best.Loudness) {
					best = voice;
				}
			}

			return best;
		}

		private void RemoveGaps()
		{
			if (!m_activeHasGaps) {
				return;
			}

			var write = 0;

			for (var read = 0; read < m_active.Count; read++) {
				var voice = m_active[read];

				if (voice != null) {
					voice.ActiveIndex = write;
					m_active[write++] = voice;
				}
			}

			m_active.RemoveRange(write, m_active.Count - write);
			m_activeHasGaps = false;
		}

		private ThunkEmitter GetDefaultEmitter()
		{
			// Unity null check, so a destroyed emitter is recreated
			if (m_defaultEmitter == null) {
				var gameObject = new GameObject("[Thunk] 2D");

				if (Application.isPlaying) {
					Object.DontDestroyOnLoad(gameObject);
				}

				m_defaultEmitter = gameObject.AddComponent<ThunkEmitter>();
			}

			return m_defaultEmitter;
		}

		private void Rejected(ThunkSound sound, string reason)
		{
			// Checked first so the sound's name is only read when the message will be logged
			if (m_logger.IsLevelEnabled(TkLogLevel.Debug)) {
				m_logger.Debug("Did not play [{sound}] because [{reason}]", ("sound", sound.name), ("reason", reason));
			}
		}
	}
}
