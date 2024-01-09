using System;
using System.IO;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;
using Tekly.Common.Utils.PropertyBags;
using Tekly.Webster.Routing;
using UnityEngine;

namespace TeklySample.App
{
	public static class AppProperties
	{
		public static PropertyBag Instance { get; private set; }
		private const string FILE = "app_properties.json";

		private static readonly string s_filePath;
		
		static AppProperties()
		{
			s_filePath = LocalFile.GetPath(FILE);
			UnityRuntimeEditorUtils.OnEnterPlayMode(Init);
			UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
		}

		private static void Init()
		{
			if (File.Exists(s_filePath)) {
				var json = File.ReadAllText(s_filePath);
				
				try {
					Instance = JsonUtility.FromJson<PropertyBag>(json);
				} catch (Exception e) {
					Debug.LogException(e);
				}
			}

			if (Instance == null) {
				Instance = new PropertyBag();
			}

			Instance.Modified.Subscribe(_ => {
				var json = JsonUtility.ToJson(Instance);
				File.WriteAllText(s_filePath, json);
			});
		}

		private static void Reset()
		{
			Instance = null;
		}
	}
	

	[Route("/app/properties")]
	public class AppPropertiesRoute
	{
		[Get("/all")]
		public PropertyBag GetAll()
		{
			return AppProperties.Instance;
		}
		
		[Post("/number")]
		public PropertyBag Set(string id, double value)
		{
			AppProperties.Instance.SetValue(id, value);
			return AppProperties.Instance;
		}
	}
}