using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Logging.Tests
{
    public class TkLogLevelsTreeTests
    {
        [Test]
        [TestCase("*", TkLogLevel.Exception)]
        [TestCase("Test", TkLogLevel.Exception)]
        [TestCase("TestA.TestB", TkLogLevel.Exception)]
        [TestCase("TestA.TestB.TestC", TkLogLevel.Exception)]
        public void SimpleTest(string name, TkLogLevel level)
        {
            TkLogLevelsTree tree = new TkLogLevelsTree();
            
            tree.Initialize(new List<LoggerLevelPair> {
                new LoggerLevelPair {Logger = name, Level = level},
            });
            
            Assert.That(tree.GetLevel(name), Is.EqualTo(level));
        }
        
        [Test]
        [TestCase("Test")]
        [TestCase("TestA.TestB")]
        [TestCase("TestA.TestB.TestC")]
        public void DefaultTest(string name)
        {
            TkLogLevel defaultLevel = TkLogLevel.Exception;
            
            TkLogLevelsTree tree = new TkLogLevelsTree();
            
            tree.Initialize(new List<LoggerLevelPair> {
                new LoggerLevelPair {Logger = "*", Level = defaultLevel},
            });
            
            Assert.That(tree.GetLevel(name), Is.EqualTo(defaultLevel));
        }
        
        [Test]
        public void SubGroupTest()
        {
            TkLogLevel defaultLevel = TkLogLevel.Exception;
            TkLogLevel targetLevel = TkLogLevel.Trace;
            TkLogLevel testDLevel = TkLogLevel.Info;
            
            TkLogLevelsTree tree = new TkLogLevelsTree();
            
            tree.Initialize(new List<LoggerLevelPair> {
                new LoggerLevelPair {Logger = "*", Level = defaultLevel},
                new LoggerLevelPair {Logger = "TestA.TestB", Level = targetLevel},
                new LoggerLevelPair {Logger = "TestA.TestB.TestC.TestD", Level = testDLevel},
            });
            
            Assert.That(tree.GetLevel("TestA"), Is.EqualTo(defaultLevel));
            Assert.That(tree.GetLevel("TestA.TestB"), Is.EqualTo(targetLevel));
            Assert.That(tree.GetLevel("TestA.TestB.TestC"), Is.EqualTo(targetLevel));
            Assert.That(tree.GetLevel("TestA.TestB.TestC2"), Is.EqualTo(targetLevel));
            Assert.That(tree.GetLevel("TestA.TestB.TestC.TestD"), Is.EqualTo(testDLevel));
        }
    }
}

