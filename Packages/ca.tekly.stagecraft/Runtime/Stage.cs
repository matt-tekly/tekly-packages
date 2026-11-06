using System;
using System.Collections.Generic;
using Tekly.Common.LifeCycles;
using Tekly.Common.Observables;
using Tekly.Injectors;
using Tekly.Injectors.Utils;
using Tekly.Logging;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Scripting;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// Owns every actor: pools, ids, lifecycle, ticking and teardown. Driven from outside through
	/// <see cref="Tick"/> and <see cref="FixedTick"/> (see <see cref="StageRunner"/> for the default driver).
	/// Changes to the set of actors are applied only at sync points, which sit at the start and end of each tick.
	/// </summary>
	public sealed class Stage : IDisposable
	{
		public const int MAX_SYNC_PASSES = 16;

		public string Name { get; }
		public StageTime Time { get; } = new StageTime();
		public InjectorContainer Container { get; }
		public PrefabRegistry Prefabs { get; } = new PrefabRegistry();

		/// <summary>The physics backend, if any. Stepped and drained inside FixedTick.</summary>
		public IStagePhysics Physics { get; set; }

		/// <summary>Number of Active actors.</summary>
		public int AliveCount { get; private set; }

		public bool IsDisposed { get; private set; }

		/// <summary>True between the opening and closing sync of a Tick or FixedTick.</summary>
		public bool IsTicking => m_ticking;

		/// <summary>Parent of the pools, and of every pooled instance.</summary>
		public Transform Root => m_root;

		/// <summary>Fires at the end of Spawn/Activate. The actor is alive.</summary>
		public ITriggerable<Actor> Spawned => m_spawned;

		/// <summary>Fires at the sync point, after OnDespawned (or after an external destroy). The actor is not alive but still has its id.</summary>
		public ITriggerable<Actor> Despawned => m_despawned;

		private readonly TkLogger m_logger = TkLogger.Get<Stage>();
		private readonly Transform m_root;
		private readonly ActorRegistry m_registry = new ActorRegistry(256);
		private readonly ActorPools m_pools;
		private readonly Triggerable<Actor> m_spawned = new Triggerable<Actor>();
		private readonly Triggerable<Actor> m_despawned = new Triggerable<Actor>();

		private readonly List<TickGroup> m_groups = new List<TickGroup>();
		private readonly Dictionary<Type, TickGroup> m_groupsByType = new Dictionary<Type, TickGroup>();
		private readonly Dictionary<Type, TypeInfo> m_typeInfos = new Dictionary<Type, TypeInfo>();
		private TickGroup<ActorBehaviour> m_fallbackGroup;

		private List<Actor> m_pendingAdd = new List<Actor>();
		private List<Actor> m_pendingAddScratch = new List<Actor>();
		private List<Actor> m_pendingRemove = new List<Actor>();
		private List<Actor> m_pendingRemoveScratch = new List<Actor>();

		private bool m_ticking;
		private bool m_fixedBegun;
		private bool m_clearRequested;
		private bool m_disposing;
		private bool m_loggedPooledDespawn;

		private static readonly ProfilerMarker s_syncMarker = new ProfilerMarker("Stage.Sync");
		private static readonly ProfilerMarker s_physicsStepMarker = new ProfilerMarker("Stage.Physics.Step");
		private static readonly ProfilerMarker s_physicsDeliverMarker = new ProfilerMarker("Stage.Physics.Deliver");

		[Preserve]
		public Stage(StageConfig config, InjectorContainer container = null, string name = "Stage")
		{
			Name = name;
			Container = container;

			m_root = new GameObject($"[Stage] {name}").transform;
			m_pools = new ActorPools(this, m_root);

			foreach (var registration in config.Groups) {
				var group = registration.Create(this);
				m_groups.Add(group);
				m_groupsByType.Add(group.Type, group);
			}
		}

		// ----- Spawning -------------------------------------------------------------------------------------------

		public Actor Spawn(Actor prefab)
		{
			return Spawn(prefab, Pose.identity);
		}

		public Actor Spawn(Actor prefab, in Pose pose)
		{
			var actor = Acquire(prefab);
			Activate(actor, pose);
			return actor;
		}

		public T Spawn<T>(T prefab) where T : ActorBehaviour
		{
			return Spawn(prefab, Pose.identity);
		}

		public T Spawn<T>(T prefab, in Pose pose) where T : ActorBehaviour
		{
			var instance = Acquire(prefab);
			Activate(instance.Actor, pose);
			return instance;
		}

		/// <summary>Spawns with per-spawn data, delivered to T.Initialize before any OnSpawned.</summary>
		public T Spawn<T, TArgs>(T prefab, in Pose pose, in TArgs args) where T : ActorBehaviour, ISpawnArgs<TArgs> where TArgs : struct
		{
			var instance = Acquire(prefab);
			Activate(instance, pose, args);
			return instance;
		}

		public Actor Spawn(ActorPrefabId prefabId, in Pose pose)
		{
			return Spawn(ResolvePrefab(prefabId), pose);
		}

		/// <summary>Spawns by prefab id with args, delivered to the first behaviour that accepts TArgs.</summary>
		public Actor Spawn<TArgs>(ActorPrefabId prefabId, in Pose pose, in TArgs args) where TArgs : struct
		{
			var actor = Acquire(ResolvePrefab(prefabId));

			if (!actor.TryGet<ISpawnArgs<TArgs>>(out var target)) {
				m_logger.Error("Prefab [{prefab}] has no behaviour taking [{args}]", ("prefab", actor.name), ("args", typeof(TArgs).Name));
				Activate(actor, pose);
				return actor;
			}

			ActivateCore(actor, pose, target, args);
			return actor;
		}

		/// <summary>
		/// Pooled → Acquired. The instance is inactive, injected and not ticked; nothing has been called on it.
		/// For creators that activate later (netcode). Despawning an Acquired actor returns it without callbacks.
		/// </summary>
		public Actor Acquire(Actor prefab)
		{
			ThrowIfDisposed();

			if (prefab == null) {
				throw new ArgumentNullException(nameof(prefab));
			}

			return m_pools.Get(prefab).Take();
		}

		public T Acquire<T>(T prefab) where T : ActorBehaviour
		{
			if (prefab == null) {
				throw new ArgumentNullException(nameof(prefab));
			}

			var prefabActor = prefab.Actor;
			if (ReferenceEquals(prefabActor, null)) {
				prefabActor = prefab.GetComponentInParent<Actor>(true);
				if (prefabActor == null) {
					throw new ArgumentException($"{prefab.name} has no Actor", nameof(prefab));
				}

				prefabActor.EnsureInitialized();
			}

			var index = Array.IndexOf(prefabActor.Behaviours, prefab);
			if (index < 0) {
				throw new ArgumentException($"{prefab.name} is not a behaviour of actor {prefabActor.name} (nested actor?)", nameof(prefab));
			}

			var instance = Acquire(prefabActor);
			return (T) instance.Behaviours[index];
		}

		/// <summary>
		/// Acquired → Active: applies the pose, allocates the id, activates the object, runs OnSpawned.
		/// <paramref name="reason"/> is visible as Actor.SpawnReason; pass Restored when the instance's state was
		/// set up between Acquire and Activate (a save game, a replicated spawn) so behaviours can skip spawn-only work.
		/// </summary>
		public void Activate(Actor actor, in Pose pose, SpawnReason reason = SpawnReason.Spawned)
		{
			ActivateCore<NoArgs>(actor, pose, null, default, reason);
		}

		public void Activate<T, TArgs>(T behaviour, in Pose pose, in TArgs args, SpawnReason reason = SpawnReason.Spawned) where T : ActorBehaviour, ISpawnArgs<TArgs> where TArgs : struct
		{
			ActivateCore(behaviour.Actor, pose, behaviour, args, reason);
		}

		private void ActivateCore<TArgs>(Actor actor, in Pose pose, ISpawnArgs<TArgs> argsTarget, in TArgs args, SpawnReason reason = SpawnReason.Spawned) where TArgs : struct
		{
			ThrowIfDisposed();

			if (!CheckOwned(actor)) {
				return;
			}

			switch (actor.State) {
				case ActorState.Acquired:
					break;
				case ActorState.Active:
				case ActorState.Despawning:
					return;
				default:
					m_logger.Error("Activate on actor [{actor}] in state [{state}]", ("actor", actor.name), ("state", actor.State));
					return;
			}

			actor.Transform.SetPositionAndRotation(pose.position, pose.rotation);
			actor.Id = m_registry.Allocate(actor);
			actor.State = ActorState.Active;
			actor.SpawnReason = reason;
			AliveCount++;

			actor.gameObject.SetActive(true);

			argsTarget?.Initialize(args);

			var behaviours = actor.Behaviours;
			for (var i = 0; i < behaviours.Length; i++) {
				behaviours[i].OnSpawned();
			}

			// Joins the tick lists at the next sync so a running loop is never mutated
			m_pendingAdd.Add(actor);
			m_spawned.Emit(actor);
		}

		private struct NoArgs { }

		private Actor ResolvePrefab(ActorPrefabId prefabId)
		{
			if (!Prefabs.TryGet(prefabId, out var prefab)) {
				throw new ArgumentException($"No prefab registered as {prefabId}", nameof(prefabId));
			}

			return prefab;
		}

		// ----- Despawning -----------------------------------------------------------------------------------------

		/// <summary>
		/// Marks the actor dead now (not alive, not ticked, refs null); OnDespawned and the return to the pool happen
		/// at the next sync point. Safe from anywhere, including ticks and callbacks. A no-op if already despawning.
		/// </summary>
		public void Despawn(Actor actor)
		{
			if (ReferenceEquals(actor, null) || IsDisposed || !CheckOwned(actor)) {
				return;
			}

			switch (actor.State) {
				case ActorState.Active:
					actor.State = ActorState.Despawning;
					AliveCount--;
					m_pendingRemove.Add(actor);
					break;

				case ActorState.Acquired:
					// Never activated: nothing was called on it, so nothing to undo
					actor.Pool.Release(actor);
					break;

				case ActorState.Pooled:
					if (!m_loggedPooledDespawn) {
						m_loggedPooledDespawn = true;
						m_logger.Warning("Despawn on a pooled actor [{actor}]: a stale reference. Hold an ActorRef instead. (Logged once)", ("actor", actor.name));
					}
					break;
			}
		}

		/// <summary>Despawns the actor with this id. False if it was not alive.</summary>
		public bool Despawn(ActorId id)
		{
			if (!TryGet(id, out var actor)) {
				return false;
			}

			Despawn(actor);
			return true;
		}

		// ----- Queries --------------------------------------------------------------------------------------------

		public bool TryGet(ActorId id, out Actor actor)
		{
			if (m_registry.TryGet(id, out actor) && actor.State == ActorState.Active) {
				return true;
			}

			actor = null;
			return false;
		}

		public bool TryGet<T>(ActorId id, out T behaviour) where T : class
		{
			if (TryGet(id, out var actor)) {
				return actor.TryGet(out behaviour);
			}

			behaviour = null;
			return false;
		}

		/// <summary>Every live T, for a registered T. Enumeration skips actors despawned since the last sync.</summary>
		public ActorList<T> All<T>() where T : ActorBehaviour
		{
			if (m_groupsByType.TryGetValue(typeof(T), out var group)) {
				return ((TickGroup<T>) group).All;
			}

			m_logger.Error("All<{type}>() on an unregistered type. Register it in the StageConfig.", ("type", typeof(T).Name));
			return ActorList<T>.Empty;
		}

		// ----- Ticking --------------------------------------------------------------------------------------------

		/// <summary>Presentation tick. Sync → Tick groups → Sync.</summary>
		public void Tick(float deltaTime)
		{
			if (!BeginTick()) {
				return;
			}

			Sync();

			var scaled = Time.IsPaused ? 0f : deltaTime * Time.Scale;
			Time.DeltaTime = scaled;
			Time.Elapsed += scaled;

			if (!Time.IsPaused) {
				for (var i = 0; i < m_groups.Count; i++) {
					m_groups[i].Tick(scaled);
				}

				m_fallbackGroup?.Tick(scaled);
			}

			Sync();
			EndTick();
		}

		/// <summary>Gameplay tick: FixedTickBegin → physics step (if attached) → FixedTickEnd.</summary>
		public void FixedTick(float fixedDeltaTime)
		{
			if (!FixedTickBegin(fixedDeltaTime)) {
				return;
			}

			if (!Time.IsPaused && Physics != null) {
				using (s_physicsStepMarker.Auto()) {
					Physics.Step(Time.FixedDeltaTime);
				}
			}

			FixedTickEnd();
		}

		/// <summary>
		/// First half of a fixed step: Sync → fixed-step logic. For callers that own the physics step: call this,
		/// step physics, then <see cref="FixedTickEnd"/>.
		/// </summary>
		public bool FixedTickBegin(float fixedDeltaTime)
		{
			if (!BeginTick()) {
				return false;
			}

			m_fixedBegun = true;
			Sync();

			var scaled = Time.IsPaused ? 0f : fixedDeltaTime * Time.Scale;
			Time.FixedDeltaTime = scaled;
			Time.FixedElapsed += scaled;

			if (!Time.IsPaused) {
				Time.FixedStep++;

				for (var i = 0; i < m_groups.Count; i++) {
					m_groups[i].FixedTick(scaled);
				}

				m_fallbackGroup?.FixedTick(scaled);
			}

			return true;
		}

		/// <summary>Second half of a fixed step: deliver buffered physics events → Sync.</summary>
		public void FixedTickEnd()
		{
			if (!m_fixedBegun) {
				m_logger.Error("FixedTickEnd without FixedTickBegin");
				return;
			}

			if (Physics != null) {
				using (s_physicsDeliverMarker.Auto()) {
					Physics.DeliverEvents();
				}
			}

			Sync();
			m_fixedBegun = false;
			EndTick();
		}

		private bool BeginTick()
		{
			if (IsDisposed) {
				return false;
			}

			if (m_ticking) {
				m_logger.Error("Tick called while already ticking; ignored");
				return false;
			}

			m_ticking = true;
			return true;
		}

		private void EndTick()
		{
			m_ticking = false;

			if (m_clearRequested) {
				m_clearRequested = false;
				Clear();
			}
		}

		// ----- Sync -----------------------------------------------------------------------------------------------

		/// <summary>
		/// Applies pending removes and adds, repeating for anything spawned or despawned by the callbacks, up to
		/// <see cref="MAX_SYNC_PASSES"/> times. Only here do actors enter or leave the tick lists.
		/// </summary>
		private void Sync()
		{
			using var marker = s_syncMarker.Auto();

			for (var pass = 0; pass < MAX_SYNC_PASSES; pass++) {
				if (m_pendingRemove.Count == 0 && m_pendingAdd.Count == 0) {
					return;
				}

				// Swap the queues out so re-entrant pushes from callbacks land in the next pass
				var removes = m_pendingRemove;
				m_pendingRemove = m_pendingRemoveScratch;
				m_pendingRemoveScratch = removes;

				for (var i = 0; i < removes.Count; i++) {
					FinishRemove(removes[i]);
				}

				removes.Clear();

				var adds = m_pendingAdd;
				m_pendingAdd = m_pendingAddScratch;
				m_pendingAddScratch = adds;

				for (var i = 0; i < adds.Count; i++) {
					FinishAdd(adds[i]);
				}

				adds.Clear();
			}

			if (m_pendingRemove.Count > 0 || m_pendingAdd.Count > 0) {
				m_logger.Error("Sync ran {passes} passes and still has work: something spawns and despawns in a loop. Leftovers run next sync.", ("passes", MAX_SYNC_PASSES));
			}
		}

		private void FinishAdd(Actor actor)
		{
			if (actor.State != ActorState.Active || actor.IsScheduled) {
				// Despawned or destroyed before it was ever scheduled; the remove queue handles the rest
				return;
			}

			var behaviours = actor.Behaviours;
			for (var i = 0; i < behaviours.Length; i++) {
				var behaviour = behaviours[i];
				behaviour.TypeInfo.Group.Add(behaviour, behaviour.TypeInfo);
			}

			actor.IsScheduled = true;
		}

		private void FinishRemove(Actor actor)
		{
			var behaviours = actor.Behaviours;

			if (actor.IsScheduled) {
				actor.IsScheduled = false;
				for (var i = 0; i < behaviours.Length; i++) {
					behaviours[i].TypeInfo.Group.Remove(behaviours[i]);
				}
			}

			var id = actor.Id;

			switch (actor.State) {
				case ActorState.Despawning:
					for (var i = 0; i < behaviours.Length; i++) {
						behaviours[i].OnDespawned();
					}

					m_despawned.Emit(actor);

					actor.gameObject.SetActive(false);
					m_registry.Free(id);
					actor.Pool.Release(actor);
					break;

				case ActorState.Destroyed:
					m_despawned.Emit(actor);

					if (!id.IsNone) {
						m_registry.Free(id);
						actor.Id = ActorId.None;
					}

					actor.Pool.Forget(actor);
					break;

				default:
					m_logger.Error("Actor [{actor}] in the remove queue with state [{state}]", ("actor", actor.name), ("state", actor.State));
					break;
			}
		}

		// ----- Teardown -------------------------------------------------------------------------------------------

		/// <summary>
		/// Despawns everything, including actors spawned this frame and by OnDespawned callbacks, and returns
		/// acquired instances to their pools. From inside a tick it is deferred to that tick's closing sync.
		/// </summary>
		public void Clear()
		{
			if (IsDisposed) {
				return;
			}

			if (m_ticking) {
				m_clearRequested = true;
				return;
			}

			for (var pass = 0; pass < MAX_SYNC_PASSES; pass++) {
				var any = m_pools.ReleaseAcquired();

				for (var i = 0; i < m_registry.Capacity; i++) {
					var actor = m_registry.GetAt(i);
					if (!ReferenceEquals(actor, null) && actor.State == ActorState.Active) {
						Despawn(actor);
						any = true;
					}
				}

				if (!any && m_pendingRemove.Count == 0 && m_pendingAdd.Count == 0) {
					return;
				}

				Sync();
			}

			if (m_registry.Count > 0) {
				m_logger.Error("Clear gave up after {passes} passes with {count} actors left", ("passes", MAX_SYNC_PASSES), ("count", m_registry.Count));
			}
		}

		/// <summary>Clears, then destroys every pooled instance and the pool roots. A no-op while the application quits.</summary>
		public void Dispose()
		{
			if (IsDisposed) {
				return;
			}

			if (LifeCycle.Instance.IsQuitting) {
				IsDisposed = true;
				return;
			}

			if (m_ticking) {
				m_logger.Error("Dispose called during a tick; ignored");
				return;
			}

			Clear();

			m_disposing = true;
			m_pools.DestroyAll();

			if (m_root != null) {
				ActorPool.DestroyObject(m_root.gameObject);
			}

			m_spawned.Dispose();
			m_despawned.Dispose();
			IsDisposed = true;
		}

		// ----- Internals ------------------------------------------------------------------------------------------

		/// <summary>Called once per pooled instance when it is created: injection and tick-group lookup.</summary>
		internal void InitializeInstance(Actor actor)
		{
			var behaviours = actor.Behaviours;
			var container = Container;

			if (container != null) {
				for (var i = 0; i < behaviours.Length; i++) {
					if (behaviours[i] is IInjectionProvider) {
						actor.Container = new InjectorContainer(container, actor.name);
						container = actor.Container;
						break;
					}
				}

				if (actor.Container != null) {
					for (var i = 0; i < behaviours.Length; i++) {
						if (behaviours[i] is IInjectionProvider provider) {
							provider.Provide(actor.Container);
						}
					}
				}

				for (var i = 0; i < behaviours.Length; i++) {
					if (TypeUtils.IsInjectable(behaviours[i])) {
						container.Inject(behaviours[i]);
					}
				}
			}

			for (var i = 0; i < behaviours.Length; i++) {
				behaviours[i].TypeInfo = GetTypeInfo(behaviours[i].GetType());
			}
		}

		private TypeInfo GetTypeInfo(Type type)
		{
			if (m_typeInfos.TryGetValue(type, out var info)) {
				return info;
			}

			TickDetection.Detect(type, out var ticks, out var fixedTicks);
			info = new TypeInfo { Ticks = ticks, FixedTicks = fixedTicks };

			// Own registration, else the nearest registered base type
			for (var t = type; t != null && t != typeof(ActorBehaviour); t = t.BaseType) {
				if (m_groupsByType.TryGetValue(t, out var group)) {
					info.Group = group;
					break;
				}
			}

			if (info.Group == null) {
				if (ticks || fixedTicks) {
					m_logger.Error("[{type}] overrides Tick/FixedTick but is not registered in the StageConfig. It ticks last. (Logged once)", ("type", type.Name));
				}

				m_fallbackGroup ??= new TickGroup<ActorBehaviour>(null);
				info.Group = m_fallbackGroup;
			}

			m_typeInfos.Add(type, info);
			return info;
		}

		/// <summary>From Actor.OnDestroy: the GameObject is gone, forget it at the next sync.</summary>
		internal void OnActorDestroyed(Actor actor)
		{
			if (IsDisposed || m_disposing) {
				return;
			}

			var previous = actor.State;
			actor.State = ActorState.Destroyed;

			switch (previous) {
				case ActorState.Active:
					AliveCount--;
					m_pendingRemove.Add(actor);
					break;

				case ActorState.Despawning:
					// Already queued
					break;

				case ActorState.Pooled:
				case ActorState.Acquired:
					actor.Pool.Forget(actor);
					break;
			}
		}

		private bool CheckOwned(Actor actor)
		{
			if (ReferenceEquals(actor.Stage, this)) {
				return true;
			}

			m_logger.Error("Actor [{actor}] does not belong to stage [{stage}]", ("actor", actor.name), ("stage", Name));
			return false;
		}

		private void ThrowIfDisposed()
		{
			if (IsDisposed) {
				throw new ObjectDisposedException(Name);
			}
		}

		// ----- Diagnostics (for the DevBoard page and tests) -----------------------------------------------------

		internal IReadOnlyList<TickGroup> Groups => m_groups;
		internal TickGroup FallbackGroup => m_fallbackGroup;
		internal IReadOnlyCollection<ActorPool> Pools => m_pools.Pools;
		internal int PendingAddCount => m_pendingAdd.Count;
		internal int PendingRemoveCount => m_pendingRemove.Count;
		internal int RegisteredCount => m_registry.Count;
	}
}
