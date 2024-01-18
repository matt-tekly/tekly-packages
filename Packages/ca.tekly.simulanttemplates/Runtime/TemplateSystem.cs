using System;
using JetBrains.Annotations;
using Tekly.Balance;
using Tekly.Simulant.Core;
using Tekly.Simulant.Systems;

namespace Tekly.Simulant.Templates
{
	[UsedImplicitly]
	public class TemplateSystem : ISystem, IQueryListener, IDisposable
	{
		private readonly World m_world;
		private readonly BalanceManager m_balanceManager;
		private readonly Query m_query;
		private readonly DataPool<TemplateInstance> m_templates;
		
		public TemplateSystem(World world, BalanceManager balanceManager)
		{
			m_world = world;
			m_balanceManager = balanceManager;
			m_query = world.Query<TemplateInstance>();
			m_templates = world.GetPool<TemplateInstance>();

			foreach (var entity in m_query) {
				ref var templateInstance = ref m_templates.Get(entity);
				
				var template = m_balanceManager.Get<EntityTemplate>(templateInstance.Template);
				template.Populate(m_world, entity);
			}

			m_query.AddListener(this);
		}

		public void EntityAdded(int entity, Query query)
		{
			ref var templateInstance = ref m_templates.Get(entity);
				
			var template = m_balanceManager.Get<EntityTemplate>(templateInstance.Template);
			template.Populate(m_world, entity);
		}

		public void EntityRemoved(int entity, Query query)
		{
			
		}

		public void Dispose()
		{
			m_query.RemoveListener(this);
		}
	}
}