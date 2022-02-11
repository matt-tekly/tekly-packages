//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Tekly.Webster.Routes
{
	[Serializable]
	public class SceneInfo
	{
		public bool IsLoaded;
		public string Name;
		public string Path;

		public SceneInfo(Scene scene)
		{
			Name = scene.name;
			Path = scene.path;
			IsLoaded = scene.isLoaded;
		}
	}

	[Serializable]
	public class ScenesInfo
	{
		public List<SceneInfo> ActiveScenes = new List<SceneInfo>();

		public ScenesInfo()
		{
			var sceneCount = SceneManager.sceneCount;
			for (var i = 0; i < sceneCount; i++) {
				ActiveScenes.Add(new SceneInfo(SceneManager.GetSceneAt(i)));
			}
		}
	}
}