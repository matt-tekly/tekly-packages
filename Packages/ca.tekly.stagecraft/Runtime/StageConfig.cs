using System;
using System.Collections.Generic;
using Tekly.Injectors;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// The one place tick order is set. Each registration is a tick group; groups tick in registration order.
	/// A behaviour type belongs to its own registration or, failing that, its nearest registered base type's.
	/// </summary>
	public sealed class StageConfig
	{
		internal readonly List<GroupRegistration> Groups = new List<GroupRegistration>();

		/// <summary>Registers T for ordering, per-object ticks (if it overrides Tick/FixedTick) and All&lt;T&gt;().</summary>
		public StageConfig Register<T>() where T : ActorBehaviour
		{
			return Add(new GroupRegistration<T>(null));
		}

		/// <summary>Registers T with a system created through the stage's injector when the stage is built.</summary>
		public StageConfig RegisterSystem<T, TSystem>() where T : ActorBehaviour where TSystem : ActorSystem<T>, new()
		{
			return Add(new GroupRegistration<T>(container => {
				var system = new TSystem();
				container?.Inject(system);
				return system;
			}));
		}

		/// <summary>Registers T with an already built system. It is still injected if the stage has a container.</summary>
		public StageConfig RegisterSystem<T>(ActorSystem<T> system) where T : ActorBehaviour
		{
			if (system == null) {
				throw new ArgumentNullException(nameof(system));
			}

			return Add(new GroupRegistration<T>(container => {
				container?.Inject(system);
				return system;
			}));
		}

		private StageConfig Add(GroupRegistration registration)
		{
			for (var i = 0; i < Groups.Count; i++) {
				if (Groups[i].Type == registration.Type) {
					throw new ArgumentException($"{registration.Type.Name} is registered twice");
				}
			}

			Groups.Add(registration);
			return this;
		}
	}

	internal abstract class GroupRegistration
	{
		public abstract Type Type { get; }
		public abstract TickGroup Create(Stage stage);
	}

	internal sealed class GroupRegistration<T> : GroupRegistration where T : ActorBehaviour
	{
		public override Type Type => typeof(T);

		private readonly Func<InjectorContainer, ActorSystem<T>> m_systemFactory;

		public GroupRegistration(Func<InjectorContainer, ActorSystem<T>> systemFactory)
		{
			m_systemFactory = systemFactory;
		}

		public override TickGroup Create(Stage stage)
		{
			var system = m_systemFactory?.Invoke(stage.Container);
			if (system != null) {
				system.Stage = stage;
			}

			return new TickGroup<T>(system);
		}
	}
}
