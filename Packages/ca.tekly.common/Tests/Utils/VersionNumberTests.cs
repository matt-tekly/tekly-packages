using NUnit.Framework;

namespace Tekly.Common.Utils
{
	[TestFixture]
	public class VersionNumberTests
	{
		[Test]
		public void Parse()
		{
			const string versionString = "1.2.3";
			VersionNumber.TryParse(versionString, out var version);

			Assert.That(version.Major, Is.EqualTo(1));
			Assert.That(version.Minor, Is.EqualTo(2));
			Assert.That(version.Patch, Is.EqualTo(3));
		}

		[TestCase("1.0.0", "2.0.0", -1)]
		[TestCase("1.2.0", "2.0.0", -1)]
		[TestCase("1.2.3", "2.0.0", -1)]
		[TestCase("1.0.1", "1.0.2", -1)]
		[TestCase("1.2.9", "1.3.2", -1)]
		[TestCase("2.0.0", "1.0.0", 1)]
		[TestCase("1.2.3", "1.2.3", 0)]
		public void CompareOperators(string stringVersionA, string stringVersionB, int comparison)
		{
			VersionNumber.TryParse(stringVersionA, out var versionA);
			VersionNumber.TryParse(stringVersionB, out var versionB);

			Assert.That(versionA.CompareTo(versionB), Is.EqualTo(comparison));

			var result = comparison switch {
				-1 => versionA < versionB,
				1 => versionA > versionB,
				_ => versionA == versionB
			};

			Assert.That(result, Is.True);
		}
	}
}