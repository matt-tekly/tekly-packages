using UnityEngine;
using UnityEngine.Scripting;

namespace Tekly.Vibration
{
	public interface IVibrate
	{
		void Pop();
		void Peak();
		void Negative();

		bool HasVibration();
	}

	public static class Vibrate
	{
		private static readonly IVibrate s_impl;

		static Vibrate()
		{
#if UNITY_EDITOR
			s_impl = new VibrateStub();
#elif UNITY_IOS
			s_impl = new VibrateIos();
#elif UNITY_ANDROID
			s_impl = new VibrateAndroid();
#else
			s_impl = new VibrateStub();
#endif
		}

		///<summary>
		/// Tiny pop vibration
		///</summary>
		public static void Pop()
		{
			s_impl.Pop();
		}

		///<summary>
		/// Small peak vibration
		///</summary>
		public static void Peak()
		{
			s_impl.Peak();
		}

		///<summary>
		/// 3 small vibrations
		///</summary>
		public static void Negative()
		{
			s_impl.Negative();
		}

		public static bool HasVibration()
		{
			return s_impl.HasVibration();
		}

		/// <summary>
		/// Hack to make Unity add the correct permissions requests to the build automatically
		/// </summary>
		[Preserve]
		private static void VibrateHack()
		{
			Handheld.Vibrate();
		}
	}
}