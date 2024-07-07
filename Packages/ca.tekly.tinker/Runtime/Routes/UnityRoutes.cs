using System.Net;
using System.Text;
using Tekly.Tinker.Core;
using Tekly.Tinker.Routing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tekly.Tinker.Routes
{
	[Route("/unity"), Description("Unity Commands")]
	public class UnityRoutes
	{
		[Post("/framerate"), Description("Set Target Framerate"), Command("app.framerate") ]
		public int SetTargetFrameRate(int framerate)
		{
			Application.targetFrameRate = framerate;
			return framerate;
		}

		[Post("/quit"), Description("Quit", "Quit the application"), Command("app.quit")]
		public void Quit(int status = 0)
		{
			Application.Quit(status);
		}
		
		[Post("/load_scene"), Description("Load Scene", "Loads the given scene"), Command("app.scene.load")]
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
		
		[Page("/assets/card", "tinker_stats_card", "Stats"), Command("app.assets")]
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
		
		[Page("/info/app", "tinker_data_card", "Data"), Command("app.info")]
		public DataList AppInfo()
		{
			return new DataList("App Info")
				.Add("Version", Application.version)
				.Add("Identifier", Application.identifier)
				.Add("Persistent Data Path", Application.persistentDataPath)
				.Add("System Language", Application.systemLanguage.ToString())
				.Add("Unity Version", Application.unityVersion)
				.Add("Frame", Time.frameCount)
				.Add("Screen Size", $"{Screen.width}x{Screen.height}");
		}
		
		[Get("/screenshot"), Command("app.screenshot")]
		public void Screenshot(HttpListenerResponse response)
		{
			var texture = ScreenCapture.CaptureScreenshotAsTexture();
			response.ContentType = "image/png";
			response.ContentEncoding = Encoding.Default;
			
			response.WriteContent(texture.EncodeToPNG());
			Object.Destroy(texture);
		}
	}
}