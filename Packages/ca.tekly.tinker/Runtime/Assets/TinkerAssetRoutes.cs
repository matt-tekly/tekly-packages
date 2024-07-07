using System;
using System.Collections.Generic;
using System.Net;
using DotLiquid;
using DotLiquid.FileSystems;
using Tekly.Tinker.Http;
using Tekly.Tinker.Routing;
using UnityEngine;

namespace Tekly.Tinker.Assets
{
	public class TinkerAssetRoutes : IFileSystem, ITinkerRoutes
	{
		private bool m_initialized;
		private float m_lastRefreshTime;
		private readonly List<TinkerAssets> m_tinkerAssets = new List<TinkerAssets>();
		
		public void AddAssets(TinkerAssets tinkerAssets)
		{
			m_tinkerAssets.Add(tinkerAssets);
		}

		public string ReadTemplateFile(Context context, string templateName)
		{
			return ReadTemplateFile(templateName);
		}
		
		public string ReadTemplateFile(string templateName)
		{
			RefreshAssetDatabase();
			Initialize();

#if TINKER_ENABLED
			foreach (var tinkerAssets in m_tinkerAssets) {
				foreach (var tinkerAsset in tinkerAssets.Assets) {
					if (tinkerAsset.IsTemplate && tinkerAsset.Name == templateName) {
						var textAsset = tinkerAsset.Asset as TextAsset;
						return textAsset.text;
					}
				}
			}
#endif
			return $"[Template [{templateName}] - Not Found]";
		}

		public bool TryHandle(string route, HttpListenerRequest request, HttpListenerResponse response)
		{
			Initialize();

			foreach (var tinkerAssets in m_tinkerAssets) {
				foreach (var tinkerAsset in tinkerAssets.Assets) {
					if (!string.Equals(tinkerAsset.Url, route, StringComparison.OrdinalIgnoreCase)) {
						continue;
					}

					if (TryHandleAsset(response, tinkerAsset)) {
						return true;
					}
				}
			}

			return false;
		}

		public void GetAssets(string extension, List<TinkerAsset> assets)
		{
			foreach (var tinkerAssets in m_tinkerAssets) {
				foreach (var tinkerAsset in tinkerAssets.Assets) {
					if (tinkerAsset.Url.EndsWith(extension)) {
						assets.Add(tinkerAsset);
					}
				}
			}
		}

		private bool TryHandleAsset(HttpListenerResponse response, TinkerAsset tinkerAsset)
		{
			RefreshAssetDatabase();
			
			if (tinkerAsset.Asset is Texture texture) {
				response.WritePng(texture);
			}

			if (tinkerAsset.Asset is TextAsset textAsset) {
				if (tinkerAsset.Url.EndsWith("css")) {
					var template = Template.Parse(textAsset.text);
					response.WriteCss(template.Render());
					return true;
				}

				if (tinkerAsset.Url.EndsWith("html")) {
					var template = Template.Parse(textAsset.text);
					response.WriteHtml(template.Render());
					return true;
				}

				if (tinkerAsset.Url.EndsWith("js")) {
					var template = Template.Parse(textAsset.text);
					response.WriteJavascript(template.Render());
					return true;
				}
			}
			
			return false;
		}

		private void Initialize()
		{
			if (m_initialized) {
				return;
			}

			var assets = Resources.LoadAll<TinkerAssets>("");
			foreach (var asset in assets) {
				AddAssets(asset);	
			}

			m_initialized = true;
		}

		private void RefreshAssetDatabase()
		{
			if ((Time.realtimeSinceStartup - m_lastRefreshTime) > 1f) {
				return;
			}

			m_lastRefreshTime = Time.realtimeSinceStartup;
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh();
#endif
		}
	}
}