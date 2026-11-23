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
	/// state of each sound and track, and the pools of AudioSources that sounds play through. Nothing outside the
	/// package talks to it directly: sounds, emitters, tracks and handles forward to it.
	/// </summary>
	internal sealed class ThunkRuntime : Singleton<ThunkRuntime>, IDisposable
	{
		public IReadOnlyList<ThunkVoice> ActiveVoices => m_active;

		private readonly TkLogger m_logger = TkLogger.Get<ThunkRuntime>();

		private readonly List<ThunkVoice> m_voices = new List<ThunkVoice>();
		private readonly Stack<ThunkVoice> m_freeVoices = new Stack<ThunkVoice>();

		// Voices being ticked. Ending a voice sets its slot to null and the nulls are removed at the end of the next
		// tick, so voices can start and end while the list is being walked.
		private readonly List<ThunkVoice> m_active = new List<ThunkVoice>();
		private bool m_activeHasGaps;

		private readonly ThunkStateMap<ThunkSoundRecord> m_records = new ThunkStateMap<ThunkSoundRecord>();
		private readonly ThunkStateMap<ThunkTrackState> m_tracks = new ThunkStateMap<ThunkTrackState>();

		// Tracks whose duck level is changing
		private readonly List<ThunkTrackState> m_fadingTracks = new List<ThunkTrackState>();

		// One pool for each template sounds have been played through, and one for sounds with no template
		private readonly Dictionary<AudioSource, ThunkSourcePool> m_pools = new Dictionary<AudioSource, ThunkSourcePool>();
		private readonly List<AudioSource> m_deadTemplates = new List<AudioSource>();
		private ThunkSourcePool m_plainPool;
		private Transform m_root;

		private long m_playCount;
		private bool m_applicationPaused;

		public ThunkRuntime()
		{
			LifeCycle.Instance.Update += OnUpdate;
			LifeCycle.Instance.LateUpdate += OnLateUpdate;
			LifeCycle.Instance.Pause += OnPause;
		}

		public void Dispose()
		{
			LifeCycle.Instance.Update -= OnUpdate;
			LifeCycle.Instance.LateUpdate -= OnLateUpdate;
			LifeCycle.Instance.Pause -= OnPause;

			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice != null) {
					// The tracks are being torn down too, so they are not notified
					voice.Track = null;
					Free(voice);
				}
			}

			m_active.Clear();
			m_records.Clear();
			m_tracks.Clear();
			m_fadingTracks.Clear();

			m_pools.Clear();
			m_plainPool = null;

			// Every pooled AudioSource is a child of the root
			if (m_root != null) {
				DestroyObject(m_root.gameObject);
			}
		}

		public static void DestroyObject(Object target)
		{
			if (Application.isPlaying) {
				Object.Destroy(target);
			} else {
				Object.DestroyImmediate(target);
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

			// A sound played with no emitter has no position, whatever its spatial template says
			if (emitter == null) {
				return Begin(sound, null, null, Vector3.zero, fadeIn, track, mixerGroup, trackGain);
			}

			var template = Alive(sound.SpatialTemplate);

			if (ReferenceEquals(template, null)) {
				template = Alive(emitter.SpatialTemplate);
			}

			return Begin(sound, emitter, template, Vector3.zero, fadeIn, track, mixerGroup, trackGain);
		}

		/// <summary>
		/// Plays a sound at a point in the world, through its own spatial template.
		/// </summary>
		public ThunkHandle PlayAt(ThunkSound sound, Vector3 position)
		{
			if (sound == null) {
				m_logger.Warning("PlayAt was called with a null sound");
				return ThunkHandle.None;
			}

			return Begin(sound, null, Alive(sound.SpatialTemplate), position, -1f, null, null, 1f);
		}

		/// <summary>
		/// The sound plays through copies of the template. A null template means the sound is not positioned. With
		/// an emitter and a template the voice follows the emitter. Otherwise it stays at the given position.
		/// </summary>
		private ThunkHandle Begin(ThunkSound sound, ThunkEmitter emitter, AudioSource template, Vector3 position, float fadeIn, ThunkTrackState track, AudioMixerGroup mixerGroup, float trackGain)
		{
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

				Free(stolen);
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
			voice.Pool = GetPool(template);
			voice.Positioned = voice.Pool.HasTemplate;
			voice.Position = position;
			voice.Rotation = Quaternion.identity;

			if (voice.Positioned && !ReferenceEquals(emitter, null)) {
				voice.Follow = emitter.transform;
				voice.Follow.GetPositionAndRotation(out voice.Position, out voice.Rotation);
			}

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
			// The track is told now, not when the fade ends, so whatever it brings back fades in while this fades out
			var track = voice.Track;
			var handle = voice.Handle;
			voice.Track = null;

			// A paused voice is already silent, so there is nothing to fade
			if (fade <= 0f || voice.State == ThunkVoiceState.Paused) {
				// Freed before the track reacts, so the voice no longer counts against its sound's limits
				Free(voice);
			} else {
				voice.State = ThunkVoiceState.Stopping;
				voice.Sound.Stopping(voice);
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
		/// Detaches every voice from an emitter that has been disabled or destroyed. Each voice stays where the
		/// emitter last was. A voice that ends on its own plays out, and one that would play forever is released.
		/// </summary>
		public void EmitterGone(ThunkEmitter emitter)
		{
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice == null || !ReferenceEquals(voice.Emitter, emitter)) {
					continue;
				}

				// The emitter's volume and pitch are baked in, since the voice can no longer read them from it
				voice.BaseVolume *= emitter.Volume;
				voice.BasePitch *= emitter.Pitch;
				voice.Emitter = null;
				voice.Follow = null;

				if (!voice.Sound.EndsOnItsOwn && voice.State != ThunkVoiceState.Stopping) {
					voice.Sound.Release(voice, this);
				}
			}
		}

		/// <summary>
		/// Moves the sources of every voice that follows something to where that thing is now.
		/// </summary>
		public void Follow()
		{
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice == null || ReferenceEquals(voice.Follow, null)) {
					continue;
				}

				// Unity null check: an emitter that was never active is destroyed without any callback
				if (voice.Follow == null) {
					voice.Follow = null;
					continue;
				}

				voice.Follow.GetPositionAndRotation(out voice.Position, out voice.Rotation);

				for (var sourceIndex = 0; sourceIndex < voice.Sources.Count; sourceIndex++) {
					voice.Sources[sourceIndex].Transform.SetPositionAndRotation(voice.Position, voice.Rotation);
				}
			}
		}

		public void Tick(float deltaTime, float unscaledDeltaTime)
		{
			var suspended = IsAudioSuspended();

			// Voices started during the tick are appended and ticked in the same pass
			for (var index = 0; index < m_active.Count; index++) {
				var voice = m_active[index];

				if (voice == null) {
					continue;
				}

				if (voice.IsFading && voice.TickFade(voice.Unscaled ? unscaledDeltaTime : deltaTime)) {
					if (voice.State == ThunkVoiceState.Stopping) {
						Free(voice);
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

				if (voice.State != ThunkVoiceState.Paused && voice.IsFinished(suspended)) {
					Free(voice);
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
		/// Keeps ticking the track's duck level until the track reports that it has settled.
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

		private void OnLateUpdate()
		{
			// After everything has moved for the frame
			Follow();
		}

		/// <summary>
		/// True while the application is paused and nothing can be heard. A suspended AudioSource reports that it is
		/// not playing, which must not be mistaken for its clip having finished.
		/// </summary>
		private bool IsAudioSuspended()
		{
#if UNITY_EDITOR
			// The editor's pause button suspends audio without any application pause callback. Stepping a frame while
			// paused still runs this tick.
			if (UnityEditor.EditorApplication.isPaused) {
				return true;
			}
#endif

			return m_applicationPaused;
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

		private void Free(ThunkVoice voice)
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

			ReturnSources(voice);

			var track = voice.Track;

			voice.Reset();
			m_freeVoices.Push(voice);

			// Done last, because the track may start or resume other voices in response
			track?.VoiceEnded(this, handle, -1f);
		}

		private static void ReturnSources(ThunkVoice voice)
		{
			for (var index = 0; index < voice.Sources.Count; index++) {
				var source = voice.Sources[index];
				source.Pool.Return(source);
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

		/// <summary>
		/// Unity null check, so a destroyed template counts as none.
		/// </summary>
		private static AudioSource Alive(AudioSource template)
		{
			return template != null ? template : null;
		}

		private ThunkSourcePool GetPool(AudioSource template)
		{
			if (ReferenceEquals(template, null)) {
				m_plainPool ??= new ThunkSourcePool(GetRoot(), null);
				return m_plainPool;
			}

			if (!m_pools.TryGetValue(template, out var pool)) {
				// Creating a pool is rare, so it is a cheap moment to clean up pools whose templates have been destroyed
				RemoveDeadPools();

				pool = new ThunkSourcePool(GetRoot(), template);
				m_pools.Add(template, pool);
			}

			return pool;
		}

		private void RemoveDeadPools()
		{
			foreach (var entry in m_pools) {
				// A pool with sources still playing is left for a later pass
				if (entry.Value.IsDead && entry.Value.Rented == 0) {
					entry.Value.Clear();
					m_deadTemplates.Add(entry.Key);
				}
			}

			for (var index = 0; index < m_deadTemplates.Count; index++) {
				m_pools.Remove(m_deadTemplates[index]);
			}

			m_deadTemplates.Clear();
		}

		private Transform GetRoot()
		{
			if (ReferenceEquals(m_root, null)) {
				var gameObject = new GameObject("[Thunk]");

				if (Application.isPlaying) {
					Object.DontDestroyOnLoad(gameObject);
				} else {
					gameObject.hideFlags = HideFlags.DontSave;
				}

				m_root = gameObject.transform;
			}

			return m_root;
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
