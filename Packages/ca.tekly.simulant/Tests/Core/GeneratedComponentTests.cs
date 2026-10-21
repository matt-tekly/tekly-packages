using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.Simulant.Core
{
	/// <summary>
	/// A component written by the SuperSerial source generator. If the generator isn't applied to this assembly
	/// this file doesn't compile, since nothing else implements ISuperSerialize for it.
	/// </summary>
	[SuperSerializable]
	internal partial struct GeneratedMeta
	{
		public string Id;
		public int Level;
	}

	public class GeneratedComponentTests
	{
		[Test]
		public void GeneratedComponentRoundTripsThroughAWorld()
		{
			var worldA = new World();
			var entity = worldA.Create();
			worldA.Add(entity, new GeneratedMeta { Id = "GEN", Level = 3 });

			var serializer = new SuperSerializer();
			Tekly.SuperSerial.Generated.SuperSerialRegistry_Tekly_Simulant_Tests.RegisterAll(serializer);

			Assert.IsTrue(SuperSerializeStructs<GeneratedMeta>.IsRegistered);

			using var memory = new MemoryStream();
			using (var output = new TokenOutputStream(memory, leaveOpen: true)) {
				serializer.Write(output, worldA);
			}

			memory.Position = 0;
			var worldB = new World();

			using (var input = new TokenInputStream(memory, true)) {
				serializer.Read(input, worldB);
			}

			ref var meta = ref worldB.Get<GeneratedMeta>(entity);
			Assert.AreEqual("GEN", meta.Id);
			Assert.AreEqual(3, meta.Level);
		}
	}
}
