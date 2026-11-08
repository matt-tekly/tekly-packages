using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// One pool per prefab. Instances are created inactive (no Awake/OnEnable until the first activation) and
	/// injected once. The pool only ever sees Pooled, Acquired and Destroyed instances; the stage owns the rest.
	/// </summary>
	internal sealed class ActorPool
	{
		public readonly Actor Prefab;
		public readonly Transform Root;

		public int FreeCount => m_free.Count;
		public int TotalCount => m_all.Count;
		public IReadOnlyList<Actor> All => m_all;

		private readonly Stage m_stage;
		private readonly Transform m_staging;
		private readonly Stack<Actor> m_free = new Stack<Actor>();
		private readonly List<Actor> m_all = new List<Actor>();

		public ActorPool(Stage stage, Actor prefab, Transform stageRoot, Transform staging)
		{
			m_stage = stage;
			m_staging = staging;
			Prefab = prefab;

			stage.ValidateActorData(prefab);

			// Binding the prefab makes behaviour.Actor usable on the asset, which Stage.Spawn<T>(prefabBehaviour) relies on
			prefab.Bind();

			Root = new GameObject($"[Pool] {prefab.name}").transform;
			Root.SetParent(stageRoot, false);
		}

		/// <summary>Pooled → Acquired. Skips instances destroyed from outside.</summary>
		public Actor Take()
		{
			while (m_free.Count > 0) {
				var actor = m_free.Pop();
				if (actor.State == ActorState.Pooled) {
					actor.State = ActorState.Acquired;
					return actor;
				}
			}

			var created = Create();
			created.State = ActorState.Acquired;
			return created;
		}

		/// <summary>Any state the stage has finished with → Pooled. The caller has already deactivated the object.</summary>
		public void Release(Actor actor)
		{
			actor.Id = ActorId.None;
			actor.State = ActorState.Pooled;
			m_free.Push(actor);
		}

		/// <summary>An instance destroyed from outside: drop it, O(1), so a scene unload of thousands stays linear.</summary>
		public void Forget(Actor actor)
		{
			var index = actor.PoolIndex;
			if (index < 0 || index >= m_all.Count || !ReferenceEquals(m_all[index], actor)) {
				return;
			}

			var last = m_all.Count - 1;
			var moved = m_all[last];
			m_all[index] = moved;
			moved.PoolIndex = index;
			m_all.RemoveAt(last);
			actor.PoolIndex = -1;
		}

		public void DestroyAll()
		{
			for (var i = 0; i < m_all.Count; i++) {
				var actor = m_all[i];
				if (actor != null) {
					actor.Container?.Dispose();
					DestroyObject(actor.gameObject);
				}
			}

			m_all.Clear();
			m_free.Clear();

			if (Root != null) {
				DestroyObject(Root.gameObject);
			}
		}

		private Actor Create()
		{
			// Instantiating under an inactive parent keeps Awake/OnEnable from running. The instance is then made
			// inactive itself so it can move under the (active) pool root.
			var instance = Object.Instantiate(Prefab, m_staging, false);
			instance.name = Prefab.name;
			instance.gameObject.SetActive(false);
			instance.transform.SetParent(Root, false);

			instance.Pool = this;
			instance.Stage = m_stage;
			instance.State = ActorState.Pooled;
			instance.Bind();

			m_stage.InitializeInstance(instance);
			instance.PoolIndex = m_all.Count;
			m_all.Add(instance);

			return instance;
		}

		public static void DestroyObject(Object obj)
		{
			if (Application.isPlaying) {
				Object.Destroy(obj);
			} else {
				Object.DestroyImmediate(obj);
			}
		}
	}

	internal sealed class ActorPools
	{
		public IReadOnlyCollection<ActorPool> Pools => m_pools.Values;

		private readonly Stage m_stage;
		private readonly Transform m_root;
		private readonly Transform m_staging;
		private readonly Dictionary<Actor, ActorPool> m_pools = new Dictionary<Actor, ActorPool>();

		public ActorPools(Stage stage, Transform root)
		{
			m_stage = stage;
			m_root = root;

			var staging = new GameObject("[Staging]");
			staging.SetActive(false);
			m_staging = staging.transform;
			m_staging.SetParent(root, false);
		}

		public ActorPool Get(Actor prefab)
		{
			if (!m_pools.TryGetValue(prefab, out var pool)) {
				pool = new ActorPool(m_stage, prefab, m_root, m_staging);
				m_pools.Add(prefab, pool);
			}

			return pool;
		}

		/// <summary>Returns every Acquired instance to its pool without callbacks. Part of Clear.</summary>
		public bool ReleaseAcquired()
		{
			var any = false;

			foreach (var pool in m_pools.Values) {
				var all = pool.All;
				for (var i = 0; i < all.Count; i++) {
					var actor = all[i];
					if (actor.State == ActorState.Acquired) {
						pool.Release(actor);
						any = true;
					}
				}
			}

			return any;
		}

		public void DestroyAll()
		{
			foreach (var pool in m_pools.Values) {
				pool.DestroyAll();
			}

			m_pools.Clear();

			if (m_staging != null) {
				ActorPool.DestroyObject(m_staging.gameObject);
			}
		}
	}

	/// <summary>
	/// The stage's bookkeeping for actors it adopted rather than instantiated (scene-placed actors). They have no
	/// pool: when the stage is finished with one it is destroyed. Membership is tracked the same way a pool tracks
	/// its instances, through <see cref="Actor.PoolIndex"/>, so a scene unload of thousands stays linear.
	/// </summary>
	internal sealed class AdoptedActors
	{
		public int Count => m_all.Count;
		public IReadOnlyList<Actor> All => m_all;

		private readonly List<Actor> m_all = new List<Actor>();

		public void Add(Actor actor)
		{
			actor.PoolIndex = m_all.Count;
			m_all.Add(actor);
		}

		/// <summary>Drops the actor without touching it, O(1). For an actor destroyed from outside.</summary>
		public void Forget(Actor actor)
		{
			var index = actor.PoolIndex;
			if (index < 0 || index >= m_all.Count || !ReferenceEquals(m_all[index], actor)) {
				return;
			}

			var last = m_all.Count - 1;
			var moved = m_all[last];
			m_all[index] = moved;
			moved.PoolIndex = index;
			m_all.RemoveAt(last);
			actor.PoolIndex = -1;
		}

		/// <summary>The stage is finished with the actor: forget it and destroy its object. Nothing is called on it.</summary>
		public void Destroy(Actor actor)
		{
			Forget(actor);
			DestroyObject(actor);
		}

		public void DestroyAll()
		{
			for (var i = 0; i < m_all.Count; i++) {
				var actor = m_all[i];
				if (actor != null) {
					actor.PoolIndex = -1;
					DestroyObject(actor);
				}
			}

			m_all.Clear();
		}

		private static void DestroyObject(Actor actor)
		{
			actor.Container?.Dispose();
			actor.Container = null;
			actor.State = ActorState.Destroyed;

			// The stage is doing the destroying, so Actor.OnDestroy has nothing to report
			actor.Stage = null;
			ActorPool.DestroyObject(actor.gameObject);
		}
	}
}
