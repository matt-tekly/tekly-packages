using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tekly.Common.LifeCycles;
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
		private const string DEFAULT_PROCESS = "Main";

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Initialize(bool selfManaged = true)
		{
			s_stopwatch.Start();
			Debug.Log("TraceEvents initialized");

			s_previousTraceEvent = InstantInternal("__LongFrame", "__LongFrame", "__LongFrame");

			if (selfManaged) {
				LifeCycle.Instance.Update += Update;
				LifeCycle.Instance.Quit += Stop;
			}
		}
		
		public static void Stop()
		{
			LifeCycle.Instance.Update -= Update;
			LifeCycle.Instance.Quit -= Stop;
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
			var frameEvent = InstantInternal(DEFAULT_PROCESS, id, category);
			lock (s_eventsLock) {
				s_events.Add(frameEvent);
			}
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Instant(string process, string id, string category)
		{
			var frameEvent = InstantInternal(process, id, category);
			lock (s_eventsLock) {
				s_events.Add(frameEvent);
			}
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Begin(string name, string category)
		{
			BeginWithId(DEFAULT_PROCESS, name, category);
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Begin<T0>(string name, string category, string arg0, T0 value0)
		{
			BeginWithId(DEFAULT_PROCESS, name, category, new [] {
				TraceEventArg.Create(arg0, value0)
			});
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void Begin<T0, T1>(string name, string category, string arg0, T0 value0, string arg1, T1 value1)
		{
			BeginWithId(DEFAULT_PROCESS, name, category, new [] {
				TraceEventArg.Create(arg0, value0),
				TraceEventArg.Create(arg1, value1)
			});
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void BeginProcess(string process, string name, string category)
		{
			BeginWithId(process, name, category);
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void BeginProcess<T0>(string process, string name, string category, string arg0, T0 value0)
		{
			BeginWithId(process, name, category, new [] {
				TraceEventArg.Create(arg0, value0)
			});
		}
		
		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void BeginProcess<T0, T1>(string process, string name, string category, string arg0, T0 value0, string arg1, T1 value1)
		{
			BeginWithId(process, name, category, new [] {
				TraceEventArg.Create(arg0, value0),
				TraceEventArg.Create(arg1, value1)
			});
		}
		
		public static int BeginWithId(string process, string name, string category, TraceEventArg[] args = null)
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
			frameEvent.Process = process;

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
			return ScopedProcess(DEFAULT_PROCESS, name, category);
		}

		public static TraceEventDisposable ScopedProcess(string process, string name, string category)
		{
#if TEKLY_TRACEEVENTS_ENABLED
			return TraceEventDisposable.BeginEvent(process, name, category);
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
			EndProcess(DEFAULT_PROCESS, id, type);
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		public static void EndProcess(string process, string id, string type)
		{
			lock (s_pendingEventsLock) {
				for (var index = 0; index < s_pendingEvents.Count; index++) {
					var target = s_pendingEvents[index];
					if (target.Process == process && target.Category == type && target.Name == id) {
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

		public static void Update()
		{
			UpdateInternal();
		}

		[Conditional("TEKLY_TRACEEVENTS_ENABLED")]
		private static void UpdateInternal()
		{
			s_frame = Time.frameCount;
			var frameEvent = InstantInternal("__LongFrame", "__LongFrame", "__LongFrame");

			s_previousTraceEvent.EndTime = frameEvent.StartTime;

			var frameLength = s_previousTraceEvent.EndTime - s_previousTraceEvent.StartTime;
			if (frameLength > LongFrameMs) {
				lock (s_eventsLock) {
					s_events.Add(s_previousTraceEvent);
				}
			}

			s_previousTraceEvent = frameEvent;
		}

		private static TraceEvent InstantInternal(string process, string id, string type)
		{
			var unixTime = s_stopwatch.Elapsed.TotalMilliseconds;

			return new TraceEvent {
				Name = id,
				Category = type,
				StartFrame = s_frame,
				EndFrame = s_frame,
				StartTime = unixTime,
				EndTime = unixTime,
				ThreadName = "Long Frames",
				Process = process
			};
		}
	}
}
