using System;
using Tekly.Injectors;
using Tekly.Injectors.Utils;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// The root of a pooled gameplay object: a GameObject made of <see cref="ActorBehaviour"/>s. The stage owns its
	/// state; game code holds it through <see cref="ActorRef"/> or <see cref="ActorId"/>.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class Actor : MonoBehaviour
	{
		public ActorId Id { get; internal set; }
		public ActorState State { get; internal set; }
		public bool IsAlive => State == ActorState.Active;
		public Stage Stage { get; internal set; }

		/// <summary>Why the current activation happened. Valid from Initialize/OnSpawned until the actor is pooled.</summary>
		public SpawnReason SpawnReason { get; internal set; }

		/// <summary>Cached transform; valid once the pool has bound the instance.</summary>
		public Transform Transform => m_transform;

		/// <summary>
		/// Every behaviour of this actor, in hierarchy order. Behaviours of nested actors are not included. Serialized
		/// by <see cref="Collect"/>, which the editor runs from OnValidate; an actor built in code calls it itself.
		/// </summary>
		public ActorBehaviour[] Behaviours => m_behaviours ?? Array.Empty<ActorBehaviour>();

		/// <summary>
		/// The injector on this actor's root, or null. It is the only way an actor is injected: the stage drives it
		/// once per pooled instance, so it must not inject itself from Awake or OnEnable.
		/// </summary>
		public HierarchyInjector Injector => m_injector;

		[SerializeField] private ActorBehaviour[] m_behaviours;
		[SerializeField] private HierarchyInjector m_injector;

		private Transform m_transform;

		internal ActorPool Pool;
		internal int PoolIndex = -1;
		internal InjectorContainer Container;

		// Only Stage.Sync sets or clears this: true iff the behaviours are in their tick groups
		internal bool IsScheduled;

		/// <summary>Despawn, deferred to the next sync point. A no-op if the actor is not alive.</summary>
		public void Despawn()
		{
			Stage?.Despawn(this);
		}

		/// <summary>The first behaviour that is a T (a type or an interface), or null.</summary>
		public T Get<T>() where T : class
		{
			TryGet<T>(out var result);
			return result;
		}

		public bool TryGet<T>(out T result) where T : class
		{
			var behaviours = Behaviours;
			for (var i = 0; i < behaviours.Length; i++) {
				if (behaviours[i] is T match) {
					result = match;
					return true;
				}
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Finds the behaviours and the injector and stores them in the serialized fields. The editor calls this from
		/// OnValidate; call it after building an actor in code. Never called by the stage at runtime.
		/// </summary>
		public void Collect()
		{
			TryGetComponent(out m_injector);

			using (ListPool<ActorBehaviour>.Get(out var found)) {
				GetComponentsInChildren(true, found);

				using (ListPool<ActorBehaviour>.Get(out var mine)) {
					for (var i = 0; i < found.Count; i++) {
						var behaviour = found[i];
						if (behaviour.GetComponentInParent<Actor>(true) == this) {
							mine.Add(behaviour);
						}
					}

					m_behaviours = mine.ToArray();
				}
			}
		}

		/// <summary>Caches the transform and points the behaviours back at this actor. Once per object, by the pool.</summary>
		internal void Bind()
		{
			m_transform = transform;

			var behaviours = Behaviours;
			for (var i = 0; i < behaviours.Length; i++) {
				behaviours[i].Actor = this;
			}
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		/// <summary>
		/// True when the serialized behaviours match what is in the hierarchy. False, with a reason, when
		/// <see cref="Collect"/> never ran or a behaviour was added, removed or destroyed since. Editor and
		/// development builds only: the data is fixed at build time, so a release build has nothing new to learn.
		/// </summary>
		internal bool HasValidBehaviours(out string problem)
		{
			if (m_behaviours != null) {
				for (var i = 0; i < m_behaviours.Length; i++) {
					if (m_behaviours[i] == null) {
						problem = $"behaviour [{i}] is missing";
						return false;
					}
				}
			}

			using (ListPool<ActorBehaviour>.Get(out var found)) {
				GetComponentsInChildren(true, found);

				var expected = 0;
				for (var i = 0; i < found.Count; i++) {
					if (found[i].GetComponentInParent<Actor>(true) == this) {
						expected++;
					}
				}

				var actual = m_behaviours?.Length ?? 0;
				if (expected != actual) {
					problem = $"hierarchy has [{expected}] behaviours, [{actual}] are serialized";
					return false;
				}
			}

			problem = null;
			return true;
		}
#endif

		private void OnDestroy()
		{
			Stage?.OnActorDestroyed(this);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			Collect();

			if (m_injector != null && m_injector.SelfInjects) {
				Debug.LogWarning($"Actor [{name}]: the stage drives its HierarchyInjector, so inject on Awake/OnEnable have been turned off", this);
				m_injector.DisableAutoInject();
				UnityEditor.EditorUtility.SetDirty(m_injector);
			}
		}
#endif

		public override string ToString()
		{
			return $"{name} [{State} {Id}]";
		}
	}
}
