using System;

namespace Tekly.Common.Utils
{
	[Serializable]
	public struct OptionalBool
	{
		public bool IsSet;
		public bool Value;

		public OptionalBool(bool value)
		{
			IsSet = true;
			Value = value;
		}

		public bool GetValueOrDefault(bool fallback = default)
		{
			return IsSet ? Value : fallback;
		}

		public bool TryGetValue(out bool value)
		{
			value = Value;
			return IsSet;
		}

		public static implicit operator OptionalBool(bool value)
		{
			return new OptionalBool(value);
		}

		public static implicit operator OptionalBool(bool? value)
		{
			return value.HasValue ? new OptionalBool(value.Value) : default;
		}

		public static implicit operator bool?(OptionalBool optional)
		{
			return optional.IsSet ? optional.Value : null;
		}

		public override string ToString()
		{
			return IsSet ? Value.ToString() : "<unset>";
		}
	}
	
	[Serializable]
	public struct OptionalInt
	{
		public bool IsSet;
		public int Value;

		public OptionalInt(int value)
		{
			IsSet = true;
			Value = value;
		}

		public int GetValueOrDefault(int fallback = default)
		{
			return IsSet ? Value : fallback;
		}

		public bool TryGetValue(out int value)
		{
			value = Value;
			return IsSet;
		}

		public static implicit operator OptionalInt(int value)
		{
			return new OptionalInt(value);
		}

		public static implicit operator OptionalInt(int? value)
		{
			return value.HasValue ? new OptionalInt(value.Value) : default;
		}

		public static implicit operator int?(OptionalInt optional)
		{
			return optional.IsSet ? optional.Value : null;
		}

		public override string ToString()
		{
			return IsSet ? Value.ToString() : "<unset>";
		}
	}
	
	[Serializable]
	public struct OptionalFloat
	{
		public bool IsSet;
		public float Value;

		public OptionalFloat(float value)
		{
			IsSet = true;
			Value = value;
		}

		public float GetValueOrDefault(float fallback = default)
		{
			return IsSet ? Value : fallback;
		}

		public bool TryGetValue(out float value)
		{
			value = Value;
			return IsSet;
		}

		public static implicit operator OptionalFloat(float value)
		{
			return new OptionalFloat(value);
		}

		public static implicit operator OptionalFloat(float? value)
		{
			return value.HasValue ? new OptionalFloat(value.Value) : default;
		}

		public static implicit operator float?(OptionalFloat optional)
		{
			return optional.IsSet ? optional.Value : null;
		}

		public override string ToString()
		{
			return IsSet ? Value.ToString() : "<unset>";
		}
	}
	
	[Serializable]
	public struct OptionalDouble
	{
		public bool IsSet;
		public double Value;

		public OptionalDouble(double value)
		{
			IsSet = true;
			Value = value;
		}

		public double GetValueOrDefault(double fallback = default)
		{
			return IsSet ? Value : fallback;
		}

		public bool TryGetValue(out double value)
		{
			value = Value;
			return IsSet;
		}

		public static implicit operator OptionalDouble(double value)
		{
			return new OptionalDouble(value);
		}

		public static implicit operator OptionalDouble(double? value)
		{
			return value.HasValue ? new OptionalDouble(value.Value) : default;
		}

		public static implicit operator double?(OptionalDouble optional)
		{
			return optional.IsSet ? optional.Value : null;
		}

		public override string ToString()
		{
			return IsSet ? Value.ToString() : "<unset>";
		}
	}
	
	[Serializable]
	public struct OptionalString
	{
		public bool IsSet;
		public string Value;

		public OptionalString(string value)
		{
			IsSet = true;
			Value = value;
		}

		public string GetValueOrDefault(string fallback = default)
		{
			return IsSet ? Value : fallback;
		}

		public bool TryGetValue(out string value)
		{
			value = Value;
			return IsSet;
		}

		public static implicit operator OptionalString(string value)
		{
			return new OptionalString(value);
		}

		public override string ToString()
		{
			return IsSet ? Value ?? "<null>" : "<unset>";
		}
	}
}