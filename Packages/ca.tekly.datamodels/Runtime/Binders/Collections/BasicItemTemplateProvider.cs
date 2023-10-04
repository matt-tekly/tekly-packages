using System;
using Tekly.DataModels.Models;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.DataModels.Binders.Collections
{
	[Serializable]
	public struct NamedTemplate
	{
		public string Id;
		public BinderContainer Value;
	}
	
	public class BasicItemTemplateProvider : ItemTemplateProvider
	{
		[SerializeField] private ModelRef m_templateKey;
		[SerializeField] private NamedTemplate[] m_templates;
		
		public override BinderContainer Get(ObjectModel model)
		{
			if (model.TryGetModel(m_templateKey.Path, out StringValueModel templateId)) {
				var id = templateId.Value;
				foreach (var template in m_templates) {
					if (template.Id == id) {
						return template.Value;
					}
				}
				
				TkLogger.Get<BasicItemTemplateProvider>().Error("Failed to find template for id [{id}]", ("id", id));
			}

			TkLogger.Get<BasicItemTemplateProvider>().Error("Failed to find model [{path}]", ("path", m_templateKey.Path));
			
			return null;
		}
	}
}