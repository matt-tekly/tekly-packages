#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Tekly.DevBoard
{
	/// <summary>
	/// Keeps the browser from opening its own find-in-page on Ctrl+F while DevBoard has focus, so the shortcut reaches
	/// DevBoard's search. Does nothing outside WebGL builds.
	/// </summary>
	internal static class BrowserKeys
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		private static bool s_blockFind;

		[DllImport("__Internal")]
		private static extern void DevBoard_SetBlockFind(bool block);
#endif

		public static void SetBlockFind(bool block)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			if (s_blockFind == block) {
				return;
			}

			s_blockFind = block;
			DevBoard_SetBlockFind(block);
#endif
		}
	}
}
