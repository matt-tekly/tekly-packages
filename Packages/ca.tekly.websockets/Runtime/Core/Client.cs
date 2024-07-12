using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Tekly.WebSockets.Core
{
	public class Client
	{
		public readonly int Id;
		public bool Active { get; private set; }

		private TcpClient m_client;
		private NetworkStream m_stream;
		private StreamWriter m_writer;
		private Thread m_thread;

		public event Action<Client> Closed;
		public event Action<Client, string> ReceivedText;
		public event Action<Client, byte[]> ReceivedBinary;

		private readonly JsonSerializerSettings m_serializerSettings;

		public Client(WebSocketRequest request, TcpClient client, int id)
		{
			m_client = client;
			Id = id;

			m_serializerSettings = new JsonSerializerSettings();
			m_serializerSettings.Converters.Add(new StringEnumConverter());

			m_stream = m_client.GetStream();

			var secWebSocketAccept = Convert.ToBase64String(
				SHA1.Create().ComputeHash(
					Encoding.UTF8.GetBytes(request.SecurityKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
				)
			);

			var response = Encoding.UTF8.GetBytes(
				"HTTP/1.1 101 Switching Protocols\r\n" +
				"Connection: Upgrade\r\n" +
				"Upgrade: websocket\r\n" +
				"Sec-WebSocket-Accept: " + secWebSocketAccept + "\r\n\r\n");

			m_stream.Write(response, 0, response.Length);

			Active = true;

			m_thread = new Thread(Listen);
			m_thread.IsBackground = true;
			m_thread.Start();
		}

		public void Stop()
		{
			Close();
		}

		public void Send(string message)
		{
			if (!Active || m_client == null || m_stream == null) {
				Debug.LogError("Trying to send message to Connection that is not active.");
				return;
			}

			SendTextFrame(m_stream, message);
		}

		public void Send(byte[] message)
		{
			if (!Active || m_client == null || m_stream == null) {
				Debug.LogError("Trying to send message to Connection that is not active.");
				return;
			}

			SendBinaryFrame(m_stream, message);
		}

		private void Listen()
		{
			while (true) {
				try {
					while (m_client.Available < 2) { }

					var frame = WebSocketFrame.Parse(m_stream);

					if (frame.Opcode == OpCode.Text) {
						var receivedMessage = Encoding.UTF8.GetString(frame.PayloadData);
						ReceivedText?.Invoke(this, receivedMessage);
					} else if (frame.Opcode == OpCode.Binary) {
						ReceivedBinary?.Invoke(this, frame.PayloadData);
					} else if (frame.Opcode == OpCode.Ping) {
						Debug.LogError("Ping frames are unsupported");
					} else if (frame.Opcode == OpCode.Pong) {
						Debug.LogError("Pong frames are unsupported");
					} else if (frame.Opcode == OpCode.Close) {
						break;
					} else if (frame.Opcode == OpCode.Continuation) {
						Debug.LogError("Continuation frames are unsupported");
					} else {
						Debug.Log("Received: " + frame.Opcode);
					}
				} catch (ThreadAbortException) {
					break;
				} catch (Exception ex) {
					Debug.LogException(ex);
					break;
				}
			}

			m_client?.Close();
			m_client = null;

			Closed?.Invoke(this);
		}

		private void Close()
		{
			Closed?.Invoke(this);

			// TODO: This clean up logic needs to be better
			if (m_client != null) {
				SendCloseFrame(m_stream, 0);
			}

			Active = false;

			if (m_thread != null) {
				m_thread.Abort();
				m_thread = null;
			}

			m_client?.Close();
			m_stream?.Dispose();

			m_client = null;
			m_stream = null;
		}

		private static void SendTextFrame(Stream outputStream, string text)
		{
			var payload = Encoding.UTF8.GetBytes(text);
			WebSocketFrame.EncodeFrame(outputStream, true, OpCode.Text, payload);
		}

		private static void SendBinaryFrame(Stream outputStream, byte[] data)
		{
			WebSocketFrame.EncodeFrame(outputStream, true, OpCode.Binary, data);
		}

		public static void SendPingFrame(Stream outputStream, byte[] applicationData = null)
		{
			WebSocketFrame.EncodeFrame(outputStream, true, OpCode.Ping, applicationData ?? Array.Empty<byte>());
		}

		public static void SendPongFrame(Stream outputStream, byte[] applicationData = null)
		{
			WebSocketFrame.EncodeFrame(outputStream, true, OpCode.Pong, applicationData ?? Array.Empty<byte>());
		}

		private static void SendCloseFrame(Stream outputStream, ushort statusCode, string reason = null)
		{
			if (reason != null) {
				var reasonBytes = Encoding.UTF8.GetBytes(reason);
				var payload = new byte[2 + reasonBytes.Length];
				BinaryPrimitives.WriteUInt16BigEndian(payload, statusCode);
				reasonBytes.CopyTo(payload, 2);
				WebSocketFrame.EncodeFrame(outputStream, true, OpCode.Close, payload);
			} else {
				Span<byte> headerBuffer = stackalloc byte[2];
				BinaryPrimitives.WriteUInt16BigEndian(headerBuffer, statusCode);
				WebSocketFrame.EncodeFrame(outputStream, true, OpCode.Close, headerBuffer);
			}
		}
	}
}