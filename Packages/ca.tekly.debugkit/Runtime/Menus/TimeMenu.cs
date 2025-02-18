using UnityEngine;

namespace Tekly.DebugKit.Menus
{
	public static class TimeMenu
	{
		private static string _test;
		private static bool _bool;
		
		public static void Register()
		{
			DebugKit.Instance.Menu("Time")
				.Property("Real Time", () => Time.realtimeSinceStartup, "{0:F2}")
				.Property("Frame Count", () => Time.frameCount)
				.Column("raised p4 r4 mv4", column => {
					column.FloatField("Time Scale", () => Time.timeScale, v => Time.timeScale = v)
						.SliderFloat(null, 0.00f, 3f, () => Time.timeScale, v => Time.timeScale = v);
				})
				.IntField("Target FPS", () => Application.targetFrameRate, v => Application.targetFrameRate = v);
		}
	}
}