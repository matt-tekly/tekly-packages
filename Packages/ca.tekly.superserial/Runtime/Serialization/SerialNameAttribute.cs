using System;
using System.Reflection;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Pins the name used to identify a field or type in serialized data, so the member can be renamed in code
	/// without orphaning existing data. On a field it also opts a private field in.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class SerialNameAttribute : Attribute
	{
		public readonly string Name;

		public SerialNameAttribute(string name)
		{
			if (string.IsNullOrEmpty(name)) {
				throw new ArgumentException("Serial name cannot be empty", nameof(name));
			}

			Name = name;
		}

		public static string GetName(FieldInfo field)
		{
			return field.GetCustomAttribute<SerialNameAttribute>()?.Name ?? field.Name;
		}

		public static string GetName(Type type)
		{
			return type.GetCustomAttribute<SerialNameAttribute>()?.Name ?? type.FullName;
		}
	}
}
