namespace Tekly.Stagecraft
{
	/// <summary>
	/// A reference to an actor that reads as null the moment the actor stops being alive: as soon as Despawn is
	/// called, not only once the instance is pooled or reused. This is what game code stores across frames.
	/// </summary>
	public readonly struct ActorRef
	{
		public ActorId Id => m_id;

		/// <summary>The actor, or null if it is no longer alive.</summary>
		public Actor Value => TryGet(out var actor) ? actor : null;

		public bool IsAlive => TryGet(out _);

		private readonly ActorId m_id;
		private readonly Actor m_actor;

		public ActorRef(Actor actor)
		{
			if (ReferenceEquals(actor, null) || !actor.IsAlive) {
				m_id = ActorId.None;
				m_actor = null;
			} else {
				m_id = actor.Id;
				m_actor = actor;
			}
		}

		internal ActorRef(ActorId id, Actor actor)
		{
			m_id = id;
			m_actor = actor;
		}

		public bool TryGet(out Actor actor)
		{
			// No Unity null check: a destroyed actor's state is Destroyed, and the managed object stays readable.
			if (!ReferenceEquals(m_actor, null) && m_actor.State == ActorState.Active && m_actor.Id == m_id) {
				actor = m_actor;
				return true;
			}

			actor = null;
			return false;
		}

		public static implicit operator ActorRef(Actor actor)
		{
			return new ActorRef(actor);
		}

		public override string ToString()
		{
			return IsAlive ? $"ActorRef({m_id})" : "ActorRef(null)";
		}
	}

	/// <summary>
	/// A typed <see cref="ActorRef"/> to one behaviour of an actor.
	/// </summary>
	public readonly struct ActorRef<T> where T : ActorBehaviour
	{
		public ActorId Id => m_id;

		/// <summary>The behaviour, or null if its actor is no longer alive.</summary>
		public T Value => TryGet(out var behaviour) ? behaviour : null;

		public bool IsAlive => TryGet(out _);

		private readonly ActorId m_id;
		private readonly T m_behaviour;

		public ActorRef(T behaviour)
		{
			if (ReferenceEquals(behaviour, null) || ReferenceEquals(behaviour.Actor, null) || !behaviour.Actor.IsAlive) {
				m_id = ActorId.None;
				m_behaviour = null;
			} else {
				m_id = behaviour.Actor.Id;
				m_behaviour = behaviour;
			}
		}

		public bool TryGet(out T behaviour)
		{
			if (!ReferenceEquals(m_behaviour, null)) {
				var actor = m_behaviour.Actor;
				if (actor.State == ActorState.Active && actor.Id == m_id) {
					behaviour = m_behaviour;
					return true;
				}
			}

			behaviour = null;
			return false;
		}

		public static implicit operator ActorRef<T>(T behaviour)
		{
			return new ActorRef<T>(behaviour);
		}

		public static implicit operator ActorRef(ActorRef<T> reference)
		{
			return ReferenceEquals(reference.m_behaviour, null)
				? default
				: new ActorRef(reference.m_id, reference.m_behaviour.Actor);
		}

		public override string ToString()
		{
			return IsAlive ? $"ActorRef<{typeof(T).Name}>({m_id})" : $"ActorRef<{typeof(T).Name}>(null)";
		}
	}
}
