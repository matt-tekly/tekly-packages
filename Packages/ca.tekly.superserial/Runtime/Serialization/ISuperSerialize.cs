using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Interface for an object to implement its own serialization
	/// </summary>
	public interface ISuperSerialize
	{
		void Write(TokenOutputStream output, SuperSerializer superSerializer);
		void Read(TokenInputStream input, SuperSerializer superSerializer);
	}

	/// <summary>
	/// Implemented by the code the source generator emits. A generated type's payload is a field list ended
	/// by a zero hash, the same layout the reflection converter writes, so the serializer lets it read data
	/// that was written as WireType.Object before the type was annotated.
	/// </summary>
	public interface ISuperSerialGenerated : ISuperSerialize
	{
	}

	/// <summary>
	/// Writes and reads one struct in place. SuperSerializeStructs&lt;T&gt; hands these out for any struct that
	/// implements ISuperSerialize, with no boxing and no reflection.
	/// </summary>
	public delegate void WriterDelegate<T>(ref T target, TokenOutputStream output, SuperSerializer superSerializer)
		where T : struct;

	public delegate void ReaderDelegate<T>(ref T target, TokenInputStream input, SuperSerializer superSerializer)
		where T : struct;

	/// <summary>
	/// Ref access to a struct's ISuperSerialize methods. The delegates are built through a constrained generic
	/// instantiation, which is reached through SuperSerializeStructs&lt;T&gt;.Register, so every instantiation
	/// is a plain compiled method and nothing needs MakeGenericMethod. The generated registry registers every
	/// [SuperSerializable] struct; a hand written struct registers itself with Register&lt;T&gt;() once at startup,
	/// and until something does, Write and Read fall back to boxing through the interface.
	/// </summary>
	public static class SuperSerializeStructs<T> where T : struct
	{
		public static WriterDelegate<T> Write { get; private set; }
		public static ReaderDelegate<T> Read { get; private set; }

		public static bool IsRegistered => Write != null;

		static SuperSerializeStructs()
		{
			if (!typeof(ISuperSerialize).IsAssignableFrom(typeof(T))) {
				throw new SuperSerialException($"Type {typeof(T)} does not implement ISuperSerialize");
			}

			Write = WriteBoxed;
			Read = ReadBoxed;
		}

		/// <summary>
		/// Installs the constrained, non boxing delegates. Called by the generated registry.
		/// </summary>
		public static void Register<TSerial>() where TSerial : struct, ISuperSerialize
		{
			if (typeof(TSerial) != typeof(T)) {
				throw new SuperSerialException($"SuperSerializeStructs<{typeof(T)}>.Register was given {typeof(TSerial)}");
			}

			Write = (WriterDelegate<T>) (object) new WriterDelegate<TSerial>(Constrained<TSerial>.Write);
			Read = (ReaderDelegate<T>) (object) new ReaderDelegate<TSerial>(Constrained<TSerial>.Read);
		}

		private static void WriteBoxed(ref T target, TokenOutputStream output, SuperSerializer superSerializer)
		{
			((ISuperSerialize) target).Write(output, superSerializer);
		}

		private static void ReadBoxed(ref T target, TokenInputStream input, SuperSerializer superSerializer)
		{
			object boxed = target;
			((ISuperSerialize) boxed).Read(input, superSerializer);
			target = (T) boxed;
		}

		private static class Constrained<TSerial> where TSerial : struct, ISuperSerialize
		{
			public static void Write(ref TSerial target, TokenOutputStream output, SuperSerializer superSerializer)
			{
				target.Write(output, superSerializer);
			}

			public static void Read(ref TSerial target, TokenInputStream input, SuperSerializer superSerializer)
			{
				target.Read(input, superSerializer);
			}
		}
	}
}
