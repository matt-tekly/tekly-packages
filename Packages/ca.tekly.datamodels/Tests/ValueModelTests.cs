// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using NSubstitute;
using NUnit.Framework;
using Tekly.Common.Observables;
using Tekly.DataModels.Models;

namespace Tekly.DataModels.Tests
{
    public class ValueModelTests
    {
        [Test]
        public void TestBoolModelSubscribe()
        {
            const bool testValueA = true;
            const bool testValueB = false;

            var model = new BoolValueModel(false);
            
            var observer = Substitute.For<IValueObserver<BasicValueModel>>();

            var disposable = model.Subscribe(observer);
            
            observer.Received(1).Changed(model);
            observer.ClearReceivedCalls();

            model.AsBool = testValueA;
            Assert.That(model.AsObject, Is.EqualTo(testValueA));
            
            observer.Received(1).Changed(model);
            observer.ClearReceivedCalls();
            
            disposable.Dispose();

            model.AsBool = testValueB;
            Assert.That(model.AsBool, Is.EqualTo(testValueB));
            
            observer.DidNotReceiveWithAnyArgs().Changed(default);
        }
        
        [Test]
        public void TestNumberModelSubscribe()
        {
            const int testValueA = 1;
            const int testValueB = 2;

            var model = new NumberValueModel(0);
            
            var observer = Substitute.For<IValueObserver<BasicValueModel>>();

            var disposable = model.Subscribe(observer);
            
            observer.Received(1).Changed(model);
            observer.ClearReceivedCalls();

            model.AsDouble = testValueA;
            Assert.That(model.AsObject, Is.EqualTo(testValueA));
            
            observer.Received(1).Changed(model);
            observer.ClearReceivedCalls();
            
            disposable.Dispose();

            model.AsDouble = testValueB;
            Assert.That(model.AsDouble, Is.EqualTo(testValueB));
            
            observer.DidNotReceiveWithAnyArgs().Changed(default);
        }

        [Test]
        public void TestStringModelSubscribe()
        {
            const string testValueA = "testA";
            const string testValueB = "testB";

            var model = new StringValueModel(null);
            
            var observer = Substitute.For<IValueObserver<BasicValueModel>>();

            var disposable = model.Subscribe(observer);
            
            observer.Received(1).Changed(model);
            observer.ClearReceivedCalls();

            model.AsString = testValueA;
            Assert.That(model.AsString, Is.EqualTo(testValueA));
            
            observer.Received(1).Changed(model);
            observer.ClearReceivedCalls();
            
            disposable.Dispose();

            model.AsString = testValueB;
            Assert.That(model.AsString, Is.EqualTo(testValueB));
            
            observer.DidNotReceiveWithAnyArgs().Changed(default);
        }
    }
}