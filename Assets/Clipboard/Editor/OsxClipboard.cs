#if UNITY_EDITOR_OSX
using System;
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

		private static void CopyToClipboard(byte[] imageData)
		{
			var pinnedImage = Marshal.AllocHGlobal(imageData.Length);

			try {
				Marshal.Copy(imageData, 0, pinnedImage, imageData.Length);
				var nsData = SendMessage(GetClass("NSData"), GetSelector("dataWithBytes:length:"), pinnedImage, imageData.Length);
				var nsImageHandle = SendMessage(GetClass("NSImage"), GetSelector("alloc"));
				var nsImagePtr = SendMessage(nsImageHandle, GetSelector("initWithData:"), nsData);
				var nsArrayPtr = SendMessage(GetClass("NSArray"), GetSelector("arrayWithObject:"), nsImagePtr);

				var nsPasteboard = GetClass("NSPasteboard");
				var generalPasteboard = SendMessage(nsPasteboard, GetSelector("generalPasteboard"));
				SendMessage(generalPasteboard, GetSelector("clearContents"));
				SendMessage(generalPasteboard, GetSelector("writeObjects:"), nsArrayPtr);
			} finally {
				Marshal.FreeHGlobal(pinnedImage);
			}
		}

		[DllImport("/System/Library/Frameworks/AppKit.framework/AppKit", EntryPoint = "objc_getClass")]
		private static extern IntPtr GetClass(string className);

		[DllImport("/System/Library/Frameworks/AppKit.framework/AppKit", EntryPoint = "sel_registerName")]
		private static extern IntPtr GetSelector(string selectorName);

		[DllImport("/System/Library/Frameworks/AppKit.framework/AppKit", EntryPoint = "objc_msgSend")]
		private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector);

		[DllImport("/System/Library/Frameworks/AppKit.framework/AppKit", EntryPoint = "objc_msgSend")]
		private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg1);

		[DllImport("/System/Library/Frameworks/AppKit.framework/AppKit", EntryPoint = "objc_msgSend")]
		private static extern IntPtr SendMessage(IntPtr receiver, IntPtr selector, IntPtr arg1, int arg2);
	}
}
#endif