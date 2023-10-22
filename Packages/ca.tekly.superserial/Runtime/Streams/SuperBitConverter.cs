using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tekly.SuperSerial.Streams
{
	public static class SuperBitConverter
	{
		// The reads here could be faster by implementing them manually.
		// BitConverter has several checks that we could just throw away and we could do alignment writing.
		// See here for an implementation to optimize: https://referencesource.microsoft.com/#mscorlib/system/bitconverter.cs

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ReadInt(byte[] bytes, int position)
		{
			return BitConverter.ToInt32(bytes, position);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint ReadUInt(byte[] bytes, int position)
		{
			return BitConverter.ToUInt32(bytes, position);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ReadLong(byte[] bytes, int position)
		{
			return BitConverter.ToInt64(bytes, position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ReadFloat(byte[] bytes, int position)
		{
			return BitConverter.ToSingle(bytes, position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double ReadDouble(byte[] bytes, int position)
		{
			return BitConverter.ToDouble(bytes, position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ReadBool(byte[] bytes, int position)
		{
			return bytes[position] > 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ReadString(byte[] bytes, int position, int bytesToDecode)
		{
			return Encoding.UTF8.GetString(bytes, position, bytesToDecode);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Write(byte[] bytes, int position, string value)
		{
			return Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, position);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Write(byte[] bytes, int position, int value)
		{
			fixed (byte* b = bytes) {
				*(int*) (b + position) = value;
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Write(byte[] bytes, int position, uint value)
		{
			fixed (byte* b = bytes) {
				*(uint*) (b + position) = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Write(byte[] bytes, int position, float value)
		{
			fixed (byte* b = bytes) {
				*(float*) (b + position) = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Write(byte[] bytes, int position, double value)
		{
			fixed (byte* b = bytes) {
				*(double*) (b + position) = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe void Write(byte[] bytes, int position, long value)
		{
			fixed (byte* b = bytes) {
				*(long*) (b + position) = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(byte[] bytes, int position, bool value)
		{
			bytes[position] = value ? (byte) 1 : (byte) 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Write(byte[] bytes, int position, DateTime value)
		{
			Write(bytes, position, value.ToBinary());
		}
	}
}