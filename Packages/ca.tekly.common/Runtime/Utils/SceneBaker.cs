using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.Common.Utils
{
	public interface ISceneBaker
	{
		public void Bake(SceneBake sceneBake);
	}
	
	public class SceneBake
	{
		public readonly Scene Scene;
		private readonly GameObject[] m_gameObjects;

		public SceneBake(Scene scene)
		{
			Scene = scene;
			m_gameObjects = Scene.GetRootGameObjects();
		}
		
		public List<T> GetAll<T>(bool includeInactive = false)
		{
			var results = new List<T>();
			var scratch = new List<T>();

			foreach (var gameObject in m_gameObjects) {
				if (!gameObject.activeInHierarchy && !includeInactive) {
					continue;
				}
				
				gameObject.GetComponentsInChildren(scratch);
				results.AddRange(scratch);
			}

			return results;
		}
		
		public void GetAll<T>(ref T[] objects, bool includeInactive = false)
		{
			objects = GetAll<T>(includeInactive).ToArray();
		}
		
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoad]
		public static class SceneBakerProcessor
		{
			static SceneBakerProcessor()
			{
				UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += ProcessSceneBakers;
			}

			private static void ProcessSceneBakers(Scene scene, string path)
			{
				var sceneBake = new SceneBake(scene);
				var sceneBakers = sceneBake.GetAll<ISceneBaker>();

				foreach (var sceneBaker in sceneBakers) {
					sceneBaker.Bake(sceneBake);
				}
			}
		}
#endif
	}
}