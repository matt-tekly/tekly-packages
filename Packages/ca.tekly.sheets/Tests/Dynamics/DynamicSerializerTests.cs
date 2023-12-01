using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tekly.Sheets.Dynamics
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

	public class Arrays
	{
		public int[] Ints;
	}
	
	public class Lists
	{
		public List<int> Ints = new List<int>();
	}
	
	public class DynamicSerializerTests
	{
		[Test]
		public void Basic()
		{
			var dataObject = new Dynamic(DynamicType.Object) {
				["Flag"] = true,
				["Integer"] = 5,
				["Vector3"] = new Dynamic(DynamicType.Object) {
					["x"] = 1d,
					["y"] = 2d,
					["z"] = 3d
				}
			};

			var serializer = new DynamicSerializer();
			var result = serializer.Deserialize(typeof(ValueTypes), dataObject, null) as ValueTypes;
			
			Assert.That(result.Flag, Is.True);
			Assert.That(result.Integer, Is.EqualTo(5));
			Assert.That(result.Vector3, Is.EqualTo(new Vector3(1,2,3)));
		}
		
		[Test]
		public void BasicExisting()
		{
			var dataObject = new Dynamic(DynamicType.Object) {
				["Flag"] = true,
				["Integer"] = 5,
				["Vector3"] = new Dynamic(DynamicType.Object) {
					["x"] = 1f,
					["y"] = 2f,
				}
			};

			var existing = new ValueTypes();
			existing.Vector3 = new Vector3(0, 0, 5);
			existing.String = "Test";
			
			var serializer = new DynamicSerializer();
			var result = serializer.Deserialize<ValueTypes>(dataObject, existing);
			
			Assert.That(result.Flag, Is.True);
			Assert.That(result.Integer, Is.EqualTo(5));
			Assert.That(result.Vector3, Is.EqualTo(new Vector3(1,2,5)));
			Assert.That(result.String, Is.EqualTo("Test"));
		}
		
		[Test]
		public void BasicArray()
		{
			var dataObject = new Dynamic(DynamicType.Object) {
				["Ints"] = new Dynamic(DynamicType.Array) {
					[0] = 4,
				}
			};

			var existing = new Arrays();
			existing.Ints = new[] {1, 2, 3};
			
			var serializer = new DynamicSerializer();
			var result = serializer.Deserialize<Arrays>(dataObject, existing);
			
			Assert.That(result.Ints.Length, Is.EqualTo(1));
			Assert.That(result.Ints[0], Is.EqualTo(4));
		}
		
		[Test]
		public void BasicList()
		{
			var dataObject = new Dynamic(DynamicType.Object) {
				["Ints"] = new Dynamic(DynamicType.Array) {
					[0] = 4,
				}
			};

			var existing = new Lists();
			existing.Ints.AddRange(new[] {1, 2, 3});
			
			var serializer = new DynamicSerializer();
			var result = serializer.Deserialize<Lists>(dataObject, existing);
			
			Assert.That(result.Ints.Count, Is.EqualTo(1));
			Assert.That(result.Ints[0], Is.EqualTo(4));
		}
	}
}