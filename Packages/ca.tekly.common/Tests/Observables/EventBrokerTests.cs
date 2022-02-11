// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using NSubstitute;
using NUnit.Framework;

namespace Tekly.Common.Observables.Tests
{
    public class EventBrokerTests
    {
        private class TestAEvt { }
        
        private class TestBEvt { }
        
        [Test]
        public void EmitTest()
        {
            EventBroker broker = new EventBroker();
            var evt = new TestAEvt();

            broker.Emit(evt);
            
            var handler = Substitute.For<Action<TestAEvt>>();
            var disposable = broker.Subscribe(handler);
            
            broker.Emit(evt);
            broker.Emit(new TestBEvt());
            
            handler.Received(1).Invoke(Arg.Is(evt));
            
            disposable.Dispose();
            
            broker.Emit(evt);
            
            handler.Received(1).Invoke(Arg.Is(evt));
        }
    }
}


