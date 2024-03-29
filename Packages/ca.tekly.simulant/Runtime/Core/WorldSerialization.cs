using System;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.Simulant.Core
{
	public partial class World
	{
		public void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			// Write Entity meta data
			output.Write(EntityCapacity);
			
			output.Write(Entities.Count);
			output.WriteBlittable(Entities.Data, Entities.Count);
			
			output.Write(m_recycledEntities.Count);
			output.WriteBlittable(m_recycledEntities.Data, m_recycledEntities.Count);
			
			// Write Pools
			var poolCount = 0;
			for (int i = 0, length = m_pools.Count; i < length; i++) {
				if (m_pools.Data[i].ShouldSerialize) {
					poolCount++;
				}	
			}
			
			output.Write(poolCount);

			for (int i = 0, length = m_pools.Count; i < length; i++) {
				var pool = m_pools.Data[i];
				if (pool.ShouldSerialize) {
					output.Write(pool.TypeInfo.Assembly);
					pool.Write(output, superSerializer);	
				}
			}
		}

		public void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			// Read Entity meta data
			var entityCapacity = input.ReadInt();
			Entities.Resize(Math.Max(entityCapacity, m_config.EntityCapacity));

			Entities.Count = input.ReadInt();
			input.ReadBlittable(Entities.Data, Entities.Count);

			for (var i = Entities.Count - 1; i >= 0; i--) {
				ref var entityData = ref Entities.Data[i];
				entityData.ComponentsCount = entityData.PersistentComponents;
			}
			
			var recycledEntities = input.ReadInt();
			m_recycledEntities.Resize(Math.Max(recycledEntities, m_config.EntityCapacity));
			m_recycledEntities.Count = recycledEntities;
			
			input.ReadBlittable(m_recycledEntities.Data, m_recycledEntities.Count);

			// Read Pools
			var poolCount = input.ReadInt();
			m_pools.Count = poolCount;
			m_pools.Resize(Math.Max(poolCount, DATA_TYPE_CAPACITY));
			
			var poolParams = new object[4];
			poolParams[0] = this;
			poolParams[2] = entityCapacity;
			poolParams[3] = m_config.DataPools;

			var rootPoolType = typeof(DataPool<>);
			var typeParams = new Type[1];   
			
			for (var i = 0; i < poolCount; i++) {
				poolParams[1] = i;
				
				var typeName = input.ReadString();
				
				// TODO: There will need to be type migration if the name of the class changed
				typeParams[0] = Type.GetType(typeName);

				if (typeParams[0] == null) {
					throw new Exception("Failed to find type during deserialization: " + typeName);
				}
				
				var poolType = rootPoolType.MakeGenericType(typeParams);
				var pool = (IDataPool) Activator.CreateInstance(poolType, poolParams);
			
				pool.Read(input, superSerializer);
				
				m_pools.Data[i] = pool;
				m_poolMap[pool.DataType] = pool;
			}
		}
	}
}