using System;
using System.Reflection;

namespace Tekly.Common.Utils
{
	public static class ClearConsole
	{
		public static void Go()
		{
			var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
			var clearMethod = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
			clearMethod?.Invoke(null, null);
		}
	}
}