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
			
			for (int index = 0, length = Entities.Count; index < length; index++) {
				var entity = Entities.Data[index];
				output.Write(entity.ComponentsCount);
				output.Write(entity.Generation);
			}
			
			output.Write(m_recycledEntities.Count);
			for (int index = 0, length = m_recycledEntities.Count; index < length; index++) {
				output.Write(m_recycledEntities.Data[index]);
			}
			
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
					output.Write(pool.DataType.AssemblyQualifiedName);
					pool.Write(output, superSerializer);	
				}
			}
		}

		public void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			// Read Entity meta data
			var entityCapacity = input.ReadInt();
			Entities.Resize(entityCapacity);

			Entities.Count = input.ReadInt();
			
			for (int i = 0, length = Entities.Count; i < length; i++) {
				ref var entityData = ref Entities.Data[i];
				entityData.ComponentsCount = input.ReadShort();
				entityData.Generation = input.ReadShort();
			}
			
			var recycledEntities = input.ReadInt();
			
			for (var i = 0; i < recycledEntities; i++) {
				m_recycledEntities.Add(input.ReadInt());
			}

			// Read Pools
			var poolCount = input.ReadInt();
			m_pools.Count = poolCount;
			m_pools.Resize(poolCount);
			
			var poolParams = new object[4];
			poolParams[0] = this;
			poolParams[2] = entityCapacity;
			poolParams[3] = m_config.DataPools;

			var rootPoolType = typeof(DataPool<>);
			var typeParams = new Type[1];   
			
			for (var i = 0; i < poolCount; i++) {
				poolParams[1] = i;
				
				var typeName = input.ReadString();
				typeParams[0] = Type.GetType(typeName);
				
				var poolType = rootPoolType.MakeGenericType(typeParams);
				var pool = (IDataPool) Activator.CreateInstance(poolType, poolParams);
			
				pool.Read(input, superSerializer);
				
				m_pools.Data[i] = pool;
				m_poolMap[pool.DataType] = pool;
			}
		}
	}
}