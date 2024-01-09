using System;
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
	
	public delegate void WriterDelegate<T>(ref T target, TokenOutputStream output, SuperSerializer superSerializer)
		where T : struct;

	public delegate void ReaderDelegate<T>(ref T target, TokenInputStream input, SuperSerializer superSerializer)
		where T : struct;

	public static class SuperSerializeStructs<T> where T : struct
	{
		public static readonly WriterDelegate<T> Write;
		public static readonly ReaderDelegate<T> Read;

		static SuperSerializeStructs()
		{
			if (!typeof(ISuperSerialize).IsAssignableFrom(typeof(T))) {
				throw new Exception($"Type {typeof(T)} does not derive from ISuperSerialize");
			}
			
			Write = Get<WriterDelegate<T>>(nameof(Trick.Write));
			Read = Get<ReaderDelegate<T>>(nameof(Trick.Read));
		}

		private static TDelegate Get<TDelegate>(string name) where TDelegate : Delegate
		{
			var method = typeof(Trick).GetMethod(name).MakeGenericMethod(typeof(T));
			return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), method);
		}

		static class Trick
		{
			public static void Write<U>(ref U target, TokenOutputStream output, SuperSerializer superSerializer)
				where U : struct, ISuperSerialize
			{
				target.Write(output, superSerializer);
			}

			public static void Read<U>(ref U target, TokenInputStream input, SuperSerializer superSerializer)
				where U : struct, ISuperSerialize
			{
				target.Read(input, superSerializer);
			}
		}
	}
}