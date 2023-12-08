using System.Collections.Generic;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.Content;
using Tekly.Lofi.Emitters;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Lofi.Core
{
	public class Lofi : Singleton<Lofi>
	{
		public bool IsLoading => !m_coreAssetsHandle.IsDone || m_banks.Any(x => x.IsLoading);

		private readonly List<LofiClipBank> m_banks = new List<LofiClipBank>();
		private readonly Dictionary<string, LofiClip> m_clips = new Dictionary<string, LofiClip>();
		private readonly Dictionary<string, LofiTrack> m_tracks = new Dictionary<string, LofiTrack>();

		private LofiCoreAssets m_coreAssets;

		private LofiEmitter m_oneShot;
		private bool m_initialized;

		private IContentOperation<LofiCoreAssets> m_coreAssetsHandle;

		private readonly TkLogger m_logger = TkLogger.Get<Lofi>();
		private LofiEmitter m_trackEmitter;

		public Lofi()
		{
			Initialize();
		}

		private void Initialize()
		{
			m_coreAssetsHandle = ContentProvider.Instance.LoadAssetAsync<LofiCoreAssets>("lofi_core");
			m_coreAssetsHandle.Completed += CoreAssetsLoaded;
		}

		private void CoreAssetsLoaded(IContentOperation<LofiCoreAssets> operation)
		{
			if (!operation.HasError) {
				m_coreAssets = operation.Result;
				var groups = m_coreAssets.Mixer.FindMatchingGroups("");
				Debug.Log(groups.Length);
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
		
		public void PlayOnTrack(string id, string trackId)
		{
			var track = GetTrack(trackId);

			if (TryGetClip(id, out var clip)) {
				track.Play(clip);
			} else {
				m_logger.Error("PlayOnTrack Failed to find LofiClip [{id}]", ("id", id));
			}
		}

		public void CrossFadeOnTrack(string id, string trackId, float duration)
		{
			var track = GetTrack(trackId);

			if (TryGetClip(id, out var clip)) {
				track.CrossFade(clip, duration);
			} else {
				m_logger.Error("CrossFadeOnTrack Failed to find LofiClip [{id}]", ("id", id));
			}
		}
		
		public void StopTrack(string trackId)
		{
			var track = GetTrack(trackId);
			track.Stop();
		}

		public bool TryGetClip(string id, out LofiClip clip)
		{
			return m_clips.TryGetValue(id, out clip);
		}

		public void PlayOneShot(string id)
		{
			if (m_oneShot == null) {
				var go = new GameObject("[Lofi] One Shot");
				Object.DontDestroyOnLoad(go);
				m_oneShot = go.AddComponent<LofiEmitter>();
			}

			if (TryGetClip(id, out var clip)) {
				m_oneShot.Play(clip);
			} else {
				m_logger.Error("PlayOneShot Failed to find LofiClip [{id}]", ("id", id));
			}
		}

		private LofiTrack GetTrack(string trackId)
		{
			if (m_trackEmitter == null) {
				var trackEmitter = new GameObject("[Lofi] Track Emitter");

				Object.DontDestroyOnLoad(trackEmitter);
				m_trackEmitter = trackEmitter.AddComponent<LofiEmitter>();
			}

			if (!m_tracks.TryGetValue(trackId, out var track)) {
				track = new LofiTrack(m_trackEmitter);
				m_tracks.Add(trackId, track);
			}

			return track;
		}
	}
}