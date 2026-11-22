using UnityEngine;
using UnityEngine.Audio;

namespace Tekly.Thunk
{
	/// <summary>
	/// Helpers for driving exposed AudioMixer volume parameters with linear 0 to 1 values.
	/// </summary>
	public static class ThunkMixer
	{
		public static void SetVolume(AudioMixer mixer, string id, double linearValue)
		{
			mixer.SetFloat(id, ToDecibel((float) linearValue));
		}

		public static float GetVolume(AudioMixer mixer, string id)
		{
			mixer.GetFloat(id, out var volume);
			return ToLinear(volume);
		}

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
