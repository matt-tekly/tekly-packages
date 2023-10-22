using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	public class BaseObject : ISuperSerialize
	{
		public string Name;
		public virtual void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			output.Write(Name);
		}

		public virtual void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			Name = input.ReadString();
		}
	}

	public class SubObject : BaseObject
	{
		public int Number;
		public Vector2 Vector2;
		public Vector3 Vector3;
		
		public override void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			base.Write(output, superSerializer);
			output.Write(Number);
			output.Write(Vector2);
			output.Write(Vector3);
		}

		public override void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			base.Read(input, superSerializer);
			Number = input.ReadInt();
			Vector2 = input.ReadVector2();
			input.Read(ref Vector3);
		}
	}

	public class SimpleObject : ISuperSerialize
	{
		public SubObject SubObject;
		public List<int> Numbers = new List<int>();

		public void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			superSerializer.Write(output, SubObject);
			superSerializer.Write(output, Numbers);
		}

		public void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			SubObject = superSerializer.Read(input, SubObject);
			Numbers = superSerializer.Read(input, Numbers);
		}
	}

	public class SuperSerialize
	{
		[Test]
		public void ConverterTest()
		{
			var simpleObject = new SimpleObject {
				SubObject = new SubObject {
					Name = "SUB_A",
					Number = 1111
				},
				Numbers = new List<int>(new []{3,2,1})
			};

			var bytes = new byte[1000];
			var serializer = new SuperSerializer();

			using var memoryStream = new MemoryStream(bytes);
			using var outputStream = new TokenOutputStream(memoryStream);

			memoryStream.Seek(0, SeekOrigin.Begin);

			serializer.Write(outputStream, simpleObject);
			outputStream.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);
			using var inputStream = new TokenInputStream(memoryStream, true);
			var simpleObjectB = serializer.Read<SimpleObject>(inputStream);
			
			Assert.AreEqual(simpleObject.SubObject.Name, simpleObjectB.SubObject.Name);
			Assert.AreEqual(simpleObject.SubObject.Number, simpleObjectB.SubObject.Number);
			
			CollectionAssert.AreEqual(simpleObject.Numbers, simpleObjectB.Numbers);
		}

		[Test]
		public void InheritanceTest()
		{
			var subObjectA = new SubObject {
				Name = "TEST_A",
				Number = 11,
				Vector2 = new Vector3(1, 2),
				Vector3 = new Vector3(1, 2, 3)
			};

			var bytes = new byte[1000];
			var serializer = new SuperSerializer();

			using var memoryStream = new MemoryStream(bytes);
			using var outputStream = new TokenOutputStream(memoryStream);

			memoryStream.Seek(0, SeekOrigin.Begin);

			serializer.Write(outputStream, subObjectA);
			outputStream.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);
			using var inputStream = new TokenInputStream(memoryStream, true);
			var simpleObjectB = serializer.Read<SubObject>(inputStream);

			Assert.AreEqual(subObjectA.Name, simpleObjectB.Name);
			Assert.AreEqual(subObjectA.Number, simpleObjectB.Number);
			Assert.AreEqual(subObjectA.Vector2, simpleObjectB.Vector2);
			Assert.AreEqual(subObjectA.Vector3, simpleObjectB.Vector3);
		}
	}
}