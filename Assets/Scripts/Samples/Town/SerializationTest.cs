using System.IO;
using Tekly.Simulant.Core;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;
using UnityEngine.Profiling;

namespace TeklySample.Samples.Town
{
	public class SerializationTest : MonoBehaviour
	{
		[SerializeField] private int m_amount = 1000;
		
		private SuperSerializer m_serializer = new SuperSerializer();
		private World m_writeWorld;
		
		private byte[] m_bytes;
		private MemoryStream m_stream = new MemoryStream();
		
		private struct FlagData { }

		private struct MetaData
		{
			public string Id;
		}
		
		private struct FriendData
		{
			public EntityRef Friend;
		}
		
		private struct CountData : ISuperSerialize
		{
			public int Count;
			
			public void Write(TokenOutputStream output, SuperSerializer superSerializer)
			{
				output.Write(Count);
			}

			public void Read(TokenInputStream input, SuperSerializer superSerializer)
			{
				Count = input.ReadInt();
			}
		}
		
		private struct NotUsedData { }
		
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.W)) {
				Write();
			}
			
			if (Input.GetKeyDown(KeyCode.C)) {
				Create();
			}
			
			if (Input.GetKeyDown(KeyCode.R)) {
				Profiler.BeginSample("Read Test");
				Read();
				Profiler.EndSample();
			}
		}

		private void Read()
		{
			
			var worldConfig = new WorldConfig {
				EntityCapacity = 512,
				DataPools = new DataPoolConfig {
					Capacity = 512,
					RecycleCapacity = 512
				}
			};

			m_stream.Seek(0, SeekOrigin.Begin);
			using var inputStream = new TokenInputStream(m_stream, true);
			var worldB = new World(worldConfig);
			
			m_serializer.Read(inputStream, worldB);
		}

		private void Create()
		{
			Profiler.BeginSample("Create");
			const string ID = "TEST_ID";

			var worldConfig = new WorldConfig {
				EntityCapacity = 512,
				DataPools = new DataPoolConfig {
					Capacity = 512,
					RecycleCapacity = 512
				}
			};
			
			m_writeWorld = new World(worldConfig);

			for (var i = 0; i < 1000; i++) {
				var entity = m_writeWorld.Create();
				m_writeWorld.Add<FlagData>(entity);
				m_writeWorld.Add(entity, new CountData {Count = i});
			}
			
			var entity1 = m_writeWorld.Create();
			var entity2 = m_writeWorld.Create();
			
			m_writeWorld.Add(entity2, new FriendData { Friend = m_writeWorld.GetRef(entity1) });
			m_writeWorld.Add(entity2, new MetaData { Id = ID });
			m_writeWorld.Add<FlagData>(entity2);
			
			m_writeWorld.GetPool<NotUsedData>();
			Profiler.EndSample();
		}

		private void Write()
		{
			if (m_writeWorld == null) {
				return;
			}
			
			Profiler.BeginSample("Write");
			
			m_stream.SetLength(0);
			using var outputStream = new TokenOutputStream(m_stream);
			
			m_serializer.Write(outputStream, m_writeWorld);
			outputStream.Flush();
			
			m_bytes = m_stream.ToArray();
			Profiler.EndSample();
		}
	}
}