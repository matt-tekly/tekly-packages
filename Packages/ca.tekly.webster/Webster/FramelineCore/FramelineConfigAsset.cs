//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using Tekly.Webster.FramelineCore.Utility;
using UnityEngine;

namespace Tekly.Webster.FramelineCore
{
	[CreateAssetMenu(menuName = "Tekly/Frameline/Config", fileName = "framelineconfig")]
	public class FramelineConfigAsset : ScriptableObject
	{
		public FramelineConfig Config;

#if UNITY_EDITOR
		public void Reset()
		{
			Config = new FramelineConfig {
				LongFrameTimeMs = 70,
				GeneralEvent = FramelineConfig.Create("General", "#7C7C7CFF"),
				Events = new[] {
					FramelineConfig.Create("Event 1", "#ff6b6b"),
					FramelineConfig.Create("Event 2", "#d5703f"),
					FramelineConfig.Create("Event 3", "#a57324"),
					FramelineConfig.Create("Event 4", "#76701f"),
					FramelineConfig.Create("Event 5", "#4d672a"),
					FramelineConfig.Create("Event 6", "#295b36"),
					FramelineConfig.Create("Event 7", "#074d3d"),
					FramelineConfig.Create("Event 8", "#003d3d")
				}
			};
		}
#endif
	}

	[Serializable]
	public class FramelineConfig
	{
		public FramelineEventConfig[] Events;

		public FramelineEventConfig GeneralEvent = Create("General", "#7C7C7CFF");

		/// <summary>
		/// Long frames will log an event
		/// </summary>
		public int LongFrameTimeMs = 70;

		public static FramelineEventConfig Create(string id, string color)
		{
			return new FramelineEventConfig {
				Id = id,
				Color = color
			};
		}
	}

	[Serializable]
	public class FramelineEventConfig
	{
		[ColorHtmlProperty] public string Color = "#fffffff";

		public string Id;
	}
}