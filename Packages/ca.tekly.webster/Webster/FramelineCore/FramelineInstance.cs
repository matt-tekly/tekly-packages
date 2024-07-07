#if ((UNITY_EDITOR && WEBSTER_ENABLE_EDITOR) || WEBSTER_ENABLE) && !WEBSTER_FRAMELINE_DISABLE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tekly.Webster.FramelineCore
{
	public class FramelineInstance : IFramelineInstance
	{
		private const string FramelineConfig = "framelineconfig";

		private FramelineConfig m_config;
		private int m_frame;

		private List<FrameEvent> m_events = new List<FrameEvent>(256);
		private readonly List<FrameEvent> m_pendingEvents = new List<FrameEvent>(256);

		private readonly object m_eventsLock = new object();
		private readonly object m_pendingEventsLock = new object();

		private FrameEvent m_previousFrameEvent;

		private readonly Stopwatch m_stopwatch = new Stopwatch();

		public string Json {
			get {
				lock (m_eventsLock) {
					m_events.Sort();

					var events = new FramelineData {
						Events = m_events,
						Config = m_config
					};

					return JsonUtility.ToJson(events);
				}
			}
		}

		public void Initialize()
		{
			WebsterServer.AddRouteHandler<FramelineRoutes>();

			var asset = Resources.Load<FramelineConfigAsset>(FramelineConfig);

			if (asset != null) {
				m_config = asset.Config;
			} else {
				m_config = new FramelineConfig();
				Debug.LogWarning(
					"Frameline did not find a FramelineConfigAsset. Please create one in a Resources directory to configure Frameline");
			}

			m_stopwatch.Start();
			Debug.Log("Frameline initialized");

			m_previousFrameEvent = InstantEventInternal("__LongFrame", "__LongFrame");
		}

		public void InstantEvent(string id, string type)
		{
			var frameEvent = InstantEventInternal(id, type);
			lock (m_eventsLock) {
				m_events.Add(frameEvent);
			}
		}

		public void BeginEvent(string id, string type)
		{
			BeginEventGetId(id, type);
		}

		public int BeginEventGetId(string id, string type)
		{
			var unixTime = m_stopwatch.Elapsed.TotalMilliseconds;

			var frameEvent = FrameEvent.Create();
			
			frameEvent.Id = id;
			frameEvent.EventType = type;
			frameEvent.StartFrame = m_frame;
			frameEvent.EndFrame = m_frame;
			frameEvent.StartTime = unixTime;
			frameEvent.EndTime = unixTime;

			lock (m_pendingEventsLock) {
				m_pendingEvents.Add(frameEvent);
			}

			return frameEvent.FrameEventId;
		}

		public FramelineEventDisposable BeginEventDisposable(string id, string type)
		{
			return FramelineEventDisposable.BeginEvent(id, type);
		}

		public void EndEvent(int frameEventId)
		{
			lock (m_pendingEventsLock) {
				for (var index = 0; index < m_pendingEvents.Count; index++) {
					var target = m_pendingEvents[index];

					if (target.FrameEventId != frameEventId) {
						continue;
					}

					m_pendingEvents.RemoveAt(index);

					target.EndFrame = m_frame;
					target.EndTime = m_stopwatch.Elapsed.TotalMilliseconds;

					lock (m_eventsLock) {
						m_events.Add(target);
					}

					return;
				}
			}

			Debug.LogError("Frameline - EndEvent called with invalid FrameEventId");
		}

		public void EndEvent(string id, string type)
		{
			lock (m_pendingEventsLock) {
				for (var index = 0; index < m_pendingEvents.Count; index++) {
					var target = m_pendingEvents[index];
					if (target.EventType == type && target.Id == id) {
						m_pendingEvents.RemoveAt(index);

						target.EndFrame = m_frame;
						target.EndTime = m_stopwatch.Elapsed.TotalMilliseconds;

						lock (m_eventsLock) {
							m_events.Add(target);
						}

						return;
					}
				}
			}

			Debug.LogErrorFormat("Frameline - EndEvent with no BeginEvent ID: {0} - Type: {1}", id, type);
		}

		public void Clear()
		{
			lock (m_eventsLock) {
				m_events.Clear();
			}
		}

		public void ClearTo(double time)
		{
			lock (m_eventsLock) {
				m_events = m_events.Where(evt => evt.StartTime >= time).ToList();
			}
		}

		public void ClearAfter(double time)
		{
			lock (m_eventsLock) {
				m_events = m_events.Where(evt => evt.StartTime <= time).ToList();
			}
		}

		public void Update()
		{
			m_frame = Time.frameCount;
			var frameEvent = InstantEventInternal("__LongFrame", "__LongFrame");

			m_previousFrameEvent.EndTime = frameEvent.StartTime;

			var frameLength = m_previousFrameEvent.EndTime - m_previousFrameEvent.StartTime;
			if (frameLength > m_config.LongFrameTimeMs) {
				lock (m_eventsLock) {
					m_events.Add(m_previousFrameEvent);
				}
			}

			m_previousFrameEvent = frameEvent;
		}

		private FrameEvent InstantEventInternal(string id, string type)
		{
			var unixTime = m_stopwatch.Elapsed.TotalMilliseconds;

			return new FrameEvent {
				Id = id,
				EventType = type,
				StartFrame = m_frame,
				EndFrame = m_frame,
				StartTime = unixTime,
				EndTime = unixTime
			};
		}
	}
}
#endif