using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tekly.Simulant.Core
{
	public class DataPoolTests
	{
		private struct Identity
		{
			public string Id;
		}

		private struct Flag { }

		private struct TestInit : IInit
		{
			public const int INIT_VALUE = 3;

			public int Value;

			public void Init()
			{
				Value = INIT_VALUE;
			}
		}

		private struct TestRecycle : IRecycle
		{
			public const int RECYCLE_VALUE = -1;
			public int Value;

			public void Recycle()
			{
				Value = RECYCLE_VALUE;
			}
		}

		private struct TestAutoRecycle : IAutoRecycle
		{
			public int Value;
		}

		[Test]
		public void AddAndGet()
		{
			var world = new World();
			var pool = world.GetPool<Identity>();

			var entity = world.Create();

			ref var identity = ref pool.Add(entity);
			identity.Id = "Test Id 0";

			var retrievedIdentity = pool.Get(entity);

			Assert.That(identity.Id, Is.EqualTo(retrievedIdentity.Id));
		}

		[Test]
		public void TryGet()
		{
			var world = new World();
			var pool = world.GetPool<Identity>();

			var entity = world.Create();

			ref var identity = ref pool.Add(entity);
			identity.Id = "Test Id 0";

			var exists = pool.TryGet(entity, out var retrievedIdentity);

			Assert.That(exists, Is.True);
			Assert.That(identity.Id, Is.EqualTo(retrievedIdentity.Id));
		}

		[Test]
		public void AddAndDeleteEntity()
		{
			var world = new World();
			var pool = world.GetPool<Identity>();

			var entity = world.Create();

			ref var identity = ref pool.Add(entity);
			identity.Id = "Test Id 0";

			var retrievedIdentity = pool.Get(entity);

			Assert.That(identity.Id, Is.EqualTo(retrievedIdentity.Id));

			world.Delete(entity);

			Assert.That(pool.Count, Is.EqualTo(0));
		}

		[Test]
		public void Init()
		{
			var world = new World();
			var entity = world.Create();

			ref var testReset = ref world.Add<TestInit>(entity);
			Assert.That(testReset.Value, Is.EqualTo(TestInit.INIT_VALUE));
		}

		[Test]
		public void Recycle()
		{
			var world = new World();
			var pool = world.GetPool<TestRecycle>();

			var entity = world.Create();

			ref var testRecycle = ref pool.Add(entity);
			Assert.That(testRecycle.Value, Is.EqualTo(0));

			testRecycle.Value = 1;
			pool.Delete(entity);

			Assert.That(testRecycle.Value, Is.EqualTo(TestRecycle.RECYCLE_VALUE));
		}

		[Test]
		public void AutoRecycle()
		{
			var world = new World();
			var pool = world.GetPool<TestAutoRecycle>();

			var entity = world.Create();

			ref var testAutoRecycle = ref pool.Add(entity);
			Assert.That(testAutoRecycle.Value, Is.EqualTo(0));

			testAutoRecycle.Value = 1;
			pool.Delete(entity);

			Assert.That(testAutoRecycle.Value, Is.EqualTo(0));
		}
	}
}