## EventBroker
```csharp
var broker = new EventBroker();
            
// Subscribing returns a disposable which will unsubscribe from the event
var disposableA = broker.Subscribe((TestAEvt evt) => {
    Debug.Log("TestAEvt has been logged");
});

var disposableB = broker.Subscribe((TestBEvt evt) => {
    Debug.Log("TestBEvt has been logged");
});

broker.Emit(new TestAEvt()); // Log is emitted
broker.Emit(new TestBEvt());

disposableA.Dispose();
broker.Emit(new TestAEvt()); // No log is emitted
```