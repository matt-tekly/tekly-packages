using System.Collections.Generic;
using Tekly.Injectors;
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

		/// <summary>Cached transform; valid after the stage has initialized the instance.</summary>
		public Transform Transform => m_transform;

		/// <summary>Every behaviour of this actor, in hierarchy order. Behaviours of nested actors are not included.</summary>
		public ActorBehaviour[] Behaviours {
			get {
				EnsureInitialized();
				return m_behaviours;
			}
		}

		[SerializeField] private ActorBehaviour[] m_behaviours;

		private Transform m_transform;
		private bool m_initialized;

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
		/// Makes the behaviour array and the back references valid. Works whether or not OnValidate ever ran, so
		/// prefabs built in code are fine.
		/// </summary>
		internal void EnsureInitialized()
		{
			if (m_initialized) {
				return;
			}

			m_initialized = true;
			m_transform = transform;

			if (!IsBehaviourArrayValid()) {
				Collect();
			}

			for (var i = 0; i < m_behaviours.Length; i++) {
				m_behaviours[i].Actor = this;
			}
		}

		private bool IsBehaviourArrayValid()
		{
			if (m_behaviours == null || m_behaviours.Length == 0) {
				return false;
			}

			for (var i = 0; i < m_behaviours.Length; i++) {
				if (ReferenceEquals(m_behaviours[i], null)) {
					return false;
				}
			}

			return true;
		}

		private void Collect()
		{
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

		private void OnDestroy()
		{
			Stage?.OnActorDestroyed(this);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			Collect();
		}
#endif

		public override string ToString()
		{
			return $"{name} [{State} {Id}]";
		}
	}
}
