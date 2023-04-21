using NSubstitute;
using NUnit.Framework;

namespace Tekly.Common.Observables
{
    public class ObservableValueTests
    {
        [Test]
        public void SubscribeToValueTest()
        {
            var observable = new ObservableValue<int>(1);
            var valueObserver = Substitute.For<IValueObserver<int>>();

            var disposable = observable.Subscribe(valueObserver);
            
            valueObserver.Received(1).Changed(1);

            observable.Value = 2;
            valueObserver.Received(1).Changed(2);
            
            disposable.Dispose();
            
            observable.Value = 3;
            valueObserver.Received(0).Changed(3);
        }
    }
}