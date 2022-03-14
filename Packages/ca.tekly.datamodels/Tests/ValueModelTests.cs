// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using NSubstitute;
using NUnit.Framework;
using Tekly.Common.Observables;
using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using UnityEditor;

namespace Tekly.DataModels.Tests
{
    public class ValueModelTests
    {
        [SetUp]
        public void SetUp()
        {
            UnityRuntimeEditorUtils.MockPlayModeStart();
        }

        [TearDown]
        public void TearDown()
        {
            UnityRuntimeEditorUtils.MockPlayModeEnd();
        }

        [Test]
        public void TestBoolModelSubscribe()
        {
            const bool testValueA = true;
            const bool testValueB = false;

            var model = new BoolValueModel(false);
            
            var observer = Substitute.For<IValueObserver<bool>>();

            var disposable = model.Subscribe(observer);
            
            observer.Received(1).Changed(model.Value);
            observer.ClearReceivedCalls();

            model.Value = testValueA;

            observer.Received(1).Changed(model.Value);
            observer.ClearReceivedCalls();
            
            disposable.Dispose();

            model.Value = testValueB;
            Assert.That(model.Value, Is.EqualTo(testValueB));
            
            observer.DidNotReceiveWithAnyArgs().Changed(default);
        }
        
        [Test]
        public void TestNumberModelSubscribe()
        {
            const int testValueA = 1;
            const int testValueB = 2;

            var model = new NumberValueModel(0);
            
            var observer = Substitute.For<IValueObserver<double>>();

            var disposable = model.Subscribe(observer);
            
            observer.Received(1).Changed(model.Value);
            observer.ClearReceivedCalls();

            model.Value = testValueA;

            observer.Received(1).Changed(model.Value);
            observer.ClearReceivedCalls();
            
            disposable.Dispose();

            model.Value = testValueB;
            Assert.That(model.Value, Is.EqualTo(testValueB));
            
            observer.DidNotReceiveWithAnyArgs().Changed(default);
        }

        [Test]
        public void TestStringModelSubscribe()
        {
            const string testValueA = "testA";
            const string testValueB = "testB";

            var model = new StringValueModel(null);
            
            var observer = Substitute.For<IValueObserver<string>>();

            var disposable = model.Subscribe(observer);
            
            observer.Received(1).Changed(model.Value);
            observer.ClearReceivedCalls();

            model.Value = testValueA;
            Assert.That(model.Value, Is.EqualTo(testValueA));
            
            observer.Received(1).Changed(model.Value);
            observer.ClearReceivedCalls();
            
            disposable.Dispose();

            model.Value = testValueB;
            Assert.That(model.Value, Is.EqualTo(testValueB));
            
            observer.DidNotReceiveWithAnyArgs().Changed(default);
        }
    }
}