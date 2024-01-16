using System;
using System.Collections.Generic;
using Tekly.Injectors;

namespace Tekly.Simulant.Extensions.Systems
{
	public interface ISystem { }

	public interface ITickSystem : ISystem
	{
		void Tick(float deltaTime);
	}
	
	internal class RegisteredSystem
	{
		public Type Type;
		public ISystem System;
		public bool NeedsInjection;
	}
	
	public class SystemsContainer : IDisposable
	{
		private readonly InjectorContainer m_container;

		private List<RegisteredSystem> m_registeredSystems = new List<RegisteredSystem>();

		private List<ISystem> m_systems = new List<ISystem>();
		private List<ITickSystem> m_tickSystems = new List<ITickSystem>();
		private List<IDisposable> m_disposables = new List<IDisposable>();
		
		public SystemsContainer(InjectorContainer container)
		{
			m_container = container;
		}

		public void Init()
		{
			foreach (var registeredSystem in m_registeredSystems) {
				if (registeredSystem.Type != null) {
					var instance = (ISystem)m_container.Get(registeredSystem.Type);
					m_systems.Add(instance);
				} else {
					if (registeredSystem.NeedsInjection) {
						m_container.Inject(registeredSystem.System);
					}
					
					m_systems.Add(registeredSystem.System);
				}
			}
			
			Filter(m_systems, m_tickSystems);
			Filter(m_systems, m_disposables);
		}
		
		public void Tick(float deltaTime)
		{
			foreach (var tickSystem in m_tickSystems) {
				tickSystem.Tick(deltaTime);
			}
		}

		public SystemsContainer Add<T>() where T : ISystem
		{
			var pendingSystem = new RegisteredSystem {
				Type = typeof(T)
			};
			m_registeredSystems.Add(pendingSystem);
			return this;
		}
		
		private static void Filter<T>(List<ISystem> systems, List<T> results)
		{
			for (int index = 0, count = systems.Count; index < count; index++) {
				var system = systems[index];
				if (system is T tSystem) {
					results.Add(tSystem);
				}
			}
		}

		public void Dispose()
		{
			foreach (var disposable in m_disposables) {
				disposable.Dispose();
			}
		}
	}
}