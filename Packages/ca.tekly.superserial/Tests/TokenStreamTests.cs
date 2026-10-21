using System;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	/// <summary>
	/// Hands back at most one byte per Read call, the way a network or compressed stream might.
	/// </summary>
	public class TrickleStream : Stream
	{
		private readonly Stream m_inner;

		public TrickleStream(Stream inner)
		{
			m_inner = inner;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return m_inner.Read(buffer, offset, Math.Min(count, 1));
		}

		public override int Read(Span<byte> buffer)
		{
			return m_inner.Read(buffer.Slice(0, Math.Min(buffer.Length, 1)));
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	public class TokenStreamTests
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct BlitV1
		{
			public int Health;
			public float Speed;
			public byte Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BlitV2
		{
			public float Speed;
			public int Armor;
			public int Health;
			public byte Flags;
		}

		private enum SmallEnum : byte { A, B, C }

		private static MemoryStream Written(Action<TokenOutputStream> write, int bufferSize = TokenOutputStream.DEFAULT_BUFFER_SIZE)
		{
			var memory = new MemoryStream();

			using (var output = new TokenOutputStream(memory, bufferSize, true)) {
				write(output);
			}

			memory.Position = 0;
			return memory;
		}

		[Test]
		public void HeaderRoundTrips()
		{
			using var memory = Written(_ => { });
			Assert.AreEqual(TokenOutputStream.HEADER_SIZE, memory.Length);

			using var input = new TokenInputStream(memory, true);
			Assert.AreEqual(TokenOutputStream.VERSION, input.Version);
			Assert.AreEqual(TokenOutputStream.HEADER_SIZE, input.Position);
		}

		[Test]
		public void RejectsNonSuperSerialData()
		{
			using var memory = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
			Assert.Throws<SuperSerialException>(() => new TokenInputStream(memory, true));
		}

		[Test]
		public void RejectsNewerVersion()
		{
			using var memory = Written(_ => { });
			memory.GetBuffer()[4] = TokenOutputStream.VERSION + 1;
			Assert.Throws<SuperSerialException>(() => new TokenInputStream(memory, true));
		}

		[Test]
		public void RejectsVersionOlderThanMinimum()
		{
			using var memory = Written(_ => { });
			memory.GetBuffer()[4] = TokenOutputStream.MIN_VERSION - 1;
			Assert.Throws<SuperSerialException>(() => new TokenInputStream(memory, true));
		}

		[Test]
		public void RawValuesRoundTrip()
		{
			var date = DateTime.Now;

			using var memory = Written(o => {
				o.Write(true);
				o.Write((short) -12345);
				o.Write((ushort) 54321);
				o.Write(int.MinValue);
				o.Write(uint.MaxValue);
				o.Write(long.MinValue);
				o.Write(ulong.MaxValue);
				o.Write(3.25f);
				o.Write(Math.PI);
				o.Write(date);
				o.Write(new Vector2(1, 2));
				o.Write(new Vector3(1, 2, 3));
				o.Write(new Vector4(1, 2, 3, 4));
				o.Write(new Quaternion(1, 2, 3, 4));
			});

			using var input = new TokenInputStream(memory, true);
			Assert.AreEqual(true, input.ReadBoolean());
			Assert.AreEqual(-12345, input.ReadShort());
			Assert.AreEqual(54321, input.ReadUShort());
			Assert.AreEqual(int.MinValue, input.ReadInt());
			Assert.AreEqual(uint.MaxValue, input.ReadUInt());
			Assert.AreEqual(long.MinValue, input.ReadLong());
			Assert.AreEqual(ulong.MaxValue, input.ReadULong());
			Assert.AreEqual(3.25f, input.ReadFloat());
			Assert.AreEqual(Math.PI, input.ReadDouble());
			Assert.AreEqual(date, input.ReadDate());
			Assert.AreEqual(new Vector2(1, 2), input.ReadVector2());
			Assert.AreEqual(new Vector3(1, 2, 3), input.ReadVector3());
			Assert.AreEqual(new Vector4(1, 2, 3, 4), input.ReadVector4());
			Assert.AreEqual(new Quaternion(1, 2, 3, 4), input.ReadQuaternion());
		}

		[Test]
		public void VarintsRoundTrip()
		{
			var unsigned = new ulong[] { 0, 1, 127, 128, 300, uint.MaxValue, ulong.MaxValue };
			var signed = new[] { 0L, 1, -1, 63, -64, 64, -65, int.MaxValue, int.MinValue, long.MaxValue, long.MinValue };

			using var memory = Written(o => {
				foreach (var v in unsigned) o.WriteVarUInt(v);
				foreach (var v in signed) o.WriteVarInt(v);
			});

			using var input = new TokenInputStream(memory, true);
			foreach (var v in unsigned) Assert.AreEqual(v, input.ReadVarUInt());
			foreach (var v in signed) Assert.AreEqual(v, input.ReadVarInt());
		}

		[Test]
		public void SmallVarintsAreOneByte()
		{
			using var memory = Written(o => {
				o.WriteVarUInt(127);
				o.WriteVarInt(-64);
				o.WriteVarInt(63);
			});

			Assert.AreEqual(TokenOutputStream.HEADER_SIZE + 3, memory.Length);
		}

		[Test]
		public void StringsRoundTrip()
		{
			var big = new string('x', 20_000);
			var unicode = "héllo wörld – 日本語 🎮";

			using var memory = Written(o => {
				o.Write((string) null);
				o.Write("");
				o.Write("ascii");
				o.Write(unicode);
				o.Write(big);
				o.Write("after");
			});

			using var input = new TokenInputStream(memory, true);
			Assert.IsNull(input.ReadString());
			Assert.AreEqual("", input.ReadString());
			Assert.AreEqual("ascii", input.ReadString());
			Assert.AreEqual(unicode, input.ReadString());
			Assert.AreEqual(big, input.ReadString());
			Assert.AreEqual("after", input.ReadString());
		}

		[Test]
		public void SmallBufferStillWritesEverything()
		{
			var big = new string('y', 5000);

			using var memory = Written(o => {
				for (var i = 0; i < 1000; i++) {
					o.Write(i);
				}

				o.Write(big);
				o.Write(42);
			}, 64);

			using var input = new TokenInputStream(memory, true, 64);
			for (var i = 0; i < 1000; i++) {
				Assert.AreEqual(i, input.ReadInt());
			}

			Assert.AreEqual(big, input.ReadString());
			Assert.AreEqual(42, input.ReadInt());
		}

		[Test]
		public void ReadsFromAStreamThatTrickles()
		{
			var big = new string('z', 10_000);

			using var memory = Written(o => {
				o.Write(123456789L);
				o.Write(big);
				o.Write(new Vector3(1, 2, 3));
				o.WriteVarUInt(ulong.MaxValue);
			});

			using var input = new TokenInputStream(new TrickleStream(memory), true);
			Assert.AreEqual(123456789L, input.ReadLong());
			Assert.AreEqual(big, input.ReadString());
			Assert.AreEqual(new Vector3(1, 2, 3), input.ReadVector3());
			Assert.AreEqual(ulong.MaxValue, input.ReadVarUInt());
		}

		[Test]
		public void ThrowsAtEndOfStream()
		{
			using var memory = Written(o => o.Write(1));

			using var input = new TokenInputStream(memory, true);
			input.ReadInt();
			Assert.Throws<EndOfStreamException>(() => input.ReadInt());
		}

		[Test]
		public void SkipsEveryWireType()
		{
			using var memory = Written(o => {
				o.WriteWireType(WireType.Null);
				o.WriteWireType(WireType.Varint);
				o.WriteVarUInt(ulong.MaxValue);
				o.WriteWireType(WireType.Fixed32);
				o.Write(1);
				o.WriteWireType(WireType.Fixed64);
				o.Write(1L);
				o.WriteWireType(WireType.Bytes);
				o.WriteUtf8("some bytes");

				o.WriteWireType(WireType.Object);
				o.WriteField(1, 10);
				o.WriteField(2, "nested");
				o.WriteFieldHash(3);
				o.WriteWireType(WireType.Object);
				o.WriteField(4, 1.5f);
				o.WriteEndObject();
				o.WriteEndObject();

				o.WriteWireType(WireType.TypedObject);
				o.Write(0xDEADBEEF);
				o.WriteWireType(WireType.Varint);
				o.WriteVarInt(-5);

				o.WriteWireType(WireType.List);
				o.WriteVarUInt(2);
				o.WriteWireType(WireType.Varint);
				o.WriteVarInt(1);
				o.WriteWireType(WireType.Null);

				o.WriteWireType(WireType.Map);
				o.WriteVarUInt(1);
				o.WriteWireType(WireType.Bytes);
				o.WriteUtf8("key");
				o.WriteWireType(WireType.Fixed64);
				o.Write(2.0);

				o.WriteWireType(WireType.Block);
				o.BeginBlock();
				o.Write("inside a block");
				o.Write(new Quaternion(1, 2, 3, 4));
				o.EndBlock();

				o.Write(0x7777);
			});

			foreach (var seekable in new[] { true, false }) {
				memory.Position = 0;
				Stream source = seekable ? memory : new TrickleStream(memory);

				using var input = new TokenInputStream(source, true);
				for (var i = 0; i < 10; i++) {
					input.SkipValue();
				}

				Assert.AreEqual(0x7777, input.ReadInt(), seekable ? "seekable" : "trickle");
			}
		}

		[Test]
		public void RawCannotBeSkipped()
		{
			using var memory = Written(o => {
				o.WriteWireType(WireType.Raw);
				o.Write(1);
			});

			using var input = new TokenInputStream(memory, true);
			Assert.Throws<SuperSerialException>(() => input.SkipValue());
		}

		[Test]
		public void NestedBlocksPatchTheirLengths()
		{
			using var memory = Written(o => {
				o.BeginBlock();
				o.Write(1);
				o.BeginBlock();
				o.Write(new string('a', 10_000));
				o.EndBlock();
				o.Write(2);
				o.EndBlock();
				o.Write(3);
			}, 64);

			using var input = new TokenInputStream(memory, true);

			var outerLength = input.ReadBlockLength();
			var outerEnd = input.Position + outerLength;

			Assert.AreEqual(1, input.ReadInt());

			var innerLength = input.ReadBlockLength();
			input.SkipBytes(innerLength);

			Assert.AreEqual(2, input.ReadInt());
			Assert.AreEqual(outerEnd, input.Position);
			Assert.AreEqual(3, input.ReadInt());
		}

		[Test]
		public void BlittableRoundTripsSameLayout()
		{
			var data = new BlitV1[3];
			for (var i = 0; i < data.Length; i++) {
				data[i] = new BlitV1 { Health = i * 10, Speed = i * 0.5f, Flags = (byte) i };
			}

			using var memory = Written(o => o.WriteBlittable(data, 2));

			using var input = new TokenInputStream(memory, true);
			var result = new BlitV1[2];
			input.ReadBlittable(result, 2);

			Assert.AreEqual(data[0], result[0]);
			Assert.AreEqual(data[1], result[1]);
		}

		[Test]
		public void BlittableOfPrimitivesRoundTrips()
		{
			var ints = new[] { 1, -2, 3, int.MaxValue };
			var enums = new[] { SmallEnum.C, SmallEnum.A };

			using var memory = Written(o => {
				o.WriteBlittable(ints, ints.Length);
				o.WriteBlittable(enums, enums.Length);
			});

			using var input = new TokenInputStream(memory, true);
			var intsOut = new int[ints.Length];
			var enumsOut = new SmallEnum[enums.Length];
			input.ReadBlittable(intsOut, ints.Length);
			input.ReadBlittable(enumsOut, enums.Length);

			CollectionAssert.AreEqual(ints, intsOut);
			CollectionAssert.AreEqual(enums, enumsOut);
		}

		[Test]
		public void BlittableMigratesWhenLayoutChanges()
		{
			var data = new[] {
				new BlitV1 { Health = 100, Speed = 1.5f, Flags = 7 },
				new BlitV1 { Health = -3, Speed = 9f, Flags = 255 },
			};

			using var memory = Written(o => {
				o.WriteBlittable(data, data.Length);
				o.Write(0xBEEF);
			});


			using var input = new TokenInputStream(memory, true);
			var result = new[] { new BlitV2 { Armor = 55 }, new BlitV2 { Armor = 66 } };
			input.ReadBlittable(result, result.Length);

			for (var i = 0; i < data.Length; i++) {
				Assert.AreEqual(data[i].Health, result[i].Health);
				Assert.AreEqual(data[i].Speed, result[i].Speed);
				Assert.AreEqual(data[i].Flags, result[i].Flags);
				Assert.AreEqual(0, result[i].Armor, "unmatched fields are zeroed");
			}

			Assert.AreEqual(0xBEEF, input.ReadInt());
		}

		[Test]
		public void BlittableRejectsCountMismatch()
		{
			var data = new BlitV1[4];

			using var memory = Written(o => o.WriteBlittable(data, 4));

			using var input = new TokenInputStream(memory, true);
			Assert.Throws<SuperSerialException>(() => input.ReadBlittable(new BlitV1[4], 3));
		}

		[Test]
		public void BlittableRejectsNonBlittable()
		{
			using var memory = new MemoryStream();
			using var output = new TokenOutputStream(memory, leaveOpen: true);
			Assert.Throws<SuperSerialException>(() => output.WriteBlittable(new[] { ("a", 1) }, 1));
		}

		[Test]
		public void DisposeFlushesAndClosesUnlessAskedNotTo()
		{
			var closed = new MemoryStream();
			using (new TokenOutputStream(closed)) { }
			Assert.IsFalse(closed.CanWrite);

			var open = new MemoryStream();
			using (var output = new TokenOutputStream(open, leaveOpen: true)) {
				output.Write(5);
			}

			Assert.IsTrue(open.CanWrite);
			Assert.AreEqual(TokenOutputStream.HEADER_SIZE + sizeof(int), open.Length);
		}
	}
}
