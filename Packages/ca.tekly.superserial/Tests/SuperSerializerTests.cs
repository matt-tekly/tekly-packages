using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Tests
{
	public class ValueTypes : IEquatable<ValueTypes>
	{
		public bool Flag;

		public int Integer;
		public long Long;

		public float Float;
		public double Double;

		public string String;

		public Vector2 Vector2;
		public Vector3 Vector3;
		public Quaternion Quaternion;
		public DateTime Date;

		public bool Equals(ValueTypes other)
		{
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return Flag == other.Flag 
			       && Integer == other.Integer 
			       && Long == other.Long 
			       && Float.Equals(other.Float) 
			       && Double.Equals(other.Double) 
			       && String == other.String 
			       && Vector2.Equals(other.Vector2) 
			       && Vector3.Equals(other.Vector3)
			       && Quaternion.Equals(other.Quaternion)
			       && Date.Equals(other.Date);
		}
	}

	public class SuperSerializerTests
	{
		private ValueTypes CreateValueTypes() => new ValueTypes {
			Flag = true,
			Integer = 123,
			Long = 9999,
			Float = 3.1415f,
			Double = 1.2345d,
			String = "STRING VALUE",
			Vector2 = new Vector2(1, 2),
			Vector3 = new Vector3(1, 2, 3),
			Quaternion = new Quaternion(0,1,2,3),
			Date = DateTime.Now
		};
		
		[Test]
		public void ValuesTypeTest()
		{
			var valuesA = CreateValueTypes();
			
			var bytes = new byte[1000];
			var serializer = new SuperSerializer();

			using var memoryStream = new MemoryStream(bytes);
			using var outputStream = new TokenOutputStream(memoryStream);

			memoryStream.Seek(0, SeekOrigin.Begin);

			serializer.Write(outputStream, valuesA);
			outputStream.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);
			using var inputStream = new TokenInputStream(memoryStream, true);
			var valuesB = serializer.Read<ValueTypes>(inputStream);

			Assert.That(valuesA, Is.EqualTo(valuesB));
		}
		
		[Test]
		public void FileTest()
		{
			var filePath = Path.GetTempFileName();
			try {
				var valuesA = CreateValueTypes();
			
				var serializer = new SuperSerializer();
				
				serializer.Write(filePath, valuesA);
			
				var valuesB = serializer.Read<ValueTypes>(filePath);
				
				Assert.That(valuesA, Is.EqualTo(valuesB));
			} finally {
				File.Delete(filePath);
			}
		}
		
		[Test]
		public void ListBoolTest()
		{
			ListTest(true, false, true, true, false);
		}
		
		[Test]
		public void ListIntTest()
		{
			ListTest(3, 2, 1);
		}
		
		[Test]
		public void ListValueTypes()
		{
			ListTest(CreateValueTypes(), CreateValueTypes(), CreateValueTypes(), CreateValueTypes());
		}
		
		[Test]
		public void ArrayBoolTest()
		{
			ArrayTest(true, false, true, true, false);
		}
		
		[Test]
		public void ArrayIntTest()
		{
			ArrayTest(3, 2, 1);
		}
		
		[Test]
		public void ArrayValueTypes()
		{
			ArrayTest(CreateValueTypes(), CreateValueTypes(), CreateValueTypes(), CreateValueTypes());
		}

		private static void ListTest<T>(params T[] values)
		{
			var listA = new List<T>(values);
			
			var bytes = new byte[1000];
			var serializer = new SuperSerializer();

			using var memoryStream = new MemoryStream(bytes);
			using var outputStream = new TokenOutputStream(memoryStream);

			memoryStream.Seek(0, SeekOrigin.Begin);

			serializer.Write(outputStream, listA);
			outputStream.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);
			using var inputStream = new TokenInputStream(memoryStream, true);
			var listB = serializer.Read<List<T>>(inputStream);

			CollectionAssert.AreEqual(listA, listB);
		}
		
		private static void ArrayTest<T>(params T[] values)
		{
			var bytes = new byte[1000];
			var serializer = new SuperSerializer();

			using var memoryStream = new MemoryStream(bytes);
			using var outputStream = new TokenOutputStream(memoryStream);

			memoryStream.Seek(0, SeekOrigin.Begin);

			serializer.Write(outputStream, values);
			outputStream.Flush();

			memoryStream.Seek(0, SeekOrigin.Begin);
			using var inputStream = new TokenInputStream(memoryStream, true);
			var listB = serializer.Read<List<T>>(inputStream);

			CollectionAssert.AreEqual(values, listB);
		}
	}
}