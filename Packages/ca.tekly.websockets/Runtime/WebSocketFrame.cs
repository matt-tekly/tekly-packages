using System;
using System.Buffers.Binary;
using System.IO;

namespace Tekly.WebSockets
{
	public enum OpCode : byte
	{
		Continuation = 0x0,
		Text = 0x1,
		Binary = 0x2,
		Close = 0x8,
		Ping = 0x9,
		Pong = 0xa
	}

	public class WebSocketFrame
	{
		public bool Fin { get; private set; }
		public bool Rsv1 { get; private set; }
		public bool Rsv2 { get; private set; }
		public bool Rsv3 { get; private set; }
		public OpCode Opcode { get; private set; }
		public bool Mask { get; private set; }
		public byte[] MaskingKey { get; private set; }
		
		public int PayloadLength { get; private set; }
		public byte[] PayloadData { get; private set; }

		public override string ToString()
		{
			return $"Fin: {Fin}, Opcode: {Opcode}, Mask: {Mask}, PayloadLength: {PayloadLength}";
		}

		public static WebSocketFrame Parse(Stream stream)
		{
			var frame = new WebSocketFrame();

			Span<byte> headerBuffer = stackalloc byte[14]; // Max header size (2 + 8 + 4)

			// Read first two bytes
			if (stream.Read(headerBuffer.Slice(0, 2)) != 2) {
				throw new EndOfStreamException("Unable to read WebSocket frame header");
			}

			// Parse first byte
			frame.Fin = (headerBuffer[0] & 0x80) == 0x80;
			frame.Rsv1 = (headerBuffer[0] & 0x40) == 0x40;
			frame.Rsv2 = (headerBuffer[0] & 0x20) == 0x20;
			frame.Rsv3 = (headerBuffer[0] & 0x10) == 0x10;
			frame.Opcode = (OpCode)(byte)(headerBuffer[0] & 0x0f);

			// Parse second byte
			frame.Mask = (headerBuffer[1] & 0b10000000) != 0;
			frame.PayloadLength = (int)(ulong)(headerBuffer[1] & 0b01111111);

			var headerSize = 2;
			
			if (frame.PayloadLength == 126) {
				if (stream.Read(headerBuffer.Slice(2, 2)) != 2) {
					throw new EndOfStreamException("Unable to read extended payload length");
				}

				frame.PayloadLength = BinaryPrimitives.ReadUInt16BigEndian(headerBuffer.Slice(2, 2));
				headerSize += 2;
			} else if (frame.PayloadLength == 127) {
				if (stream.Read(headerBuffer.Slice(2, 8)) != 8) {
					throw new EndOfStreamException("Unable to read extended payload length");
				}

				// This isn't quite right, the maximum payload length is truncated to an int here
				// I can't imagine a case for having 2 gigs in a single frame
				frame.PayloadLength = (int)BinaryPrimitives.ReadUInt64BigEndian(headerBuffer.Slice(2, 8));
				headerSize += 8;
			}
			
			if (frame.Mask) {
				if (stream.Read(headerBuffer.Slice(headerSize, 4)) != 4) {
					throw new EndOfStreamException("Unable to read masking key");
				}

				frame.MaskingKey = headerBuffer.Slice(headerSize, 4).ToArray();
			}
			
			frame.PayloadData = new byte[frame.PayloadLength];
			var totalBytesRead = 0;
			while (totalBytesRead < frame.PayloadLength) {
				var bytesRead = stream.Read(frame.PayloadData.AsSpan(totalBytesRead));
				if (bytesRead == 0) {
					throw new EndOfStreamException("Unable to read full payload");
				}

				totalBytesRead += bytesRead;
			}
			
			if (frame.Mask) {
				for (var i = 0; i < frame.PayloadLength; i++) {
					frame.PayloadData[i] ^= frame.MaskingKey[i % 4];
				}
			}

			return frame;
		}

		public static void EncodeFrame(Stream outputStream, bool fin, OpCode opcode, ReadOnlySpan<byte> payload)
		{
			Span<byte> headerBuffer = stackalloc byte[10]; // Max header size (2 + 8)
			var headerSize = 2;

			// Set the first byte (FIN bit and opcode)
			headerBuffer[0] = (byte)((fin ? 0x80 : 0x00) | (byte)opcode);

			// Set the second byte (payload length)
			if (payload.Length < 126) {
				headerBuffer[1] = (byte)payload.Length;
			} else if (payload.Length < 65536) {
				headerBuffer[1] = 126;
				BinaryPrimitives.WriteUInt16BigEndian(headerBuffer.Slice(2), (ushort)payload.Length);
				headerSize += 2;
			} else {
				headerBuffer[1] = 127;
				BinaryPrimitives.WriteUInt64BigEndian(headerBuffer.Slice(2), (ulong)payload.Length);
				headerSize += 8;
			}

			// Write the header
			outputStream.Write(headerBuffer.Slice(0, headerSize));

			// Write the payload
			outputStream.Write(payload);
		}
		
		public static void SendCloseFrame(Stream outputStream, ushort statusCode, string reason = null)
		{
			byte[] payload;
			if (reason != null) {
				byte[] reasonBytes = System.Text.Encoding.UTF8.GetBytes(reason);
				payload = new byte[2 + reasonBytes.Length];
				BinaryPrimitives.WriteUInt16BigEndian(payload, statusCode);
				reasonBytes.CopyTo(payload, 2);
			} else {
				payload = new byte[2];
				BinaryPrimitives.WriteUInt16BigEndian(payload, statusCode);
			}

			EncodeFrame(outputStream, true, OpCode.Close, payload);
		}
	}
}