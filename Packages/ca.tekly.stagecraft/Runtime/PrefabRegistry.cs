using System;
using System.Collections.Generic;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// A stable, small id for a prefab, so a spawn can be described as (prefab id, pose, args) and sent as data.
	/// Which prefab gets which id is game data; the registry only keeps the mapping unique.
	/// </summary>
	public readonly struct ActorPrefabId : IEquatable<ActorPrefabId>
	{
		public readonly ushort Value;

		public ActorPrefabId(ushort value)
		{
			Value = value;
		}

		public bool Equals(ActorPrefabId other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			return obj is ActorPrefabId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value;
		}

		public static bool operator ==(ActorPrefabId a, ActorPrefabId b)
		{
			return a.Value == b.Value;
		}

		public static bool operator !=(ActorPrefabId a, ActorPrefabId b)
		{
			return a.Value != b.Value;
		}

		public override string ToString()
		{
			return $"Prefab#{Value}";
		}
	}

	public sealed class PrefabRegistry
	{
		public int Count => m_prefabs.Count;

		private readonly Dictionary<ActorPrefabId, Actor> m_prefabs = new Dictionary<ActorPrefabId, Actor>();
		private readonly Dictionary<Actor, ActorPrefabId> m_ids = new Dictionary<Actor, ActorPrefabId>();

		public void Register(ActorPrefabId id, Actor prefab)
		{
			if (prefab == null) {
				throw new ArgumentNullException(nameof(prefab));
			}

			if (m_prefabs.TryGetValue(id, out var existing) && existing != prefab) {
				throw new ArgumentException($"{id} is already registered to {existing.name}");
			}

			if (m_ids.TryGetValue(prefab, out var existingId) && existingId != id) {
				throw new ArgumentException($"{prefab.name} is already registered as {existingId}");
			}

			m_prefabs[id] = prefab;
			m_ids[prefab] = id;
		}

		/// <summary>Registers prefabs in order, ids starting at <paramref name="firstId"/>.</summary>
		public void Register(IReadOnlyList<Actor> prefabs, ushort firstId = 0)
		{
			for (var i = 0; i < prefabs.Count; i++) {
				Register(new ActorPrefabId((ushort) (firstId + i)), prefabs[i]);
			}
		}

		public bool TryGet(ActorPrefabId id, out Actor prefab)
		{
			return m_prefabs.TryGetValue(id, out prefab);
		}

		public bool TryGetId(Actor prefab, out ActorPrefabId id)
		{
			return m_ids.TryGetValue(prefab, out id);
		}
	}
}
