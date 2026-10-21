using System;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Marks a partial class or struct for the SuperSerial source generator. The generator emits an
	/// ISuperSerialize implementation that writes and reads the type's fields without reflection, and registers
	/// the type with the generated registry.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public sealed class SuperSerializableAttribute : Attribute
	{
		/// <summary>
		/// When true, every value typed field whose value equals its declared default is left out of the data,
		/// and comes back as that default on read. Class and collection typed fields are never omitted unless
		/// marked [SerialOptional].
		/// </summary>
		public bool OmitDefaults;
	}

	/// <summary>
	/// Opts one field of a [SuperSerializable] type into omit-defaults. A value typed field is left out when it
	/// equals its declared default. A reference typed field is left out only when it is null.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false)]
	public sealed class SerialOptionalAttribute : Attribute
	{
	}
}
