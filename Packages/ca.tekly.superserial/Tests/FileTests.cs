using System;
using System.IO;
using NUnit.Framework;
using Tekly.SuperSerial.Serialization;

namespace Tekly.SuperSerial.Tests
{
	public class FileTests
	{
		private string m_path;

		[SetUp]
		public void SetUp()
		{
			m_path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ss");
		}

		[TearDown]
		public void TearDown()
		{
			File.Delete(m_path);
			File.Delete(m_path + ".tmp");
		}

		[Test]
		public void RoundTripsThroughAFile()
		{
			var serializer = new SuperSerializer();
			var expected = ValueTypes.Create();

			serializer.Write(m_path, expected);

			Assert.AreEqual(expected, serializer.Read<ValueTypes>(m_path));
		}

		[Test]
		public void OverwritingWithLessDataTruncates()
		{
			var serializer = new SuperSerializer();

			serializer.Write(m_path, new Pair(1, new string('o', 10_000)));
			var bigLength = new FileInfo(m_path).Length;

			serializer.Write(m_path, new Pair(2, "small"));

			Assert.Less(new FileInfo(m_path).Length, bigLength);
			Assert.AreEqual(new Pair(2, "small"), serializer.Read<Pair>(m_path));
		}

		[Test]
		public void LeavesNoTemporaryFile()
		{
			new SuperSerializer().Write(m_path, new Pair(1, "a"));
			Assert.IsFalse(File.Exists(m_path + ".tmp"));
		}

		[Test]
		public void MissingFileThrows()
		{
			Assert.Throws<FileNotFoundException>(() => new SuperSerializer().Read<Pair>(m_path));
		}

		[Test]
		public void FileThatIsNotSuperSerialThrows()
		{
			File.WriteAllText(m_path, "definitely not a save file");
			Assert.Throws<SuperSerialException>(() => new SuperSerializer().Read<Pair>(m_path));
		}
	}
}
