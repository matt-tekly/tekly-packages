#if UNITY_EDITOR_WIN
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TeklySample.Clipboard.Editor
{
	public static class TeklyClipboard
	{
		public static void CopyToClipboard(Texture2D texture)
		{
			if (texture == null) {
				Debug.LogWarning("No image data submitted");
				return;
			}

			CopyToClipboard(texture.EncodeToPNG());
		}

		private static void CopyToClipboard(byte[] pngBytes)
		{
			var success = false;

			using (var pngStream = new MemoryStream(pngBytes)) {
				using var pngImage = new Bitmap(pngStream);
				var rawBytes = ImageToBmp(pngImage);

				// I'm not sure why, but sometimes the SetClipboardData call fails. Even the official .net libraries
				// retry a few times.
				for (var i = 0; i < 10; i++) {
					var rawBytesPtr = CopyToMovableMemory(rawBytes);
					OpenClipboard(IntPtr.Zero);
					EmptyClipboard();
					success = SetClipboardData(8, rawBytesPtr).ToInt64() != 0;
					Marshal.FreeHGlobal(rawBytesPtr);
					CloseClipboard();
					if (success) {
						break;
					}
				}
			}

			if (success == false) {
				Debug.LogError("Unable to copy image to clipboard");
			}
		}

		private static byte[] ImageToBmp(Image image)
		{
			var memoryStream = new MemoryStream();
			image.Save(memoryStream, ImageFormat.Bmp);
			var buffer = memoryStream.GetBuffer();
			var destinationArray = new byte[buffer.Length];
			Array.Copy(buffer, 14, destinationArray, 0, buffer.Length - 14);
			return destinationArray;
		}

		private static IntPtr CopyToMovableMemory(byte[] data)
		{
			var movableMemory = Win32GlobalAlloc(2 | 8192, data.Length);

			if (movableMemory == IntPtr.Zero) {
				throw new Win32Exception();
			}

			var destination = movableMemory != IntPtr.Zero ? Win32GlobalLock(movableMemory);

			if (destination == IntPtr.Zero) {
				throw new Win32Exception();
			}

			Marshal.Copy(data, 0, destination, data.Length);
			Win32GlobalUnlock(movableMemory);
			return movableMemory;
		}

		[DllImport("user32.dll")]
		private static extern bool OpenClipboard(IntPtr hWndNewOwner);

		[DllImport("user32.dll")]
		private static extern bool EmptyClipboard();

		[DllImport("user32.dll")]
		private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

		[DllImport("user32.dll")]
		private static extern bool CloseClipboard();

		[DllImport("kernel32.dll", EntryPoint = "GlobalAlloc", CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr Win32GlobalAlloc(uint flags, int dwBytes);

		[DllImport("kernel32.dll", EntryPoint = "GlobalLock", CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr Win32GlobalLock(IntPtr hMem);

		[DllImport("kernel32.dll", EntryPoint = "GlobalUnlock", CallingConvention = CallingConvention.StdCall)]
		private static extern IntPtr Win32GlobalUnlock(IntPtr hMem);
	}
}
#endif