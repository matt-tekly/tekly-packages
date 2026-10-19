using System;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Bindings over a PlayerPrefs key, so a control's value is kept between sessions, e.g.
	/// <code>page.TextInput("Path", PrefBinding.String("game.debug.path"));</code>
	/// The value is read from PlayerPrefs once, when the binding is made, and written on every write; the prefs are
	/// flushed to disk once at the end of a frame something was written in, so dragging a bound slider doesn't
	/// flush every frame. Read the binding from code for the current value. onChanged runs after each write with
	/// the new value.
	/// </summary>
	public static class PrefBinding
	{
		// The frame a save is already queued for. A frame number rather than a flag, so a save that never ran
		// (play mode ended that frame, with domain reloads off) can't leave the next session believing one is
		// still pending
		private static int s_saveQueuedFrame = -1;

		public static Binding<string> String(string key, string defaultValue = "", Action<string> onChanged = null)
		{
			var value = PlayerPrefs.GetString(key, defaultValue);

			return new Binding<string>(() => value, newValue => {
				value = newValue ?? string.Empty;
				PlayerPrefs.SetString(key, value);
				Written(onChanged, value);
			});
		}

		public static Binding<int> Int(string key, int defaultValue = 0, Action<int> onChanged = null)
		{
			var value = PlayerPrefs.GetInt(key, defaultValue);

			return new Binding<int>(() => value, newValue => {
				value = newValue;
				PlayerPrefs.SetInt(key, value);
				Written(onChanged, value);
			});
		}

		public static Binding<float> Float(string key, float defaultValue = 0f, Action<float> onChanged = null)
		{
			var value = PlayerPrefs.GetFloat(key, defaultValue);

			return new Binding<float>(() => value, newValue => {
				value = newValue;
				PlayerPrefs.SetFloat(key, value);
				Written(onChanged, value);
			});
		}

		public static Binding<bool> Bool(string key, bool defaultValue = false, Action<bool> onChanged = null)
		{
			var value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

			return new Binding<bool>(() => value, newValue => {
				value = newValue;
				PlayerPrefs.SetInt(key, value ? 1 : 0);
				Written(onChanged, value);
			});
		}

		private static void Written<T>(Action<T> onChanged, T value)
		{
			QueueSave();
			onChanged?.Invoke(value);
		}

		/// <summary>
		/// Flushes the prefs at the end of the frame, once however many bindings were written. Saved straight away
		/// outside play mode, where frames don't end. On WebGL and mobile the app can be closed without warning, so
		/// it isn't left until later than that.
		/// </summary>
		private static void QueueSave()
		{
			if (!Application.isPlaying) {
				PlayerPrefs.Save();
				return;
			}

			if (s_saveQueuedFrame == Time.frameCount) {
				return;
			}

			s_saveQueuedFrame = Time.frameCount;
			SaveAtEndOfFrame();
		}

		private static async void SaveAtEndOfFrame()
		{
			try {
				await Awaitable.EndOfFrameAsync();
			} catch (OperationCanceledException) {
				// Play mode ended first. Still worth saving what was written
			}

			PlayerPrefs.Save();
		}
	}
}
