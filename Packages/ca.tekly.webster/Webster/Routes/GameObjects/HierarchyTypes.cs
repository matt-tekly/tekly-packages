//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Linq;
using System.Text;
using Tekly.Webster.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.Webster.Routes.GameObjects
{
	[Serializable]
	public class HierarchySummary : IToJson
	{
		private const string DONT_DESTROY_ON_LOAD = "DontDestroyOnLoad";

		public GameObjectSummary[] GameObjects;

		public HierarchySummary()
		{
			var sceneCount = SceneManager.sceneCount;

			GameObjects = new GameObjectSummary[sceneCount + 1];

			for (var i = 0; i < sceneCount; i++) {
				var scene = SceneManager.GetSceneAt(i);
				GameObjects[i] = new GameObjectSummary(scene);
			}

			var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

			var dontDestroys = allGameObjects
				.Where(x => x.transform.parent == null && x.scene.buildIndex == -1)
				.Select(x => new GameObjectSummary(x, string.Format("{0}/{1}", DONT_DESTROY_ON_LOAD, x.name)))
				.ToArray();

			GameObjects[sceneCount] = new GameObjectSummary {
				Active = true,
				ActiveSelf = true,
				IsScene = true,
				Name = DONT_DESTROY_ON_LOAD,
				Path = DONT_DESTROY_ON_LOAD,
				Children = dontDestroys
			};
		}

		public void ToJson(StringBuilder sb)
		{
			sb.Append("{");
			sb.Write("GameObjects", GameObjects, false);
			sb.Append("}");
		}
	}
}