using System.Collections.Generic;
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

		[Page("/tinker/info/app", "tinker_data_list", "Data")]
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
		
		[Page("/tinker/info/app", "tinker_data_list", "Data")]
		public DataList UnityStuff()
		{
			return new DataList("App Info")
				.Add("Version", Application.version)
				.Add("Company Name", Application.companyName)
				.Add("Identifier", Application.identifier)
				.Add("Platform", Application.platform.ToString())
				.Add("Persistent Data Path", Application.persistentDataPath)
				.Add("Product Name", Application.productName)
				.Add("System Language", Application.systemLanguage.ToString())
				.Add("Unity Version", Application.unityVersion)
				.Add("Frame", Time.frameCount);
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

	public class DataList
	{
		public string Name;
		public List<DataItem> Items = new List<DataItem>();

		public DataList(string name)
		{
			Name = name;
		}
		
		public DataList Add(string name, string value, string color = "grey")
		{
			Items.Add(new DataItem(name, value, color));
			return this;
		}

		public DataList Add(string name, float value, string color = "grey")
		{
			Items.Add(new DataItem(name, value, color));
			return this;
		}
		
		public DataList Add(string name, double value, string color = "grey")
		{
			Items.Add(new DataItem(name, value, color));
			return this;
		}

		public DataList Add(string name, int value, string color = "grey")
		{
			Items.Add(new DataItem(name, value, color));
			return this;
		}

		public DataList Add(string name, bool value, string color = "grey")
		{
			Items.Add(new DataItem(name, value, color));
			return this;
		}
		
	}

	public class DataItem
	{
		public string Name;
		public string Value;
		public string Color = "grey";

		public DataItem(string name, string value, string color)
		{
			Name = name;
			Value = value;
			Color = color;
		}

		public DataItem(string name, float value, string color)
		{
			Name = name;
			Value = value.ToString("0.00");
			Color = color;
		}
		
		public DataItem(string name, double value, string color)
		{
			Name = name;
			Value = value.ToString("0.00");
			Color = color;
		}

		public DataItem(string name, int value, string color)
		{
			Name = name;
			Value = value.ToString();
			Color = color;
		}

		public DataItem(string name, bool value, string color)
		{
			Name = name;
			Value = value.ToString();
			Color = color;
		}
	}
}