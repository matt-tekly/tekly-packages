//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Text;
using Tekly.Webster.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.Webster.Routes.GameObjects
{
	[Serializable]
	public class GameObjectSummary : IToJson
	{
		public GameObjectSummary[] Children;

		public bool Active;
		public bool ActiveSelf;

		public int InstanceId;
		public bool IsScene;
		public string Name;
		public string Path;
		public string Layer;
		
		public ComponentInfo[] Components = new ComponentInfo[0];

		public GameObjectSummary()
		{
			
		}
		
		public GameObjectSummary(Scene scene)
		{
			Name = scene.name;
			Path = scene.name;
			Active = scene.isLoaded;
			ActiveSelf = scene.isLoaded;
			IsScene = true;

			var roots = scene.GetRootGameObjects();
			Children = new GameObjectSummary[roots.Length];

			InstanceId = scene.GetHashCode();

			for (var i = 0; i < roots.Length; i++) {
				var childGo = roots[i];
				Children[i] = new GameObjectSummary(childGo, childGo.name);
			}
		}

		public GameObjectSummary(GameObject go, string path)
		{
			Name = go.name;
			Path = path;
			InstanceId = go.GetInstanceID();

			Active = go.activeInHierarchy;
			ActiveSelf = go.activeSelf;

			Layer = LayerMask.LayerToName(go.layer);
			var components = go.GetComponents<Component>();
			Components = new ComponentInfo[components.Length];

			for (var i = 0; i < Components.Length; i++) {
				Components[i] = new ComponentInfo(components[i]);
			}

			var t = go.transform;
			Children = new GameObjectSummary[t.childCount];

			for (var i = 0; i < t.childCount; i++) {
				var child = t.GetChild(i);
				var childGo = child.gameObject;
				Children[i] = new GameObjectSummary(childGo, path + "/" + childGo.name);
			}
		}

		public void ToJson(StringBuilder sb)
		{
			sb.Append("{");
			sb.Write("Name", Name, true);
			sb.Write("Active", Active, true);
			sb.Write("ActiveSelf", ActiveSelf, true);
			sb.Write("InstanceId", InstanceId, true);
			sb.Write("IsScene", IsScene, true);
			sb.Write("Path", Path, true);
			sb.Write("Layer", Layer, true);
			sb.Write("Components", Components, true);
			sb.Write("Children", Children, false);
			sb.Append("}");
		}
	}
}