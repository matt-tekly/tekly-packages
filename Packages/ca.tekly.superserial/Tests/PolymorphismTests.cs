using System.Collections.Generic;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;

namespace Tekly.SuperSerial.Tests
{
	public class PolymorphismTests
	{
		[Test]
		public void SubclassInABaseTypedField()
		{
			var actual = Serial.RoundTripFresh(new Drawing { Main = new Square { Id = 1, Side = 2 } }, Register);

			Assert.IsInstanceOf<Square>(actual.Main);
			Assert.AreEqual(1, actual.Main.Id);
			Assert.AreEqual(2, ((Square) actual.Main).Side);
		}

		[Test]
		public void SubclassesInAList()
		{
			var expected = new Drawing {
				Shapes = new List<Shape> { new Circle { Id = 2, Radius = 3 }, null, new Square { Id = 3, Side = 4 } },
			};

			var actual = Serial.RoundTripFresh(expected, Register);

			Assert.AreEqual(3, actual.Shapes.Count);
			Assert.AreEqual(3, ((Circle) actual.Shapes[0]).Radius);
			Assert.IsNull(actual.Shapes[1]);
			Assert.AreEqual(4, ((Square) actual.Shapes[2]).Side);
		}

		[Test]
		public void SubclassOfAnISuperSerializeBase()
		{
			var expected = new Holder {
				Base = new SubObject { Name = "sub", Number = 4 },
				Bases = new List<BaseObject> { new BaseObject { Name = "base" }, new SubObject { Name = "sub2" } },
			};

			var actual = Serial.RoundTripFresh(expected, reader => reader.RegisterType<SubObject>());

			Assert.IsInstanceOf<SubObject>(actual.Base);
			Assert.AreEqual(4, ((SubObject) actual.Base).Number);
			Assert.AreEqual("base", actual.Bases[0].Name);
			Assert.IsInstanceOf<SubObject>(actual.Bases[1]);
		}

		[Test]
		public void ExactDeclaredTypeNeedsNoRegistration()
		{
			var actual = Serial.RoundTripFresh(new Holder { Circle = new Circle { Radius = 5 } });
			Assert.AreEqual(5, actual.Circle.Radius);
		}

		[Test]
		public void BoxedPrimitiveInAnObjectField()
		{
			var actual = Serial.RoundTripFresh(new Holder { Boxed = 12 });
			Assert.AreEqual(12, actual.Boxed);
		}

		[Test]
		public void BoxedStringInAnObjectField()
		{
			var actual = Serial.RoundTripFresh(new Holder { Boxed = "text" });
			Assert.AreEqual("text", actual.Boxed);
		}

		[Test]
		public void UnregisteredSubclassThrowsOnRead()
		{
			using var memory = Serial.Write(new Drawing { Main = new Circle { Id = 1 } });

			var exception = Assert.Throws<SuperSerialException>(() => Serial.Read<Drawing>(memory, new SuperSerializer()));

			StringAssert.Contains("Unknown type hash", exception.Message);
			StringAssert.Contains(nameof(Shape), exception.Message);
		}

		[Test]
		public void SerialNameOnATypePinsItsHash()
		{
			var serializer = new SuperSerializer();
			var pinned = XXHash.Hash32("Tests.Square");

			Assert.AreEqual(pinned, serializer.Settings.Converters.RegisterType(typeof(Square)));
			Assert.AreNotEqual(XXHash.Hash32(typeof(Square).FullName), pinned);
		}

		[Test]
		public void ExistingValueOfTheWrongTypeIsReplaced()
		{
			var existing = new Drawing { Main = new Circle { Id = 9 } };
			using var memory = Serial.Write(new Drawing { Main = new Square { Id = 1 } });

			var serializer = new SuperSerializer();
			Register(serializer);
			var actual = Serial.Read(memory, serializer, existing);

			Assert.IsInstanceOf<Square>(actual.Main);
			Assert.AreEqual(1, actual.Main.Id);
		}

		[Test]
		public void ExistingValueOfTheRightTypeIsReused()
		{
			var existing = new Drawing { Main = new Square { Id = 9 } };
			var main = existing.Main;
			using var memory = Serial.Write(new Drawing { Main = new Square { Id = 1 } });

			var serializer = new SuperSerializer();
			Register(serializer);
			var actual = Serial.Read(memory, serializer, existing);

			Assert.AreSame(main, actual.Main);
			Assert.AreEqual(1, actual.Main.Id);
		}

		private static void Register(SuperSerializer serializer)
		{
			serializer.RegisterType<Circle>();
			serializer.RegisterType<Square>();
		}

		private class Drawing
		{
			public Shape Main;
			public List<Shape> Shapes;
		}

		private class Holder
		{
			public BaseObject Base;
			public List<BaseObject> Bases;
			public Circle Circle;
			public object Boxed;
		}
	}
}
