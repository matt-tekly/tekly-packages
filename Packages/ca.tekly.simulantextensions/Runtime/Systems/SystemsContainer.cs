using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Tekly.Injectors;
using Tekly.Simulant.Systems;

namespace Tekly.Simulant.Extensions.Systems
{
	internal class RegisteredSystem
	{
		public Type Type;
		public ISystem System;
		public bool NeedsInjection;
	}

	[UsedImplicitly]
	public class SystemsContainer : IDisposable
	{
		private readonly InjectorContainer m_container;

		private readonly List<RegisteredSystem> m_registeredSystems = new List<RegisteredSystem>();

		private readonly List<ISystem> m_systems = new List<ISystem>();
		private readonly List<ITickSystem> m_tickSystems = new List<ITickSystem>();
		private readonly List<IDisposable> m_disposables = new List<IDisposable>();

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
		
		public SystemsContainer Delete<T>() where T : struct
		{
			var pendingSystem = new RegisteredSystem {
				Type = typeof(DeleteComponentSystem<T>)
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