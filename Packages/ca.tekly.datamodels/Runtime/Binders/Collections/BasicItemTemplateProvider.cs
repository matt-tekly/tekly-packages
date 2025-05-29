using System;
using Tekly.Common.Utils;
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
		[SerializeField] private BinderContainer m_default;

		private readonly TkLogger m_logger = TkLogger.Get<BasicItemTemplateProvider>();
		private void Awake()
		{
			for (var index = 0; index < m_templates.Length; index++) {
				ref var template = ref m_templates[index];
				template.Value = PrefabProtector.Protect(template.Value);
			}
		}

		public override BinderContainer Get(ObjectModel model)
		{
			if (model.TryGetModel(m_templateKey.Path, out StringValueModel templateId)) {
				var id = templateId.Value;
				foreach (var template in m_templates) {
					if (template.Id == id) {
						return template.Value;
					}
				}

				if (m_default == null) {
					m_logger.ErrorContext("Failed to find named template for [{id}] and there is no default", this, ("id", id));
				}
				
				return m_default;
			}

			m_logger.Error("Failed to find model [{path}]", ("path", m_templateKey.Path));

			return null;
		}
	}
}