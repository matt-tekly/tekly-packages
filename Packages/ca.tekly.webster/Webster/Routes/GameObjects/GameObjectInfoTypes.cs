//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Text;
using Tekly.Webster.Utility;
using UnityEngine;

namespace Tekly.Webster.Routes.GameObjects
{
	[Serializable]
	public class GameObjectQuery : IToJson
	{
		public bool Found;
		public GameObjectSummary GameObjectSummary;

		public GameObjectQuery(string path)
		{
			var go = GameObject.Find(path);
			Found = go != null;

			if (Found) {
				GameObjectSummary = new GameObjectSummary(go, path);
			}
		}

		public void ToJson(StringBuilder sb)
		{
			if (Found) {
				sb.Append("{");
				sb.Write("Found", Found, true);
				sb.Write("GameObjectSummary", GameObjectSummary, false);
				sb.Append("}");	
			} else {
				sb.Append("{");
				sb.Write("Found", Found, false);
				sb.Append("}");	
			}
		}
	}
}