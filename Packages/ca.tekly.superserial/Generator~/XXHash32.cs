using System;

namespace Tekly.SuperSerial.Generator
{
	/// <summary>
	/// XXHash32 over the UTF-16 bytes of a string, matching Tekly.SuperSerial.Serialization.XXHash.Hash32(string)
	/// at runtime. HashTests in the package tests prove the two agree.
	/// </summary>
	public static class XXHash32
	{
		private const uint PRIME1 = 2654435761u;
		private const uint PRIME2 = 2246822519u;
		private const uint PRIME3 = 3266489917u;
		private const uint PRIME4 = 668265263u;
		private const uint PRIME5 = 374761393u;

		public static uint Hash(string input, uint seed = 0)
		{
			var bytes = new byte[input.Length * 2];
			for (var i = 0; i < input.Length; i++) {
				var c = (ushort) input[i];
				bytes[i * 2] = (byte) c;
				bytes[i * 2 + 1] = (byte) (c >> 8);
			}

			return Hash(bytes, seed);
		}

		public static uint Hash(byte[] buffer, uint seed = 0)
		{
			var len = buffer.Length;
			var offset = 0;
			uint acc;

			if (len >= 16) {
				var acc1 = seed + PRIME1 + PRIME2;
				var acc2 = seed + PRIME2;
				var acc3 = seed;
				var acc4 = seed - PRIME1;

				var evenLength = len - len % 16;
				do {
					acc1 = Round(acc1, ReadUInt32(buffer, offset));
					acc2 = Round(acc2, ReadUInt32(buffer, offset + 4));
					acc3 = Round(acc3, ReadUInt32(buffer, offset + 8));
					acc4 = Round(acc4, ReadUInt32(buffer, offset + 12));
					offset += 16;
				} while (offset < evenLength);

				acc = RotateLeft(acc1, 1) + RotateLeft(acc2, 7) + RotateLeft(acc3, 12) + RotateLeft(acc4, 18);
			} else {
				acc = seed + PRIME5;
			}

			acc += (uint) len;

			while (len - offset >= 4) {
				acc += ReadUInt32(buffer, offset) * PRIME3;
				acc = RotateLeft(acc, 17) * PRIME4;
				offset += 4;
			}

			while (offset < len) {
				acc += buffer[offset] * PRIME5;
				acc = RotateLeft(acc, 11) * PRIME1;
				offset++;
			}

			acc ^= acc >> 15;
			acc *= PRIME2;
			acc ^= acc >> 13;
			acc *= PRIME3;
			acc ^= acc >> 16;
			return acc;
		}

		private static uint Round(uint acc, uint lane)
		{
			acc += lane * PRIME2;
			acc = RotateLeft(acc, 13);
			return acc * PRIME1;
		}

		private static uint RotateLeft(uint value, int bits)
		{
			return (value << bits) | (value >> (32 - bits));
		}

		private static uint ReadUInt32(byte[] buffer, int offset)
		{
			return buffer[offset] | ((uint) buffer[offset + 1] << 8) | ((uint) buffer[offset + 2] << 16) | ((uint) buffer[offset + 3] << 24);
		}
	}
}
