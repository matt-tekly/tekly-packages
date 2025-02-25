using UnityEngine;

namespace Tekly.DebugKit.Menus
{
	public static class TimeMenu
	{
#if DEBUGKIT_DISABLED
		[System.Diagnostics.Conditional("__UNDEFINED__")]
#endif
		public static void Register()
		{
			DebugKit.Instance.Menu("Time")
				.Property("Real Time", () => Time.realtimeSinceStartup, "{0:F2}")
				.Property("Frame Count", () => Time.frameCount)
				.CardColumn(column => {
					column.FloatField("Time Scale", () => Time.timeScale, v => Time.timeScale = v)
						.SliderFloat(null, 0.00f, 3f, () => Time.timeScale, v => Time.timeScale = v);
				})
				.IntField("Target FPS", () => Application.targetFrameRate, v => Application.targetFrameRate = v);
		}
	}
}