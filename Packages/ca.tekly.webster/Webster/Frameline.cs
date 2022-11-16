//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if ((UNITY_EDITOR && WEBSTER_ENABLE_EDITOR) || WEBSTER_ENABLE) && !WEBSTER_FRAMELINE_DISABLE
#define FRAMELINE_ENABLE
#endif

using System;
using Tekly.Webster.FramelineCore;

namespace Tekly.Webster
{
	public static class Frameline
	{
		internal static IFramelineInstance Instance { get; private set; }

		public static string Json => Instance.Json;

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void InstantEvent(string id, string type)
		{
			Instance.InstantEvent(id, type);
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void BeginEvent(string id, string type)
		{
			Instance.BeginEventGetId(id, type);
		}

		public static int BeginEventGetId(string id, string type)
		{
#if FRAMELINE_ENABLE
			return Instance.BeginEventGetId(id, type);
#else
            return -1;
#endif
		}

		public static FramelineEventDisposable BeginEventDisposable(string id, string type)
		{
			return Instance.BeginEventDisposable(id, type);
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void EndEvent(int frameEventId)
		{
			Instance.EndEvent(frameEventId);
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void EndEvent(string id, string type)
		{
			Instance.EndEvent(id, type);
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Clear()
		{
			Instance.Clear();
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		internal static void Initialize()
		{
			Instance = CreateInstance();
			Instance.Initialize();
		}


#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Update()
		{
			Instance.Update();
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void ClearTo(double time)
		{
			Instance.ClearTo(time);
		}

#if !FRAMELINE_ENABLE
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void ClearAfter(double time)
		{
			Instance.ClearAfter(time);
		}

		public static void Stop()
		{
			Instance = null;
		}

		private static IFramelineInstance CreateInstance()
		{
#if FRAMELINE_ENABLE
			return new FramelineInstance();
#else
			return new FramelineInstanceDisabled();
#endif
		}
	}
}