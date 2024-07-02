using Tekly.Tinker.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.Tinker.Routes
{
	[Route("/unity"), Description("Unity Commands")]
	public class UnityRoutes
	{
		[Post("/framerate"), Description("Set Target Framerate")]
		public int SetTargetFrameRate(int framerate)
		{
			Application.targetFrameRate = framerate;
			return framerate;
		}

		[Post("/quit"), Description("Quit", "Quit the application")]
		public void Quit(int status = 0)
		{
			Application.Quit(status);
		}
		
		[Post("/load_scene"), Description("Load Scene", "Loads the given scene")]
		public string LoadScene(string scene, LoadSceneMode mode = LoadSceneMode.Single)
		{
			var sceneAsset = SceneManager.GetSceneByName(scene);
			if (!sceneAsset.IsValid()) {
				return $"Couldn't find scene: [{scene}]";
			}
			SceneManager.LoadScene(scene, mode);

			return $"Loading Scene: [{scene}]";
		}
		
		[Get("/assets"), Description("Assets Summary", "Returns a summary of all assets in memory")]
		public AssetsSummary AssetsSummary()
		{
			return new AssetsSummary();
		}
		
		[Page("/assets/card", "tinker_stats_card", "Stats")]
		public DataList AssetsSummaryCard()
		{
			var summary = new AssetsSummary();
			return new DataList("Assets")
				.Add("Audio Clips", summary.AudioClips.Length)
				.Add("Materials", summary.Materials.Length)
				.Add("Meshes", summary.Meshes.Length)
				.Add("Shaders", summary.Shaders.Length)
				.Add("Textures", summary.Textures.Length)
				.Add("Sprites", summary.Sprites.Length);
		}
	}
}