using System;
using System.IO;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Serialization
{
	public class SuperSerializer
	{
		private readonly SuperSerializerSettings m_settings;
		private int m_registriesApplied;

		public SuperSerializerSettings Settings => m_settings;

		public SuperSerializer() : this(new SuperSerializerSettings()) { }

		public SuperSerializer(SuperSerializerSettings settings)
		{
			m_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			ApplyPendingRegistries();
		}

		/// <summary>
		/// Writes a value as the top level of a stream. ISuperSerialize values are written raw and everything
		/// else is written as a tagged value.
		/// </summary>
		public void Write(TokenOutputStream output, object obj)
		{
			if (obj is ISuperSerialize superSerialize) {
				output.WriteWireType(WireType.Raw);
				superSerialize.Write(output, this);
				return;
			}

			WriteValue(output, obj?.GetType() ?? typeof(object), obj);
		}

		public T Read<T>(TokenInputStream input, T existing = default)
		{
			return (T) Read(input, typeof(T), existing);
		}

		public object Read(TokenInputStream input, Type type, object existing)
		{
			return ReadValue(input, type, existing);
		}

		/// <summary>
		/// Writes to a temporary file and swaps it in, so a crash mid write can't leave a truncated file behind.
		/// </summary>
		public void Write(string filePath, object obj)
		{
			var tempPath = filePath + ".tmp";

			using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var output = new TokenOutputStream(stream)) {
				Write(output, obj);
			}

			if (File.Exists(filePath)) {
				File.Replace(tempPath, filePath, null);
			} else {
				File.Move(tempPath, filePath);
			}
		}

		public T Read<T>(string filePath, T existing = default)
		{
			using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var input = new TokenInputStream(stream);

			return (T) ReadValue(input, typeof(T), existing);
		}


		/// <summary>
		/// Writes [WireType][payload] for a value. Null is written as WireType.Null. A value whose runtime type
		/// differs from its declared type is wrapped in TypedObject so the reader can recreate the right type.
		/// </summary>
		public void WriteValue(TokenOutputStream output, Type declaredType, object value)
		{
			if (value == null) {
				output.WriteWireType(WireType.Null);
				return;
			}

			var actualType = value.GetType();
			declaredType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

			if (actualType != declaredType) {
				output.WriteWireType(WireType.TypedObject);
				output.Write(m_settings.Converters.RegisterType(actualType));
			}

			var converter = GetConverter(actualType);
			output.WriteWireType(converter.WireType);

			if (converter.WireType == WireType.Block) {
				output.BeginBlock();
				converter.Write(this, output, value);
				output.EndBlock();
			} else {
				converter.Write(this, output, value);
			}
		}

		/// <summary>
		/// Writes [WireType][payload] for a value whose declared type is T. Generated code uses this for every
		/// field it has no inline path for.
		/// </summary>
		public void WriteValue<T>(TokenOutputStream output, T value)
		{
			WriteValue(output, typeof(T), value);
		}

		/// <summary>
		/// Reads a value of declared type T whose wire type has already been consumed. A Null payload gives
		/// default(T), and a payload that can't be read as T is skipped and existing is returned.
		/// </summary>
		public T ReadPayload<T>(TokenInputStream input, WireType wireType, T existing)
		{
			var value = ReadPayload(input, wireType, typeof(T), existing);
			return value == null ? default : (T) value;
		}

		/// <summary>
		/// Reads a tagged value. If the data can't be read as the declared type it is skipped and existing is
		/// returned.
		/// </summary>
		public object ReadValue(TokenInputStream input, Type declaredType, object existing)
		{
			return ReadPayload(input, input.ReadWireType(), declaredType, existing);
		}

		/// <summary>
		/// Reads a value whose wire type has already been consumed. If the payload can't be read as the declared
		/// type it is skipped and existing is returned.
		/// </summary>
		public object ReadPayload(TokenInputStream input, WireType wireType, Type declaredType, object existing)
		{
			return TryReadPayload(input, wireType, declaredType, existing, out var value) ? value : existing;
		}

		/// <summary>
		/// Reads a value whose wire type has already been consumed. Returns false, having skipped the payload,
		/// when it can't be read as the declared type, so the caller can leave its current value alone.
		/// </summary>
		public bool TryReadPayload(TokenInputStream input, WireType wireType, Type declaredType, object existing, out object value)
		{
			value = ReadPayloadOrSkip(input, wireType, declaredType, existing, out var skipped);
			return !skipped;
		}

		private object ReadPayloadOrSkip(TokenInputStream input, WireType wireType, Type declaredType, object existing, out bool skipped)
		{
			skipped = false;

			switch (wireType) {
				case WireType.Null:
					return null;

				case WireType.TypedObject: {
					var typeHash = input.ReadUInt();

					if (!m_settings.Converters.TryGetType(typeHash, out var actualType)
					    && !(ApplyPendingRegistries() && m_settings.Converters.TryGetType(typeHash, out actualType))) {
						throw new SuperSerialException($"Unknown type hash {typeHash} for a value declared as [{declaredType}]. " +
						                               "The concrete type must be registered on the reading side before loading: call the generated " +
						                               "Tekly.SuperSerial.Generated.SuperSerialRegistry_<Assembly>.RegisterAll(serializer) for the assembly that declares it, " +
						                               "or RegisterType<T>() by hand. Generated registries apply themselves unless Settings.AutoRegister is off.");
					}

					if (existing != null && existing.GetType() != actualType) {
						existing = null;
					}

					return ReadValue(input, actualType, existing);
				}

				case WireType.Raw: {
					var converter = GetConverter(declaredType);
					if (!converter.Accepts(WireType.Raw)) {
						throw new SuperSerialException($"[{declaredType}] does not implement ISuperSerialize but the stream holds a raw value");
					}

					return converter.Read(this, input, existing);
				}

				default: {
					var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
					var converter = GetConverter(type);

					if (!converter.Accepts(wireType)) {
						Debug.LogWarning($"[SuperSerial] Expected {converter.WireType} for [{type}] but found {wireType}. The field's type has probably changed. Skipping.");
						input.Skip(wireType);
						skipped = true;
						return existing;
					}

					if (wireType != WireType.Block) {
						return converter.Read(this, input, existing);
					}

					var length = input.ReadBlockLength();
					var end = input.Position + length;
					var result = converter.Read(this, input, existing);

					if (input.Position > end) {
						throw new SuperSerialException($"[{type}] read {input.Position - end} bytes past the end of its block");
					}

					if (input.Position < end) {
						// The writer put more in the block than this reader understands. That's allowed.
						input.SkipBytes(end - input.Position);
					}

					return result;
				}
			}
		}

		public SerialConverter GetConverter(Type type)
		{
			return m_settings.Converters.Get(type);
		}

		public void RegisterConverter<T>(SerialConverter<T> converter)
		{
			m_settings.Converters.Register(converter);
		}

		/// <summary>
		/// Registers a type that can appear as a value of a base class or interface typed field, so it can be
		/// recreated on read.
		/// </summary>
		public void RegisterType<T>()
		{
			m_settings.Converters.RegisterType(typeof(T));
		}

		public void RegisterType(Type type)
		{
			m_settings.Converters.RegisterType(type);
		}

		/// <summary>
		/// Registers how to create a T when reading, so the ObjectFactory isn't consulted. The generated registry
		/// registers every [SuperSerializable] class's CreateForSerialization here.
		/// </summary>
		public void RegisterFactory<T>(Func<T> factory) where T : class
		{
			m_settings.Converters.RegisterFactory(typeof(T), factory);
		}

		/// <summary>
		/// Applies any generated registries that have appeared since the last call. Returns true if there were any.
		/// </summary>
		public bool ApplyPendingRegistries()
		{
			return m_settings.AutoRegister && SuperSerialRegistries.Apply(this, ref m_registriesApplied);
		}

		public object CreateInstance(Type type)
		{
			return m_settings.Factory.CreateInstance(type);
		}
	}
}
