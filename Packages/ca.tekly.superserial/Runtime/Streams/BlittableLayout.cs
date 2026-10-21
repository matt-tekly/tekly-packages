using System;
using System.Collections.Generic;
using System.Reflection;
using Tekly.SuperSerial.Serialization;
using Unity.Collections.LowLevel.Unsafe;

namespace Tekly.SuperSerial.Streams
{
	/// <summary>
	/// Describes where each field of a blittable struct sits in memory so that raw memory written by one
	/// version of the struct can be loaded into another version by matching fields by name and size.
	/// </summary>
	public sealed class BlittableLayout
	{
		public readonly struct Field
		{
			public readonly uint NameHash;
			public readonly int Offset;
			public readonly int Size;

			public Field(uint nameHash, int offset, int size)
			{
				NameHash = nameHash;
				Offset = offset;
				Size = size;
			}
		}

		public readonly int Stride;
		public readonly Field[] Fields;

		private static readonly Dictionary<Type, BlittableLayout> s_cache = new Dictionary<Type, BlittableLayout>();

		private BlittableLayout(int stride, Field[] fields)
		{
			Stride = stride;
			Fields = fields;
		}

		public static BlittableLayout Get(Type type)
		{
			lock (s_cache) {
				if (!s_cache.TryGetValue(type, out var layout)) {
					layout = Create(type);
					s_cache.Add(type, layout);
				}

				return layout;
			}
		}

		public bool Matches(BlittableLayout other)
		{
			if (Stride != other.Stride || Fields.Length != other.Fields.Length) {
				return false;
			}

			for (var i = 0; i < Fields.Length; i++) {
				ref readonly var a = ref Fields[i];
				ref readonly var b = ref other.Fields[i];

				if (a.NameHash != b.NameHash || a.Offset != b.Offset || a.Size != b.Size) {
					return false;
				}
			}

			return true;
		}

		public void Write(TokenOutputStream output)
		{
			output.WriteVarUInt((ulong) Fields.Length);

			foreach (var field in Fields) {
				output.Write(field.NameHash);
				output.WriteVarUInt((ulong) field.Offset);
				output.WriteVarUInt((ulong) field.Size);
			}
		}

		public static BlittableLayout Read(TokenInputStream input, int stride)
		{
			var count = (int) input.ReadVarUInt();
			var fields = new Field[count];

			for (var i = 0; i < count; i++) {
				var hash = input.ReadUInt();
				var offset = (int) input.ReadVarUInt();
				var size = (int) input.ReadVarUInt();

				if (offset < 0 || size < 0 || offset + size > stride) {
					throw new SuperSerialException("Blittable layout field lies outside its stride");
				}

				fields[i] = new Field(hash, offset, size);
			}

			return new BlittableLayout(stride, fields);
		}

		/// <summary>
		/// Copies every source field that has a destination field with the same name and size.
		/// The destination is expected to be zeroed already.
		/// </summary>
		public static void Migrate(ReadOnlySpan<byte> source, BlittableLayout sourceLayout, Span<byte> destination, BlittableLayout destinationLayout, int count)
		{
			var pairs = new List<(Field source, Field destination)>();

			foreach (var sourceField in sourceLayout.Fields) {
				foreach (var destinationField in destinationLayout.Fields) {
					if (sourceField.NameHash == destinationField.NameHash && sourceField.Size == destinationField.Size) {
						pairs.Add((sourceField, destinationField));
						break;
					}
				}
			}

			for (var i = 0; i < count; i++) {
				var sourceElement = source.Slice(i * sourceLayout.Stride, sourceLayout.Stride);
				var destinationElement = destination.Slice(i * destinationLayout.Stride, destinationLayout.Stride);

				foreach (var (sourceField, destinationField) in pairs) {
					sourceElement.Slice(sourceField.Offset, sourceField.Size)
						.CopyTo(destinationElement.Slice(destinationField.Offset, destinationField.Size));
				}
			}
		}

		private static BlittableLayout Create(Type type)
		{
			var stride = UnsafeUtility.SizeOf(type);

			if (type.IsPrimitive || type.IsEnum) {
				return new BlittableLayout(stride, new[] { new Field(0, 0, stride) });
			}

			var fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			var fields = new Field[fieldInfos.Length];

			for (var i = 0; i < fieldInfos.Length; i++) {
				var fieldInfo = fieldInfos[i];
				var fieldType = fieldInfo.FieldType;

				if (fieldType.IsEnum) {
					fieldType = Enum.GetUnderlyingType(fieldType);
				}

				var hash = XXHash.Hash32(SerialNameAttribute.GetName(fieldInfo));
				fields[i] = new Field(hash, UnsafeUtility.GetFieldOffset(fieldInfo), UnsafeUtility.SizeOf(fieldType));
			}

			Array.Sort(fields, (a, b) => a.Offset.CompareTo(b.Offset));

			return new BlittableLayout(stride, fields);
		}
	}
}
