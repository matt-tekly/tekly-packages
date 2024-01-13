using System;

namespace Tekly.Injectors
{
	/// <summary>
	/// Provides an instance of the BaseType
	/// </summary>
	public interface IInstanceProvider
	{
		object Instance { get; }
		Type BaseType { get; }
	}

	/// <summary>
	/// Provides instances for a type of there wasn't an InstanceProvider registered for that type.
	/// </summary>
	public interface ITypeInstanceProvider
	{
		bool CanProvide(Type type);
		IInstanceProvider Provide(Type type);
	}

	public class InstanceProvider : IInstanceProvider
	{
		public Type BaseType { get; private set; }
		public object Instance { get; private set; }

		public static IInstanceProvider Create<T>(T instance)
		{
			return Create(typeof(T), instance);
		}

		public static IInstanceProvider Create(Type type, object instance)
		{
			return new InstanceProvider {
				BaseType = type,
				Instance = instance
			};
		}
	}

	public class SingletonProvider : IInstanceProvider
	{
		private readonly InjectorContainer m_container;
		private object m_instance;

		public Type BaseType { get; }
		public object Instance => GetInstance();

		private SingletonProvider(Type type, InjectorContainer container)
		{
			BaseType = type;
			m_container = container;
		}

		private object GetInstance()
		{
			if (m_instance == null) {
				var injector = TypeDatabase.Instance.GetInjector(BaseType);
				m_instance = injector.Instantiate(m_container);
			}

			return m_instance;
		}

		public static IInstanceProvider Create<T>(InjectorContainer container)
		{
			return new SingletonProvider(typeof(T), container);
		}
	}

	public class FactoryProvider : IInstanceProvider
	{
		private readonly InjectorContainer m_container;
		private object m_instance;

		public Type BaseType { get; }
		public object Instance => GetInstance();

		private FactoryProvider(Type type, InjectorContainer container)
		{
			BaseType = type;
			m_container = container;
		}

		private object GetInstance()
		{
			var injector = TypeDatabase.Instance.GetInjector(BaseType);
			return injector.Instantiate(m_container);
		}

		public static IInstanceProvider Create<T>(InjectorContainer container)
		{
			return new FactoryProvider(typeof(T), container);
		}
	}
}