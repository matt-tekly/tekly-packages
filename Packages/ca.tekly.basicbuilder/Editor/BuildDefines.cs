using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Tekly.BasicBuilder
{
	public static class BuildDefines
	{
		private const string CLIENT_BUILDER_DEFINES_KEY = "ClientBuilder.Defines";

		private static List<string> s_builderDefines;

		public static string[] Defines => s_builderDefines.Where(x => !string.IsNullOrEmpty(x)).ToArray();

		static BuildDefines()
		{
			s_builderDefines = GetBuilderDefines();
		}

		public static void Set(string define, bool enabled)
		{
			if (s_builderDefines.Contains(define)) {
				if (!enabled) {
					s_builderDefines.Remove(define);
				} 
			} else {
				if (enabled) {
					s_builderDefines.Add(define);
				}
			}
			
			EditorPrefs.SetString(CLIENT_BUILDER_DEFINES_KEY, string.Join(',', s_builderDefines));
		}

		public static bool IsSet(string define)
		{
			return s_builderDefines.Contains(define);
		}
		
		private static List<string> GetBuilderDefines()
		{
			var strings = EditorPrefs.GetString(CLIENT_BUILDER_DEFINES_KEY);
            
			if (strings == null) {
				return new List<string>();
			}

			return strings.Split(",").Where(x => !string.IsNullOrEmpty(x)).ToList();
		}
	}
}