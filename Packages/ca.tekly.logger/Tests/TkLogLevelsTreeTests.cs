using System.Collections.Generic;
using NUnit.Framework;

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
            var loggersData = new LoggersData {
                Loggers = new List<LoggerSettingsData> {
                    new LoggerSettingsData {Logger = name, Level = level},
                }
            };

            var tree = new LogSettingsTree();
            tree.Initialize(loggersData);

            Assert.That(tree.GetSettings(name).Level, Is.EqualTo(level));
        }

        [Test]
        [TestCase("Test")]
        [TestCase("TestA.TestB")]
        [TestCase("TestA.TestB.TestC")]
        public void DefaultTest(string name)
        {
            var defaultLevel = TkLogLevel.Exception;

            var loggersData = new LoggersData {
                Default = new LoggerSettingsData {
                    Level = defaultLevel
                }
            };

            var tree = new LogSettingsTree();
            tree.Initialize(loggersData);

            Assert.That(tree.GetSettings(name).Level, Is.EqualTo(defaultLevel));
        }

        [Test]
        public void SubGroupTest()
        {
            var defaultLevel = TkLogLevel.Exception;
            var targetLevel = TkLogLevel.Debug;
            var testDLevel = TkLogLevel.Info;

            var loggersData = new LoggersData {
                Default = new LoggerSettingsData {Level = defaultLevel},
                Loggers = new List<LoggerSettingsData> {
                    new LoggerSettingsData {Logger = "TestA.TestB", Level = targetLevel},
                    new LoggerSettingsData {Logger = "TestA.TestB.TestC.TestD", Level = testDLevel},
                }
            };

            var tree = new LogSettingsTree();
            tree.Initialize(loggersData);

            Assert.That(tree.GetSettings("TestA").Level, Is.EqualTo(defaultLevel));
            Assert.That(tree.GetSettings("TestA.TestB").Level, Is.EqualTo(targetLevel));
            Assert.That(tree.GetSettings("TestA.TestB.TestC").Level, Is.EqualTo(targetLevel));
            Assert.That(tree.GetSettings("TestA.TestB.TestC2").Level, Is.EqualTo(targetLevel));
            Assert.That(tree.GetSettings("TestA.TestB.TestC.TestD").Level, Is.EqualTo(testDLevel));
        }
    }
}