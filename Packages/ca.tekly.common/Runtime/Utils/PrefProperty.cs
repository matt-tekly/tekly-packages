using UnityEngine;

namespace Tekly.Common.Utils
{
	public abstract class PrefProperty<T>
	{
		public T Value {
			get => Get();
			set => Set(value);
		}

		public abstract T Get();

		public abstract void Set(T value);

		public readonly string Key;

		public PrefProperty(string key, T defaultValue = default)
		{
			Key = key;

			if (!PlayerPrefs.HasKey(key)) {
				Set(defaultValue);
			}
		}
	}

	public class PrefPropertyString : PrefProperty<string>
	{
		public override string Get()
		{
			return PlayerPrefs.GetString(Key);
		}

		public override void Set(string value)
		{
			PlayerPrefs.SetString(Key, value);
		}

		public PrefPropertyString(string key, string defaultValue = null) : base(key, defaultValue) { }

		public static implicit operator string(PrefPropertyString pref)
		{
			return pref.Get();
		}
	}
}