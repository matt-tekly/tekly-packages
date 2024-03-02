using System;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;

namespace Tekly.Simulant.Extensions.Injection
{
	public class DeleteDataSystemProvider : ITypeInstanceProvider
	{
		private readonly World m_world;
		private readonly Type m_type = typeof(DeleteDataSystem<>);
		
		public DeleteDataSystemProvider(World world)
		{
			m_world = world;
		}
		
		public bool CanProvide(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == m_type;
		}

		public IInstanceProvider Provide(Type type)
		{
			var instance = Activator.CreateInstance(type, m_world);
			return InstanceProvider.Create(type, instance);
		}
	}
}