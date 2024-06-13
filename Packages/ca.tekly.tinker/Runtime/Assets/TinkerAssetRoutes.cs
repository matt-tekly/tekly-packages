using System;
using System.Collections.Generic;
using System.Net;
using DotLiquid;
using DotLiquid.FileSystems;
using Tekly.Tinker.Core;
using Tekly.Tinker.Routing;
using UnityEngine;

namespace Tekly.Tinker.Assets
{
	public class TinkerAssetRoutes : IFileSystem, ITinkerRoutes
	{
		private bool m_initialized;
		
		private readonly Dictionary<string, string> m_constantTemplates = new Dictionary<string, string>();
		private readonly List<TinkerAssets> m_tinkerAssets = new List<TinkerAssets>();

		public void AddTemplate(string name, string content)
		{
			m_constantTemplates[name] = content;
		}
		
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
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh();
#endif
			if (m_constantTemplates.TryGetValue(templateName, out var content)) {
				return content;
			}

			foreach (var tinkerAssets in m_tinkerAssets) {
				foreach (var tinkerAsset in tinkerAssets.Assets) {
					if (tinkerAsset.IsTemplate && tinkerAsset.Name == templateName) {
						var textAsset = tinkerAsset.Asset as TextAsset;
						return textAsset.text;
					}
				}
			}

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

		private static bool TryHandleAsset(HttpListenerResponse response, TinkerAsset tinkerAsset)
		{
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh();
#endif
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
			
		}
	}
}