using System.Text.RegularExpressions;
using Tekly.Tinker.Core;
using UnityEngine;

namespace Tekly.Tinker.Routing
{
	/// <summary>
	/// Marks a function as being a command in Tinker Terminal
	/// </summary>
	public class CommandAttribute : TinkerPreserveAttribute
	{
		public string Name;

		private static readonly Regex s_validPattern = new Regex("^[a-zA-Z0-9._-]+$");
		
		public CommandAttribute(string name)
		{
			Name = name;
			
			if (!s_validPattern.IsMatch(name)) {
				Debug.LogError($"Command name [{name}] must only contain letters, numbers, periods, underscores and dashes");
				Name = s_validPattern.Replace(name, "_");
			}
		}
	}
}