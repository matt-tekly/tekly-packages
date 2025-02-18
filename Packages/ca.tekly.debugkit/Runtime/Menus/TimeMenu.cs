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
				.FloatField("Time Scale", () => Time.timeScale, v => Time.timeScale = v)
				.SliderFloat(null, 0.01f, 3f, () => Time.timeScale, v => Time.timeScale = v)
				.TextField("Test Field", () => _test, v => _test = v)
				.Button("Button", () => Debug.Log("Poopy"))
				.Foldout("Foldout", foldout => {
					foldout.Property("Real Time", () => Time.realtimeSinceStartup, "{0:F2}")
						.FloatField("Time Scale", () => Time.timeScale, v => Time.timeScale = v)
						.SliderFloat(null, 0.01f, 3f, () => Time.timeScale, v => Time.timeScale = v)
						.TextField("Test Field", () => _test, v => _test = v)
						.Button("Button", () => foldout.Detach())
						.Checkbox("Checkit", () => _bool, v => _bool = v);
				});
		}
	}
}