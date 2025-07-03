using System;
using System.Collections.Generic;
using Tekly.DataModels.Binders;
using Tekly.DataModels.Models;
using Tekly.Injectors;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Extensions.DataProviders
{
	public struct ModelWithName
	{
		public string Name;
		public ModelBase Model;
	}
	
	[Serializable]
	public abstract class RootModelDataProvider : IUiDataProvider
	{
		[SerializeField] private ModelRef m_addTo;

		private List<ModelWithName> m_models = new List<ModelWithName>();
		private ObjectModel m_addToModel;
		
		public void Bind()
		{
			if (string.IsNullOrEmpty(m_addTo.Path)) {
				m_addToModel = RootModel.Instance;
			} else {
				var key = ModelKey.Parse(m_addTo.Path);
				if (!RootModel.Instance.TryGetModel(key, 0, out m_addToModel)) {
					TkLogger.Get<RootModelDataProvider>().Error("Failed to find Model [{key}]", ("key", m_addTo.Path));
				}
			}
			
			OnBind();
		}

		protected abstract void OnBind();

		public void Unbind()
		{
			foreach (var modelWithName in m_models) {
				modelWithName.Model.Dispose();
				m_addToModel.RemoveModel(modelWithName.Name);
			}
			
			m_models.Clear();
		}

		protected void AddModel(string name, ModelBase modelBase)
		{
			m_models.Add(new ModelWithName { Name = name, Model = modelBase});
			m_addToModel.Add(name, modelBase);
		}
	}
}