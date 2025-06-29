using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tekly.Tracing
{
	public static class TraceEvents
	{
		public static float LongFrameMs { get; set; } = 70;

		private static volatile int s_frame;

		private static readonly List<TraceEvent> s_events = new List<TraceEvent>(256);
		private static readonly List<TraceEvent> s_pendingEvents = new List<TraceEvent>(256);

		private static readonly object s_eventsLock = new object();
		private static readonly object s_pendingEventsLock = new object();

		private static TraceEvent s_previousTraceEvent;

		private static readonly Stopwatch s_stopwatch = new Stopwatch();

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Initialize()
		{
			s_stopwatch.Start();
			Debug.Log("TraceEvents initialized");

			s_previousTraceEvent = InstantInternal("__LongFrame", "__LongFrame");
		}

		public static void ProcessEvents(Action<List<TraceEvent>> callback)
		{
			lock (s_eventsLock) {
				s_events.Sort();
				callback(s_events);
			}
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Instant(string id, string category)
		{
			var frameEvent = InstantInternal(id, category);
			lock (s_eventsLock) {
				s_events.Add(frameEvent);
			}
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Begin(string name, string category)
		{
			BeginWithId(name, category);
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Begin<T0>(string name, string category, string arg0, T0 value0)
		{
			BeginWithId(name, category, new [] {
				TraceEventArg.Create(arg0, value0)
			});
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Begin<T0, T1>(string name, string category, string arg0, T0 value0, string arg1, T1 value1)
		{
			BeginWithId(name, category, new [] {
				TraceEventArg.Create(arg0, value0),
				TraceEventArg.Create(arg1, value1)
			});
		}
		
		public static int BeginWithId(string name, string category, TraceEventArg[] args = null)
		{
#if TEKLY_TRACEEVENTS_ENABLED
			var unixTime = s_stopwatch.Elapsed.TotalMilliseconds;

			var frameEvent = TraceEvent.Create(args);

			frameEvent.Name = name;
			frameEvent.Category = category;
			frameEvent.StartFrame = s_frame;
			frameEvent.EndFrame = s_frame;
			frameEvent.StartTime = unixTime;
			frameEvent.EndTime = unixTime;

			lock (s_pendingEventsLock) {
				s_pendingEvents.Add(frameEvent);
			}

			return frameEvent.Id;
#else
			return -1;
#endif
		}

		public static TraceEventDisposable Scoped(string name, string category)
		{
#if TEKLY_TRACEEVENTS_ENABLED
			return TraceEventDisposable.BeginEvent(name, category);
#else
			return TraceEventDisposable.Unit;
#endif
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void End(int eventId)
		{
			var endTime = s_stopwatch.Elapsed.TotalMilliseconds;

			lock (s_pendingEventsLock) {
				for (var index = 0; index < s_pendingEvents.Count; index++) {
					var target = s_pendingEvents[index];

					if (target.Id != eventId) {
						continue;
					}

					s_pendingEvents.RemoveAt(index);

					target.EndFrame = s_frame;
					target.EndTime = endTime;

					lock (s_eventsLock) {
						s_events.Add(target);
					}

					return;
				}
			}

			Debug.LogError("TraceEvents - End called with invalid Event Id");
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void End(string id, string type)
		{
			lock (s_pendingEventsLock) {
				for (var index = 0; index < s_pendingEvents.Count; index++) {
					var target = s_pendingEvents[index];
					if (target.Category == type && target.Name == id) {
						s_pendingEvents.RemoveAt(index);

						target.EndFrame = s_frame;
						target.EndTime = s_stopwatch.Elapsed.TotalMilliseconds;

						lock (s_eventsLock) {
							s_events.Add(target);
						}

						return;
					}
				}
			}

			Debug.LogErrorFormat("TraceEvents - End with no Begin ID: {0} - Type: {1}", id, type);
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Update()
		{
			s_frame = Time.frameCount;
			var frameEvent = InstantInternal("__LongFrame", "__LongFrame");

			s_previousTraceEvent.EndTime = frameEvent.StartTime;

			var frameLength = s_previousTraceEvent.EndTime - s_previousTraceEvent.StartTime;
			if (frameLength > LongFrameMs) {
				lock (s_eventsLock) {
					s_events.Add(s_previousTraceEvent);
				}
			}

			s_previousTraceEvent = frameEvent;
		}

		private static TraceEvent InstantInternal(string id, string type)
		{
			var unixTime = s_stopwatch.Elapsed.TotalMilliseconds;

			return new TraceEvent {
				Name = id,
				Category = type,
				StartFrame = s_frame,
				EndFrame = s_frame,
				StartTime = unixTime,
				EndTime = unixTime,
				ThreadName = "Long Frames"
			};
		}
	}
}
