using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using Unity.Collections.LowLevel.Unsafe;

namespace Tekly.Simulant.Core
{
	public partial class DataPool<T> where T : struct
	{
		public void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			output.Write(m_data.Count);
			
			if (typeof(ISuperSerialize).IsAssignableFrom(DataType)) {
				var writer = SuperSerializeStructs<T>.Write;
				for (int i = 0, length = m_data.Count; i < length; i++) {
					writer.Invoke(ref m_data.Data[i], output, superSerializer);
				}
			} else if (UnsafeUtility.IsBlittable<T>()) {
				output.WriteBlittable(m_data.Data, m_data.Count);
			} else {
				var converter = superSerializer.GetConverter(DataType);
				for (int i = 0, length = m_data.Count; i < length; i++) {
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
		}
	}
}