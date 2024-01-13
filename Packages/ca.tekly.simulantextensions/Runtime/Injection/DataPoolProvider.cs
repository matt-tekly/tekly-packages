using System;
using Tekly.Injectors;
using Tekly.Simulant.Core;

namespace Tekly.Simulant.Extensions.Injection
{
	public class DataPoolProvider : ITypeInstanceProvider
	{
		private readonly World m_world;
		private readonly Type m_type = typeof(DataPool<>);
		
		public DataPoolProvider(World world)
		{
			m_world = world;
		}
		
		public bool CanProvide(Type type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == m_type;
		}

		public IInstanceProvider Provide(Type type)
		{
			var dataType = type.GetGenericArguments()[0];
			return InstanceProvider.Create(type, m_world.GetPool(dataType));
		}
	}
}