using System;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// Bindings over a PlayerPrefs key, so a control's value is kept between sessions, e.g.
	/// <code>page.TextInput("Path", PrefBinding.String("game.debug.path"));</code>
	/// The value is read from PlayerPrefs once, when the binding is made, and saved on every write. Read the
	/// binding from code for the current value. onChanged runs after each write with the new value.
	/// </summary>
	public static class PrefBinding
	{
		public static Binding<string> String(string key, string defaultValue = "", Action<string> onChanged = null)
		{
			var value = PlayerPrefs.GetString(key, defaultValue);

			return new Binding<string>(() => value, newValue => {
				value = newValue ?? string.Empty;
				PlayerPrefs.SetString(key, value);
				Save(onChanged, value);
			});
		}

		public static Binding<int> Int(string key, int defaultValue = 0, Action<int> onChanged = null)
		{
			var value = PlayerPrefs.GetInt(key, defaultValue);

			return new Binding<int>(() => value, newValue => {
				value = newValue;
				PlayerPrefs.SetInt(key, value);
				Save(onChanged, value);
			});
		}

		public static Binding<float> Float(string key, float defaultValue = 0f, Action<float> onChanged = null)
		{
			var value = PlayerPrefs.GetFloat(key, defaultValue);

			return new Binding<float>(() => value, newValue => {
				value = newValue;
				PlayerPrefs.SetFloat(key, value);
				Save(onChanged, value);
			});
		}

		public static Binding<bool> Bool(string key, bool defaultValue = false, Action<bool> onChanged = null)
		{
			var value = PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;

			return new Binding<bool>(() => value, newValue => {
				value = newValue;
				PlayerPrefs.SetInt(key, value ? 1 : 0);
				Save(onChanged, value);
			});
		}

		private static void Save<T>(Action<T> onChanged, T value)
		{
			// Saved straight away: on WebGL and mobile the app can be closed without warning
			PlayerPrefs.Save();
			onChanged?.Invoke(value);
		}
	}
}
