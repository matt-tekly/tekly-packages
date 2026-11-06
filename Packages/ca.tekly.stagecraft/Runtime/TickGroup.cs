using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Profiling;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// What the stage knows about one behaviour type: its tick group and whether it overrides Tick/FixedTick.
	/// Computed once per type per stage.
	/// </summary>
	internal sealed class TypeInfo
	{
		public TickGroup Group;
		public bool Ticks;
		public bool FixedTicks;
	}

	internal static class TickDetection
	{
		private static readonly Dictionary<Type, (bool ticks, bool fixedTicks)> s_cache = new Dictionary<Type, (bool, bool)>();
		private static readonly Type[] s_floatArg = { typeof(float) };

		public static void Detect(Type type, out bool ticks, out bool fixedTicks)
		{
			if (!s_cache.TryGetValue(type, out var result)) {
				result = (Overrides(type, nameof(ActorBehaviour.Tick)), Overrides(type, nameof(ActorBehaviour.FixedTick)));
				s_cache[type] = result;
			}

			ticks = result.ticks;
			fixedTicks = result.fixedTicks;
		}

		private static bool Overrides(Type type, string method)
		{
			var info = type.GetMethod(method, BindingFlags.Public | BindingFlags.Instance, null, s_floatArg, null);
			return info != null && info.DeclaringType != typeof(ActorBehaviour);
		}
	}

	internal abstract class TickGroup
	{
		public abstract Type Type { get; }
		public abstract int RawCount { get; }
		public abstract int TickCount { get; }
		public abstract int FixedTickCount { get; }
		public abstract bool HasSystem { get; }

		public abstract void Add(ActorBehaviour behaviour, TypeInfo info);
		public abstract void Remove(ActorBehaviour behaviour);
		public abstract void Tick(float deltaTime);
		public abstract void FixedTick(float fixedDeltaTime);
	}

	internal sealed class TickGroup<T> : TickGroup where T : ActorBehaviour
	{
		public override Type Type => typeof(T);
		public override int RawCount => m_all.Count;
		public override int TickCount => m_tick.Count;
		public override int FixedTickCount => m_fixed.Count;
		public override bool HasSystem => m_system != null;

		public ActorList<T> All => new ActorList<T>(m_all);
		public ActorSystem<T> System => m_system;

		private readonly ActorCollection<T> m_all = new ActorCollection<T>(ListSlot.All);
		private readonly ActorCollection<T> m_tick = new ActorCollection<T>(ListSlot.Tick);
		private readonly ActorCollection<T> m_fixed = new ActorCollection<T>(ListSlot.Fixed);
		private readonly ActorSystem<T> m_system;

		private readonly ProfilerMarker m_tickMarker;
		private readonly ProfilerMarker m_fixedMarker;

		public TickGroup(ActorSystem<T> system)
		{
			m_system = system;
			m_tickMarker = new ProfilerMarker($"Stage.Tick {typeof(T).Name}");
			m_fixedMarker = new ProfilerMarker($"Stage.FixedTick {typeof(T).Name}");
		}

		public override void Add(ActorBehaviour behaviour, TypeInfo info)
		{
			var typed = (T) behaviour;
			m_all.Add(typed);

			if (info.Ticks) {
				m_tick.Add(typed);
			}

			if (info.FixedTicks) {
				m_fixed.Add(typed);
			}

			m_system?.OnAdded(typed);
		}

		public override void Remove(ActorBehaviour behaviour)
		{
			var typed = (T) behaviour;
			m_system?.OnRemoved(typed);

			m_all.Remove(typed);
			m_tick.Remove(typed);
			m_fixed.Remove(typed);
		}

		public override void Tick(float deltaTime)
		{
			using var marker = m_tickMarker.Auto();

			var items = m_tick.Items;
			var count = m_tick.Count;

			for (var i = 0; i < count; i++) {
				var behaviour = items[i];
				if (behaviour.Actor.State == ActorState.Active) {
					behaviour.Tick(deltaTime);
				}
			}

			m_system?.Tick(All, deltaTime);
		}

		public override void FixedTick(float fixedDeltaTime)
		{
			using var marker = m_fixedMarker.Auto();

			var items = m_fixed.Items;
			var count = m_fixed.Count;

			for (var i = 0; i < count; i++) {
				var behaviour = items[i];
				if (behaviour.Actor.State == ActorState.Active) {
					behaviour.FixedTick(fixedDeltaTime);
				}
			}

			m_system?.FixedTick(All, fixedDeltaTime);
		}
	}
}
