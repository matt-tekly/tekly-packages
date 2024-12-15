using NUnit.Framework;
using AssertionException = UnityEngine.Assertions.AssertionException;

namespace Tekly.Common.Utils
{
	[TestFixture]
	public class RandomSelector64Tests
	{
		[Test]
		public void ValidatesInput()
		{
			Assert.DoesNotThrow(() => new RandomSelector64(1, RandomSelectMode.Random));
			Assert.DoesNotThrow(() => new RandomSelector64(65, RandomSelectMode.Random));

			Assert.DoesNotThrow(() => new RandomSelector64(1, RandomSelectMode.NonRepeating));
			Assert.DoesNotThrow(() => new RandomSelector64(65, RandomSelectMode.NonRepeating));

			Assert.DoesNotThrow(() => new RandomSelector64(1, RandomSelectMode.Exhaustive));
			Assert.DoesNotThrow(() => new RandomSelector64(64, RandomSelectMode.Exhaustive));

			Assert.Throws<AssertionException>(() => new RandomSelector64(0, RandomSelectMode.Random));
			Assert.Throws<AssertionException>(() => new RandomSelector64(0, RandomSelectMode.NonRepeating));
			Assert.Throws<AssertionException>(() => new RandomSelector64(0, RandomSelectMode.Exhaustive));

			Assert.Throws<AssertionException>(() => new RandomSelector64(65, RandomSelectMode.Exhaustive));
		}

		[TestCase(1, 10)]
		[TestCase(64, 1000)]
		[TestCase(128, 1000)]
		public void Select_RandomMode_ReturnsValuesWithinRange(int size, int runs)
		{
			var selector = new RandomSelector64(size, RandomSelectMode.Random);

			for (var i = 0; i < runs; i++) {
				var result = selector.Select();
				Assert.That(result, Is.GreaterThanOrEqualTo(0).And.LessThan(size));
			}
		}

		[TestCase(2)]
		[TestCase(64)]
		[TestCase(128)]
		public void Select_NonRepeatingMode_DoesNotRepeatConsecutively(int size)
		{
			var selector = new RandomSelector64(size, RandomSelectMode.NonRepeating);

			var previous = selector.Select();
			for (var i = 0; i < 100; i++) {
				var next = selector.Select();
				Assert.AreNotEqual(previous, next, "NonRepeating mode should not repeat consecutively.");
				previous = next;
			}
		}

		[Test]
		public void Select_NonRepeatingMode_SizeOneReturns0()
		{
			var selector = new RandomSelector64(1, RandomSelectMode.NonRepeating);

			Assert.That(selector.Select(), Is.EqualTo(0));
			Assert.That(selector.Select(), Is.EqualTo(0));
			Assert.That(selector.Select(), Is.EqualTo(0));
		}

		[TestCase(1)]
		[TestCase(32)]
		[TestCase(64)]
		public void Select_ExhaustiveMode_SelectsAllValuesBeforeRepeating(int size)
		{
			var selector = new RandomSelector64(size, RandomSelectMode.Exhaustive);
			var seen = new bool[size];
			
			for (var i = 0; i < size; i++) {
				var value = selector.Select();
				Assert.That(value, Is.GreaterThanOrEqualTo(0).And.LessThan(size));
				Assert.IsFalse(seen[value], $"Value {value} should not be repeated before all items are selected.");
				seen[value] = true;
			}

			for (var i = 0; i < size; i++) {
				Assert.IsTrue(seen[i], $"Value [{i}] was not selected during the exhaustive round.");
			}
			
			var firstRepeat = selector.Select();
			Assert.That(firstRepeat, Is.GreaterThanOrEqualTo(0).And.LessThan(size));
		}
	}
}