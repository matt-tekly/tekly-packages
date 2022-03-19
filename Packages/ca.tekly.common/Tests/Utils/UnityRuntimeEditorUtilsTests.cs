using NUnit.Framework.Interfaces;
using Tekly.Common.Utils;
using Tests.Utils;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(UnityRuntimeEditorUtilsTests))]

namespace Tests.Utils
{
    public class UnityRuntimeEditorUtilsTests : ITestRunCallback
    {
        public void RunStarted(ITest testsToRun) { }

        public void RunFinished(ITestResult testResults) { }

        public void TestStarted(ITest test)
        {
            UnityRuntimeEditorUtils.MockPlayModeStart();
        }

        public void TestFinished(ITestResult result)
        {
            UnityRuntimeEditorUtils.MockPlayModeEnd();
        }
    }
}