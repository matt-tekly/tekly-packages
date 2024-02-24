using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using Unity.Collections.LowLevel.Unsafe;

namespace Tekly.Simulant.Core
{
	public partial class DataPool<T> where T : struct
	{
		public void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			var dataCount = m_data.Count;
			output.Write(dataCount);
			
			if (typeof(ISuperSerialize).IsAssignableFrom(DataType)) {
				var writer = SuperSerializeStructs<T>.Write;
				for (int i = 0, length = dataCount; i < length; i++) {
					writer.Invoke(ref m_data.Data[i], output, superSerializer);
				}
			} else if (UnsafeUtility.IsBlittable<T>()) {
				output.WriteBlittable(m_data.Data, dataCount);
			} else {
				var converter = superSerializer.GetConverter(DataType);
				for (int i = 0, length = dataCount; i < length; i++) {
					converter.Write(superSerializer, output, m_data.Data[i]);
				}
			}
			
			for (int i = 0, length = m_entityMap.Data.Length; i < length; i++) {
				var dataIndex = m_entityMap.Data[i];
				if (dataIndex != BAD_ID) {
					output.Write(i); // Entity Index
					output.Write(m_entityMap.Data[i]); // Data Index	
				}
			}
			
			output.Write(m_recycled.Count);
			output.WriteBlittable(m_recycled.Data, m_recycled.Count);
		}

		public void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			var entityCount = input.ReadInt();

			m_data.Resize(entityCount);
			m_data.Count = entityCount;

			if (typeof(ISuperSerialize).IsAssignableFrom(DataType)) {
				var reader = SuperSerializeStructs<T>.Read;
				for (var i = 0; i < entityCount; i++) {
					ref var data = ref m_data.Data[i];
					reader.Invoke(ref data, input, superSerializer);
				}
			} else if (UnsafeUtility.IsBlittable<T>()) {
				input.ReadBlittable(m_data.Data, entityCount);
			} else {
				var converter = superSerializer.GetConverter(DataType);
				for (var i = 0; i < entityCount; i++) {
					m_data.Data[i] = (T)converter.Read(superSerializer, input, null);
				}
			}

			m_entityMap.Count = entityCount;

			for (var i = 0; i < entityCount; i++) {
				var entityId = input.ReadInt();
				var dataIndex = input.ReadInt();

				m_entityMap.Data[entityId] = dataIndex;
			}

			var recycledCount = input.ReadInt();
			m_recycled.Resize(recycledCount);
			m_recycled.Count = recycledCount;
			input.ReadBlittable(m_recycled.Data, recycledCount);
		}
	}
}