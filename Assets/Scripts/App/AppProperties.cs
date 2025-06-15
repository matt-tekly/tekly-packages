using System;
using System.IO;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;
using Tekly.Common.Utils.PropertyBags;
using Tekly.Webster.Routing;
using UnityEngine;

namespace TeklySample.App
{
	public class AppProperties : SingletonFactory<PropertyBag, AppProperties>, IDisposable
	{
		private const string FILE = "app_properties.json";
		private IDisposable m_disposable;
		
		protected override PropertyBag Create()
		{
			PropertyBag propertyBag = null;
			var filePath = LocalFile.GetPath(FILE);
			
			if (File.Exists(filePath)) {
				var json = File.ReadAllText(filePath);
				propertyBag = JsonUtility.FromJson<PropertyBag>(json);
			}

			if (propertyBag == null) {
				propertyBag = new PropertyBag();
			}
			
			m_disposable = propertyBag.Modified.Subscribe(_ => {
				var json = JsonUtility.ToJson(propertyBag);
				File.WriteAllText(filePath, json);
			});

			return propertyBag;
		}

		public void Dispose()
		{
			m_disposable?.Dispose();
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