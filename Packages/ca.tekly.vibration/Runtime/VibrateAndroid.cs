#if UNITY_ANDROID
using UnityEngine;

namespace Tekly.Vibration
{
	public class VibrateAndroid : IVibrate
	{
		private readonly AndroidJavaObject m_vibrator;
		private readonly AndroidJavaObject m_context;

		private readonly AndroidJavaClass m_vibrationEffect;
		private readonly int m_androidSdkLevel;

		public VibrateAndroid()
		{
			var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

			m_vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
			m_context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

			m_androidSdkLevel = GetSdkLevel();

			if (m_androidSdkLevel >= 26) {
				m_vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
			}
		}

		public void Pop()
		{
			Vibrate(50);
		}

		public void Peak()
		{
			Vibrate(100);
		}

		public void Negative()
		{
			long[] pattern = {0, 50, 50, 50};
			Vibrate(pattern, -1);
		}

		public bool HasVibration()
		{
			var contextClass = new AndroidJavaClass("android.content.Context");
			var contextVibratorService = contextClass.GetStatic<string>("VIBRATOR_SERVICE");
			var systemService = m_context.Call<AndroidJavaObject>("getSystemService", contextVibratorService);

			return systemService.Call<bool>("hasVibrator");
		}

		private void Vibrate(long milliseconds)
		{
			if (m_androidSdkLevel >= 26) {
				var createOneShot = m_vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1);
				m_vibrator.Call("vibrate", createOneShot);
			} else {
				m_vibrator.Call("vibrate", milliseconds);
			}
		}

		private void Vibrate(long[] pattern, int repeat)
		{
			if (m_androidSdkLevel >= 26) {
				var createWaveform = m_vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", pattern, repeat);
				m_vibrator.Call("vibrate", createWaveform);
			} else {
				m_vibrator.Call("vibrate", pattern, repeat);
			}
		}

		private static int GetSdkLevel()
		{
			using var version = new AndroidJavaClass("android.os.Build$VERSION");
			return version.GetStatic<int>("SDK_INT");
		}
	}
}

#endif