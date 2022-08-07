#if UNITY_IOS
using System.Runtime.InteropServices;

namespace Tekly.Vibration
{
	public class VibrateIos : IVibrate
	{
		public void Pop()
		{
			_VibratePop();
		}

		public void Peak()
		{
			_VibratePeak();
		}

		public void Negative()
		{
			_VibrateNegative();
		}

		public bool HasVibration()
		{
			return _HasVibration();
		}

		[DllImport("__Internal")]
		private static extern bool _HasVibration();

		[DllImport("__Internal")]
		private static extern void _VibratePop();

		[DllImport("__Internal")]
		private static extern void _VibratePeak();

		[DllImport("__Internal")]
		private static extern void _VibrateNegative();
	}
}
#endif