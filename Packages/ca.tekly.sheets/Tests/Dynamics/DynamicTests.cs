using NUnit.Framework;

namespace Tekly.Sheets.Dynamics
{
	public class DynamicTests
	{
		[Test]
		public void SetAndGetPath()
		{
			SetAndGetPath<int>(5);
			SetAndGetPath<int>(9999);
			SetAndGetPath<string>("test");
		}

		private void SetAndGetPath<T>(object targetValue)
		{
			var path = new object[] {"a", 2, "b"};
			var dynObj = new Dynamic(DynamicType.Object);
			dynObj.Set(path, targetValue);

			var dynArr = dynObj["a"] as Dynamic;
			var dynObj2 = dynArr[2] as Dynamic;
			
			Assert.That(dynObj2["b"], Is.EqualTo(targetValue));

			var found = dynObj.TryGet<T>(path, out var result);
			Assert.That(found, Is.True);
			Assert.That(result, Is.EqualTo(targetValue));
		}
	}
}