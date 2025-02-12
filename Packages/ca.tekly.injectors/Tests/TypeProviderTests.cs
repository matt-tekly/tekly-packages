using System;
using NUnit.Framework;
using UnityEngine.Scripting;

namespace Tekly.Injectors.Tests
{
	public class TypeProviderTests
	{
		internal class BaseTypeInstanceProvider : ITypeInstanceProvider
		{
			private readonly Type m_type = typeof(Base<>);
			
			public bool CanProvide(Type type)
			{
				var canProvide = false;
				
				if (type.IsGenericType) {
					canProvide = type.GetGenericTypeDefinition() == m_type;
				}
				
				return canProvide;
			}

			public IInstanceProvider Provide(Type type)
			{
				var genericArgument = type.GetGenericArguments()[0];
				var typeParams = new[] { genericArgument };
				
				var genericType = m_type.MakeGenericType(typeParams);
				return InstanceProvider.Create(Activator.CreateInstance(genericType));
			}
		}
		
		internal class Base<T>
		{
			public T Value { get; set; }
		}

		[Preserve]
		internal class InjectionClass
		{
			[Inject] public Base<int> Ints;
			[Inject] public Base<string> Strings;
			
			[Preserve]
			public InjectionClass() {}
		}

		[Test]
		public void TypeProviderTest()
		{
			var container = new InjectorContainer();
			container.RegisterTypeProvider(new BaseTypeInstanceProvider());
			container.Factory<InjectionClass>();

			var instance = container.Get<InjectionClass>();

			Assert.That(instance, Is.Not.Null);
			Assert.That(instance.Ints, Is.Not.Null);
			Assert.That(instance.Strings, Is.Not.Null);
		}
	}
}