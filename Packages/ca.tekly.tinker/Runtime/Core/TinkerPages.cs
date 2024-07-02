using System.Net;
using System.Text;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	[Route("")]
	public class TinkerPages
	{
		[Page("", "tinker_home", "page")]
		public void Home() { }
		
		[Page("/", "tinker_home", "page")]
		public void Home2() { }

		[Page("/tinker/routes", "tinker_routes")]
		public void Routes() { }
		
		[Page("/tinker/terminal", "terminal")]
		public void Terminal() { }

		[Page("/tinker/info/app", "tinker_data_card", "Data")]
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
		
		[Get("/screenshot")]
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