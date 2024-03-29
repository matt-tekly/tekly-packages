using System;

namespace Tekly.Simulant.Core
{
	[Serializable]
	public struct EntityData
	{
		public short Generation;
		public short ComponentsCount;
		public short PersistentComponents;
	}

	[Serializable]
	public struct EntityRef : IEquatable<EntityRef>
	{
		public int Entity;
		public int Gen;

		public bool IsValid => Entity != -1 && Gen != -1;
		public static readonly EntityRef Invalid = new EntityRef { Entity = -1, Gen = -1 };

		public bool Equals(EntityRef other) => Entity == other.Entity && Gen == other.Gen;
		public override bool Equals(object obj) => obj is EntityRef other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(Entity, Gen);
		
		public static bool operator ==(EntityRef left, EntityRef right) => left.Equals(right);
		public static bool operator !=(EntityRef left, EntityRef right) => !left.Equals(right);
	}

	public static class EntityExtensions
	{
		public static EntityRef GetRef(this World world, int entity)
		{
			return new EntityRef {
				Entity = entity,
				Gen = world.Entities.Data[entity].Generation
			};
		}

		public static bool IsValidRef(in EntityRef entityRef, World world)
		{
			return entityRef.IsValid 
			       && world.IsAlive(entityRef.Entity) 
			       && world.Entities.Data[entityRef.Entity].Generation == entityRef.Gen;
		}
		
		public static bool TryGet(this World world, in EntityRef entityRef, out int entity)
		{
			if (!IsValidRef(in entityRef, world)) {
				entity = -1;
				return false;
			}

			entity = entityRef.Entity;
			return true;
		}
		
		public static bool TryGet(this in EntityRef entityRef, World world, out int entity)
		{
			if (!IsValidRef(in entityRef, world)) {
				entity = -1;
				return false;
			}

			entity = entityRef.Entity;
			return true;
		}
	}
}