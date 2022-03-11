using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tekly.Logging.Tests
{
    public class TkLogMessageTests : IPostBuildCleanup
    {
        [Test]
        public void TestPrint()
        {
            TkLogger.SetValue("g_user", "Mr User");
            var logParams = TkLogParam.Create(("a", 1), ("b", 2), ("c", "3"));
            var logMessage = new TkLogMessage(TkLogLevel.Info, "Test", "Test", "Test {a} {b} {c} {g_user}", StackTraceUtility.ExtractStackTrace(), logParams);
            
            var sb = new StringBuilder();
            logMessage.Print(sb);
            Assert.That(sb.ToString(), Is.EqualTo("Test 1 2 3 Mr User"));
        }

        public void Cleanup()
        {
            TkLogger.Reset();
        }
    }
}

