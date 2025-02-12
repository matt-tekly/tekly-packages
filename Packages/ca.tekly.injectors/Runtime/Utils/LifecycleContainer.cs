using System;
using System.Collections.Generic;
using Tekly.Common.Utils;

namespace Tekly.Injectors.Utils
{
	public interface ILifecycleInjectorProvider
	{
		void Provide(LifecycleContainer lifecycle);
	}
	
	public class LifecycleContainer
	{
		public readonly InjectorContainer Container;

		private readonly List<ITickable> m_tickables = new List<ITickable>();
		private readonly List<IDisposable> m_disposables = new List<IDisposable>();

		private readonly List<Type> m_targets = new List<Type>();

		public LifecycleContainer(InjectorContainer container)
		{
			Container = container;
			Container.Register(this);
		}

		public void Singleton<T>()
		{
			Container.Singleton<T>();
			m_targets.Add(typeof(T));
		}

		public void Singleton<TInterface, TImpl>() where TImpl : TInterface
		{
			Container.Singleton<TInterface, TImpl>();
			m_targets.Add(typeof(TInterface));
		}

		public void Factory<T>()
		{
			Container.Factory<T>();
		}
		
		public void Register<T>(T instance)
		{
			Container.Register(instance);
			m_targets.Add(typeof(T));
		}

		public void Register<TInterface, TImpl>(TInterface instance) where TImpl : TInterface
		{
			Container.Register<TInterface, TImpl>(instance);
			m_targets.Add(typeof(TInterface));
		}
		
		public void Initialize()
		{
			foreach (var target in m_targets) {
				var instance = Container.Get(target);

				if (instance is IDisposable disposable) {
					m_disposables.Add(disposable);
				}

				if (instance is ITickable tickable) {
					m_tickables.Add(tickable);
				}
			}
		}

		public void Dispose()
		{
			Clear();
		}

		public void Tick()
		{
			foreach (var tickable in m_tickables) {
				tickable.Tick();
			}
		}

		public void Clear()
		{
			foreach (var disposable in m_disposables) {
				disposable.Dispose();
			}

			m_disposables.Clear();
			m_tickables.Clear();
		}
	}
}