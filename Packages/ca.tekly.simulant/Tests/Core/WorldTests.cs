using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.Simulant.Core
{
	public class WorldTests
	{
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
		
		private struct FriendData
		{
			public EntityRef Friend;
		}
		
		private struct CountData
		{
			public int Count;
		}
		
		private struct NotUsedData { }

		[Test]
		public void QueryAddAndDeleteDataTest()
		{
			var worldConfig = new WorldConfig {
				EntityCapacity = 2,
				DataPools = new DataPoolConfig {
					Capacity = 2,
					RecycleCapacity = 2
				}
			};

			var world = new World(worldConfig);
			var query = world.Query().Include<FlagData>().Build();
			var flags = world.GetPool<FlagData>();

			var entity = world.Create();
			var entity2 = world.Create();

			Assert.That(query.Count, Is.EqualTo(0));

			flags.Add(entity);

			Assert.That(query.Count, Is.EqualTo(1));

			flags.Add(entity2);

			Assert.That(query.Count, Is.EqualTo(2));

			flags.Delete(entity);

			Assert.That(query.Count, Is.EqualTo(1));
		}

		[Test]
		public void QueriesAreReused()
		{
			var worldConfig = new WorldConfig {
				EntityCapacity = 2,
				DataPools = new DataPoolConfig {
					Capacity = 2,
					RecycleCapacity = 2
				}
			};

			var world = new World(worldConfig);
			var queryA = world.Query().Include<FlagData>().Build();
			var queryB = world.Query().Include<FlagData>().Build();

			Assert.That(queryA, Is.EqualTo(queryB));
		}

		[Test]
		public void Serialize()
		{
			const string ID = "TEST_ID";

			var worldConfig = new WorldConfig {
				EntityCapacity = 512,
				DataPools = new DataPoolConfig {
					Capacity = 512,
					RecycleCapacity = 512
				}
			};

			using var memoryStream = new MemoryStream();
			using var outputStream = new TokenOutputStream(memoryStream);
			var serializer = new SuperSerializer();

			var worldA = new World(worldConfig);

			for (var i = 0; i < 1000; i++) {
				var entity = worldA.Create();
				
				worldA.Add(entity, new CountData {Count = i});
				worldA.Add<FlagData>(entity);
			}
			
			var entity1 = worldA.Create();
			var entity2 = worldA.Create();
			
			worldA.Add(entity2, new FriendData { Friend = worldA.GetRef(entity1) });
			worldA.Add(entity2, new MetaData { Id = ID });
			worldA.Add<FlagData>(entity2);
			
			worldA.GetPool<NotUsedData>();
			
			serializer.Write(outputStream, worldA);
			outputStream.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);

			using var inputStream = new TokenInputStream(memoryStream, true);
			var worldB = new World(worldConfig);
			serializer.Read(inputStream, worldB);

			Assert.That(worldB.Entities.Count, Is.EqualTo(worldA.Entities.Count));
			
			Assert.That(worldB.IsAlive(entity1));
			
			ref var entity2MetaData = ref worldB.Get<MetaData>(entity2);
			Assert.That(entity2MetaData.Id, Is.EqualTo(ID));

			worldB.Get<FlagData>(entity2);
			ref var friendData = ref worldB.Get<FriendData>(entity2);
			Assert.IsTrue(worldB.TryGet(friendData.Friend, out var friend));
			Assert.That(friend, Is.EqualTo(entity1));

			var entityC = worldB.Create();
			worldB.Add<MetaData>(entityC);
		}
	}
}