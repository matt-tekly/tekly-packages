using System;
using NUnit.Framework;
using UnityEngine;

namespace Tekly.Common.Utils.PropertyBags
{
	[TestFixture]
	public class PropertyBagTests
	{
		[Test]
		public void GetValue()
		{
			var bag = new PropertyBag();

			var numberValue = bag.GetValue("a", 5);
			Assert.That(numberValue, Is.EqualTo(5));

			var stringValue = bag.GetValue("a", "test");
			Assert.That(stringValue, Is.EqualTo("test"));

			var boolValue = bag.GetValue("a", true);
			Assert.That(boolValue, Is.EqualTo(true));

			var now = DateTime.Now;
			var dateValue = bag.GetValue("a", now);
			Assert.That(dateValue, Is.EqualTo(now));
		}

		[Test]
		public void SetValue()
		{
			var bag = new PropertyBag();

			bag.SetValue("a", 5);
			Assert.That(bag.GetValue("a", 0), Is.EqualTo(5));

			bag.SetValue("a", "test");
			Assert.That(bag.GetValue("a", null), Is.EqualTo("test"));

			bag.SetValue("a", true);
			Assert.That(bag.GetValue("a", false), Is.EqualTo(true));

			var now = DateTime.Now;
			bag.SetValue("a", now);
			Assert.That(bag.GetValue("a", default(DateTime)), Is.EqualTo(now));
		}

		[Test]
		public void Modified()
		{
			var bag = new PropertyBag();

			var modifiedCount = 0;
			bag.Modified.Subscribe(_ => modifiedCount++);

			bag.SetValue("a", 5);
			Assert.That(modifiedCount, Is.EqualTo(1));

			bag.SetValue("a", 5);
			Assert.That(modifiedCount, Is.EqualTo(1));

			bag.SetValue("a", 6);
			Assert.That(modifiedCount, Is.EqualTo(2));
		}

		[Test]
		public void Serialize()
		{
			var bag = new PropertyBag();

			var now = DateTime.Now;
			now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond)); // Strip sub-seconds from the time because serialize doesn't support it.

			bag.SetValue("a", 5);
			bag.SetValue("a", "test");
			bag.SetValue("a", true);
			bag.SetValue("a", now);

			var json = JsonUtility.ToJson(bag);

			var newBag = JsonUtility.FromJson<PropertyBag>(json);

			Assert.That(newBag.GetValue("a", default(double)), Is.EqualTo(5));
			Assert.That(newBag.GetValue("a", default(string)), Is.EqualTo("test"));
			Assert.That(newBag.GetValue("a", default(bool)), Is.EqualTo(true));
			Assert.That(newBag.GetValue("a", default(DateTime)), Is.EqualTo(now));
		}
	}
}