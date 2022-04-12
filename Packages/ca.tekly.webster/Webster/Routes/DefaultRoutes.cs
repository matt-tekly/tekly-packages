//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if (WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR)

using Tekly.Webster.Routes.AssetInfos;
using Tekly.Webster.Routes.GameObjects;
using Tekly.Webster.Routing;
using Tekly.Webster.Utility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.Webster.Routes
{
	[Route("/api")]
	public class DefaultRoutes
	{
		[Get("/routes")]
		[Description("Get all the routes")]
		[RequestMainThread(false)]
		public RoutesInfo GetRoutes()
		{
			return WebsterServer.Instance.GetRoutes();
		}

		[Get("/info")]
		[Description("Get general info about the device")]
		public InfoSummary GetInfo()
		{
			return InfoSummary.Get();
		}

		[Get("/hierarchy")]
		[Description("Get the GameObject hierarchy")]
		public HierarchySummary GetHierarchy()
		{
			return new HierarchySummary();
		}

		[Get("/gameobject")]
		[Description("Find a GameObject by path")]
		public GameObjectQuery GetGameObject(string path = "Main Camera")
		{
			return new GameObjectQuery(path);
		}

		[Put("/gameobject/setactive")]
		[Description("Set GameObject active")]
		public HierarchySummary SetGameObjectActive(int instanceId, bool active = true)
		{
			var gameObject = UnityApi.FindGameObjectByInstanceId(instanceId);
			if (gameObject != null) {
				gameObject.SetActive(active);
			}

			return new HierarchySummary();
		}

		[Put("/component/setenabled")]
		[Description("Set Component enabled")]
		public HierarchySummary SetComponentActive(int instanceId, bool enabled = true)
		{
			var component = UnityApi.FindComponent(instanceId);
			if (component != null) {
				UnityApi.SetComponentEnabled(component, enabled);
			}

			return new HierarchySummary();
		}

		[Put("/quit")]
		[Description("Quit the game")]
		public void Quit()
		{
			Application.Quit();
		}

		[Get("/scenes/info")]
		[Description("Get the active scenes")]
		public ScenesInfo GetScenes()
		{
			return new ScenesInfo();
		}

		[Put("/scenes/load")]
		[Description("Load a scene")]
		public void LoadScene(string scene = "SceneName", LoadSceneMode sceneMode = LoadSceneMode.Additive)
		{
			SceneManager.LoadScene(scene, sceneMode);
		}

		[Get("/assets")]
		[Hidden]
		public AssetsSummary GetAssetsSummary()
		{
			return new AssetsSummary();
		}

		[Get("/time/scale")]
		[Description("Get the Time.timeScale")]
		public float GetTimeScale()
		{
			return Time.timeScale;
		}

		[Put("/time/scale")]
		[Description("Set the Time.timeScale")]
		public void SetTimeScale(float scale = 1)
		{
			Time.timeScale = scale;
		}

		[Get("/time/maximumDeltaTime")]
		[Description("Get the Time.maximumDeltaTime")]
		public float GetMaximumDeltaTime()
		{
			return Time.maximumDeltaTime;
		}

		[Put("/time/maximumDeltaTime")]
		[Description("Set the Time.maximumDeltaTime")]
		public void SetMaximumDeltaTime(float maximumDeltaTime = 0.1f)
		{
			Time.maximumDeltaTime = maximumDeltaTime;
		}
	}
}
#endif