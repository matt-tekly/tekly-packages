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

		private MemoryStream m_stream = new MemoryStream();

		private struct FlagData { }

		private struct MetaData : ISuperSerialize
		{
			public string Id;

			public void Write(TokenOutputStream output, SuperSerializer superSerializer)
			{
				output.Write(Id);
			}

			public void Read(TokenInputStream input, SuperSerializer superSerializer)
			{
				Id = input.ReadString();
			}
		}

		private enum FriendType
		{
			Best,
			Buddy
		}
		
		private struct FriendData
		{
			public EntityRef Friend;
			public FriendType Type;
			public Hash128 Hash;
		}

		private struct TransformData
		{
			public Vector3 Position;
			public Quaternion Rotation;
			public Vector3 Scale;
		}

		private struct CountData
		{
			public int Count;
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

			if (Input.GetKeyDown(KeyCode.T)) {
				ReadTest();
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
			var readWorld = new World(worldConfig);

			m_serializer.Read(inputStream, readWorld);

			Debug.Log(readWorld.GetSummary().ToString());
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

			for (var i = 0; i < m_amount; i++) {
				var entity = m_writeWorld.Create();
				m_writeWorld.Add<FlagData>(entity);
				m_writeWorld.Add(entity, new CountData { Count = i });
				m_writeWorld.Add(entity, new TransformData { Position = new Vector3(i, 0, 0) });
				m_writeWorld.Add(entity, new MetaData { Id = i.ToString() });
			}

			var entity1 = m_writeWorld.Create();
			var entity2 = m_writeWorld.Create();

			m_writeWorld.Add(entity2, new FriendData { Friend = m_writeWorld.GetRef(entity1) });
			m_writeWorld.Add(entity2, new MetaData { Id = ID });
			m_writeWorld.Add<FlagData>(entity2);

			m_writeWorld.GetPool<NotUsedData>();
			Profiler.EndSample();

			Debug.Log(m_writeWorld.GetSummary().ToString());
		}

		private void ReadTest()
		{
			var worldConfig = new WorldConfig {
				EntityCapacity = 512,
				DataPools = new DataPoolConfig {
					Capacity = 512,
					RecycleCapacity = 512
				}
			};

			using var fs = File.OpenRead("test.bytes");
			using var inputStream = new TokenInputStream(fs, true);
			var readWorld = new World(worldConfig);

			m_serializer.Read(inputStream, readWorld);

			Debug.Log(readWorld.GetSummary().ToString());
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

			File.WriteAllBytes("test.bytes", m_stream.ToArray());
			Profiler.EndSample();
		}
		
		private void Write(World world, string file)
		{
			using var fileStream = File.OpenWrite(file);
			using var outputStream = new TokenOutputStream(fileStream);

			var serializer = new SuperSerializer();
			serializer.Write(outputStream, world);
			outputStream.Flush();
		}

		public World Read(string file)
		{
			var worldConfig = new WorldConfig {
				EntityCapacity = 512,
				DataPools = new DataPoolConfig {
					Capacity = 512,
					RecycleCapacity = 512
				}
			};

			using var fs = File.OpenRead(file);
			using var inputStream = new TokenInputStream(fs, true);
			var world = new World(worldConfig);

			m_serializer.Read(inputStream, world);

			return world;
		}
	}
}