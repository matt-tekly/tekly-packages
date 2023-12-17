using UnityEngine;

namespace Tekly.Lofi.Core
{
	public static class Constants
	{
		public const int INVALID_ID = -1;

		public static float ToDecibel(float linear)
		{
			return linear > 0 ? 20.0f * Mathf.Log10(linear) : -144.0f;
		}

		public static float ToLinear(float decibel)
		{
			return Mathf.Pow(10.0f, decibel / 20.0f);
		}
	}
}