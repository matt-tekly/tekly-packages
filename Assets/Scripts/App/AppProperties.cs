using Tekly.Common.Utils;
using Tekly.Common.Utils.PropertyBags;
using Tekly.Webster.Routing;
using UnityEngine;

namespace TeklySample.App
{
	public static class AppProperties
	{
		public static PropertyBag Instance { get; private set; }
		private const string KEY = "app.properties";
		
		static AppProperties()
		{
			UnityRuntimeEditorUtils.OnEnterPlayMode(Init);
			UnityRuntimeEditorUtils.OnExitPlayMode(Reset);
		}

		private static void Init()
		{
			if (PlayerPrefs.HasKey(KEY)) {
				var json = PlayerPrefs.GetString(KEY);
				Instance = JsonUtility.FromJson<PropertyBag>(json);
			} else {
				Instance = new PropertyBag();
			}

			Instance.Modified.Subscribe(_ => {
				var json = JsonUtility.ToJson(Instance);
				PlayerPrefs.SetString(KEY, json);
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