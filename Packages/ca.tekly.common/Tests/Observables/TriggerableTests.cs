using NSubstitute;
using NUnit.Framework;

namespace Tekly.Common.Observables
{
    [TestFixture]
    public class TriggerableTests
    {
        [Test]
        public void ValueObserverTest()
        {
            var triggerable = new Triggerable<int>();
            
            var valueObserverA = Substitute.For<IValueObserver<int>>();
            var valueObserverB = Substitute.For<IValueObserver<int>>();
            
            var disposableA = triggerable.Subscribe(valueObserverA);
            var disposableB = triggerable.Subscribe(valueObserverB);
            
            triggerable.Emit(1);
            valueObserverA.Received(1).Changed(1);
            valueObserverB.Received(1).Changed(1);
            
            triggerable.Emit(2);
            valueObserverA.Received(1).Changed(2);
            valueObserverB.Received(1).Changed(2);
            
            valueObserverA.ClearReceivedCalls();
            valueObserverB.ClearReceivedCalls();
            
            disposableA.Dispose();
            
            triggerable.Emit(1);
            valueObserverA.DidNotReceiveWithAnyArgs().Changed(default);
            valueObserverB.Received(1).Changed(1);
            
            valueObserverB.ClearReceivedCalls();
            
            disposableB.Dispose();
            
            triggerable.Emit(1);
            valueObserverA.DidNotReceiveWithAnyArgs().Changed(default);
            valueObserverB.DidNotReceiveWithAnyArgs().Changed(default);
        }
    }
}