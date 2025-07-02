using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Common.Utils.PropertyBags;
using Tekly.Content;
using Tekly.Lofi.Emitters;
using Tekly.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Lofi.Core
{

	[Flags]
	public enum OneShotRequestOverrides
	{
		None,
		Pitch = 1,
		Volume = 2
	}
	
	public struct OneShotRequest
	{
		public string SfxId;
		public OneShotRequestOverrides Overrides;
		
		public float Pitch {
			get => m_pitch;
			set {
				m_pitch = Mathf.Max(0.01f, value);
				Overrides |= OneShotRequestOverrides.Pitch;
			}
		}
		
		public float Volume {
			get => m_volume;
			set {
				m_volume = value;
				Overrides |= OneShotRequestOverrides.Volume;
			}
		}

		private float m_pitch;
		private float m_volume;
	}

	public class Lofi : Singleton<Lofi>
	{
		public bool IsLoading => !m_coreAssetsHandle.IsDone || m_banks.Any(x => x.IsLoading);
		public bool Paused { get; private set; }

		private readonly List<LofiClipBank> m_banks = new List<LofiClipBank>();
		private readonly Dictionary<string, LofiClip> m_clips = new Dictionary<string, LofiClip>();
		private readonly Dictionary<string, LofiTrack> m_tracks = new Dictionary<string, LofiTrack>();

		private LofiCoreAssets m_coreAssets;

		private LofiEmitter m_trackEmitter;
		private LofiEmitter m_oneShot;

		private bool m_initialized;
		private IContentOperation<LofiCoreAssets> m_coreAssetsHandle;

		private PropertyBag m_propertyBag;
		private IDisposable m_propertyListener;

		private readonly List<NumberProperty> m_volumeProperties = new List<NumberProperty>();

		private readonly TkLogger m_logger = TkLogger.Get<Lofi>();

		private const string LOFI_CORE_KEY = "lofi_core";

		public Lofi()
		{
			Initialize();
			LifeCycle.Instance.Pause += OnPause;
			LifeCycle.Instance.Update += OnTick;
		}

		private void OnTick()
		{
			foreach (var track in m_tracks.Values) {
				track.Tick();
			}
		}

		private void OnPause(bool paused)
		{
			Paused = paused;
		}

		public void SetPropertyBag(PropertyBag propertyBag)
		{
			m_propertyListener?.Dispose();
			m_propertyBag = propertyBag;

			if (m_coreAssets != null) {
				ApplyProperties();
			}
		}

		private void Initialize()
		{
			m_coreAssetsHandle = ContentProvider.Instance.LoadAssetAsync<LofiCoreAssets>(LOFI_CORE_KEY);
			m_coreAssetsHandle.Completed += CoreAssetsLoaded;
		}

		private void CoreAssetsLoaded(IContentOperation<LofiCoreAssets> operation)
		{
			if (!operation.HasError) {
				m_coreAssets = operation.Result;
				ApplyProperties();
			} else {
				m_logger.Exception(operation.Exception, "Failed to load Lofi Core Assets");
			}
		}

		public void LoadBank(string label)
		{
			var bank = m_banks.FirstOrDefault(x => x.Label == label);

			if (bank != null) {
				bank.AddRef();
			} else {
				bank = new LofiClipBank(label, m_clips);
				m_banks.Add(bank);
			}
		}
		
		public void LoadBank(LofiClipBankDefinitionRef bankRef)
		{
			var bank = m_banks.FirstOrDefault(x => x.BankRef == bankRef);

			if (bank != null) {
				bank.AddRef();
			} else {
				bank = new LofiClipBank(bankRef, m_clips);
				m_banks.Add(bank);
			}
		}

		public void UnloadBank(string label)
		{
			var bank = m_banks.FirstOrDefault(x => x.Label == label);

			if (bank != null) {
				bank.RemoveRef();

				if (bank.References == 0) {
					bank.Dispose();
					m_banks.Remove(bank);
				}
			}
		}

		public void PlayOnTrack(string clipId, string trackId)
		{
			var track = GetTrack(trackId);

			if (TryGetClip(clipId, out var clip)) {
				track.Play(clip);
			}
		}

		public void CrossFadeOnTrack(string clipId, string trackId, float duration)
		{
			var track = GetTrack(trackId);

			if (TryGetClip(clipId, out var clip)) {
				track.CrossFade(clip, duration);
			}
		}
		
		public void LoopTrack(string clipId, string trackId, float crossFadeDuration)
		{
			var track = GetTrack(trackId);

			if (TryGetClip(clipId, out var clip)) {
				track.Loop(clip, crossFadeDuration);
			}
		}

		public void StopTrack(string trackId)
		{
			var track = GetTrack(trackId);
			track.Stop();
		}

		public bool TryGetClip(string id, out LofiClip clip)
		{
			if (m_clips.TryGetValue(id, out clip)) {
				return true;
			}

			m_logger.Error("Failed to find LofiClip [{id}]", ("id", id));
			return false;
		}

		public void PlayOneShot(string id)
		{
			PlayOneShot(new OneShotRequest {
				SfxId = id
			});
		}

		public void PlayOneShot(OneShotRequest request)
		{
			if (m_oneShot == null) {
				m_oneShot = CreateEmitter("[Lofi] One Shot");
			}

			if (TryGetClip(request.SfxId, out var clip)) {
				var runnerData = clip.CreateRunnerData(request);
				m_oneShot.Play(clip, runnerData);
			}
		}

		public LofiTrack GetTrack(string trackId)
		{
			if (m_trackEmitter == null) {
				m_trackEmitter = CreateEmitter("[Lofi] Track Emitter");
			}

			if (!m_tracks.TryGetValue(trackId, out var track)) {
				track = new LofiTrack(m_trackEmitter);
				m_tracks.Add(trackId, track);
			}

			return track;
		}

		private LofiEmitter CreateEmitter(string name)
		{
			var emitter = new GameObject(name);
			Object.DontDestroyOnLoad(emitter);

			return emitter.AddComponent<LofiEmitter>();
		}

		private void SetVolume(string id, double linearValue)
		{
			m_coreAssets.Mixer.SetFloat(id, Constants.ToDecibel((float)linearValue));
		}

		private void ApplyProperties()
		{
			if (m_propertyBag == null) {
				return;
			}

			var groups = m_coreAssets.Mixer.FindMatchingGroups("");
			foreach (var group in groups) {
				var volumeId = group.name + ".volume";
				if (m_coreAssets.Mixer.GetFloat(volumeId, out var currentVolume)) {
					currentVolume = Constants.ToLinear(currentVolume);

					m_propertyBag.GetOrAdd(volumeId, currentVolume, out var volumeProp);
					SetVolume(volumeId, volumeProp.Value);

					m_volumeProperties.Add(volumeProp);
				}
			}

			m_propertyListener = m_propertyBag.Modified.Subscribe(PropertyModified);
		}

		private void PropertyModified(Property property)
		{
			if (property is NumberProperty numberProperty) {
				if (m_volumeProperties.Contains(property)) {
					SetVolume(numberProperty.Id, numberProperty.Value);
				}
			}
		}
	}
}