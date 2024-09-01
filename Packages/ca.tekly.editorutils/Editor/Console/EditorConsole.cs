using System;
using System.Reflection;

namespace Tekly.EditorUtils.Console
{
	public static class EditorConsole
	{
		public static void Clear()
		{
			var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
			var clearMethod = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
			clearMethod?.Invoke(null, null);
		}
	}
}