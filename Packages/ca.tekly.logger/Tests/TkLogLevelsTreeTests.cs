using System;
using NUnit.Framework;
using Tekly.Logging.Configurations;

namespace Tekly.Logging.Tests
{
	public class TkLogLevelsTreeTests
	{
		[Test]
		[TestCase("Test", TkLogLevel.Exception)]
		[TestCase("TestA.TestB", TkLogLevel.Exception)]
		[TestCase("TestA.TestB.TestC", TkLogLevel.Exception)]
		public void SimpleTest(string name, TkLogLevel level)
		{
			var loggerConfigs = new[] {
				new LoggerConfig {
					Logger = name,
					Level = level
				}
			};

			var tree = new LogSettingsTree();
			tree.Initialize(TkLogLevel.Debug, (ILogDestination)null, loggerConfigs);

			Assert.That(tree.GetSettings(name).Level, Is.EqualTo(level));
		}

		[Test]
		[TestCase("Test")]
		[TestCase("TestA.TestB")]
		[TestCase("TestA.TestB.TestC")]
		public void DefaultTest(string name)
		{
			var defaultLevel = TkLogLevel.Exception;

			var tree = new LogSettingsTree();
			tree.Initialize(defaultLevel, (ILogDestination)null, Array.Empty<LoggerConfig>());

			Assert.That(tree.GetSettings(name).Level, Is.EqualTo(defaultLevel));
		}

		[Test]
		public void SubGroupTest()
		{
			var defaultLevel = TkLogLevel.Exception;
			var targetLevel = TkLogLevel.Debug;
			var testDLevel = TkLogLevel.Info;

			var loggerConfigs = new[] {
				new LoggerConfig {
					Logger = "TestA.TestB",
					Level = targetLevel
				},
				new LoggerConfig {
					Logger = "TestA.TestB.TestC.TestD",
					Level = testDLevel
				}
				
			};

			var tree = new LogSettingsTree();
			tree.Initialize(defaultLevel, (ILogDestination)null, loggerConfigs);

			Assert.That(tree.GetSettings("TestA").Level, Is.EqualTo(defaultLevel));
			Assert.That(tree.GetSettings("TestA.TestB").Level, Is.EqualTo(targetLevel));
			Assert.That(tree.GetSettings("TestA.TestB.TestC").Level, Is.EqualTo(targetLevel));
			Assert.That(tree.GetSettings("TestA.TestB.TestC2").Level, Is.EqualTo(targetLevel));
			Assert.That(tree.GetSettings("TestA.TestB.TestC.TestD").Level, Is.EqualTo(testDLevel));
		}
	}
}