using System;
using UnityEngine;

namespace Tekly.Common.Utils
{
	/// <summary>
	/// A typed PlayerPrefs entry that loads on first use and saves on every change.
	/// </summary>
	public abstract class PrefProperty<T>
	{
		public readonly string Key;
		public readonly T DefaultValue;

		public T Value {
			get => Get();
			set => Set(value);
		}

		private T m_value;
		private bool m_loaded;

		protected PrefProperty(string key, T defaultValue = default)
		{
			Key = key;
			DefaultValue = defaultValue;
		}

		public T Get()
		{
			if (!m_loaded) {
				m_value = PlayerPrefs.HasKey(Key) ? Read() : DefaultValue;
				m_loaded = true;
			}

			return m_value;
		}

		public void Set(T value)
		{
			Write(value);
			PlayerPrefs.Save();

			// Cache what a later load would read back, so the value doesn't change between sessions
			m_value = Read();
			m_loaded = true;
		}

		/// <summary>
		/// Removes the saved value, so Value is the default again.
		/// </summary>
		public void Clear()
		{
			PlayerPrefs.DeleteKey(Key);
			PlayerPrefs.Save();

			m_value = DefaultValue;
			m_loaded = true;
		}

		/// <summary>
		/// Reads the saved value. Only called when the key exists.
		/// </summary>
		protected abstract T Read();

		/// <summary>
		/// Writes the value to PlayerPrefs. Saving to disk is handled by the caller.
		/// </summary>
		protected abstract void Write(T value);

		public static implicit operator T(PrefProperty<T> pref)
		{
			return pref.Get();
		}

		public override string ToString()
		{
			return Get()?.ToString() ?? "";
		}
	}

	public class PrefPropertyString : PrefProperty<string>
	{
		public PrefPropertyString(string key, string defaultValue = "") : base(key, defaultValue) { }

		protected override string Read() => PlayerPrefs.GetString(Key);
		protected override void Write(string value) => PlayerPrefs.SetString(Key, value ?? "");
	}

	public class PrefPropertyInt : PrefProperty<int>
	{
		public PrefPropertyInt(string key, int defaultValue = 0) : base(key, defaultValue) { }

		protected override int Read() => PlayerPrefs.GetInt(Key, DefaultValue);
		protected override void Write(int value) => PlayerPrefs.SetInt(Key, value);
	}

	public class PrefPropertyFloat : PrefProperty<float>
	{
		public PrefPropertyFloat(string key, float defaultValue = 0f) : base(key, defaultValue) { }

		protected override float Read() => PlayerPrefs.GetFloat(Key, DefaultValue);
		protected override void Write(float value) => PlayerPrefs.SetFloat(Key, value);
	}

	/// <summary>
	/// A bool saved as an int, 1 for true and 0 for false.
	/// </summary>
	public class PrefPropertyBool : PrefProperty<bool>
	{
		public PrefPropertyBool(string key, bool defaultValue = false) : base(key, defaultValue) { }

		protected override bool Read() => PlayerPrefs.GetInt(Key, DefaultValue ? 1 : 0) != 0;
		protected override void Write(bool value) => PlayerPrefs.SetInt(Key, value ? 1 : 0);
	}

	/// <summary>
	/// An enum saved by name, so reordering the enum doesn't change saved values. A saved name that isn't one of
	/// its values reads as the default.
	/// </summary>
	public class PrefPropertyEnum<TEnum> : PrefProperty<TEnum> where TEnum : struct, Enum
	{
		public PrefPropertyEnum(string key, TEnum defaultValue = default) : base(key, defaultValue) { }

		protected override TEnum Read() => TryParse(PlayerPrefs.GetString(Key), out var value) ? value : DefaultValue;
		protected override void Write(TEnum value) => PlayerPrefs.SetString(Key, value.ToString());

		/// <summary>
		/// Parses a saved name, failing for anything that isn't one of the enum's values.
		/// </summary>
		private static bool TryParse(string saved, out TEnum value)
		{
			return Enum.TryParse(saved, out value) && Enum.IsDefined(typeof(TEnum), value);
		}
	}
}
