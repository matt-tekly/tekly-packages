using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Injectors.Tests
{
    public class SampleInjectClass
    {
        [Inject] public string StringValue;
        [Inject("String1")] public string String1;
        [Inject("String2")] public string String2;
        
        [Inject] public List<int> Ints;
        
        [Inject] public ITestInterface TestInterface;
        [Inject] public TestClass TestClass;
    }
    
    public interface ITestInterface { }
    public class TestClass : ITestInterface { }
    
    public class SingletonClass { }
        
    public class SingletonConsumer
    {
        public readonly SingletonClass Singleton;

        public SingletonConsumer(SingletonClass singleton)
        {
            Singleton = singleton;
        }
    }
    
    public class LoopClassA
    {
        [Inject] public LoopClassB Looper;
    }
        
    public class LoopClassB
    {
        [Inject] public LoopClassA Looper;
    }
        
    public class LoopClassC
    {
        [Inject] public LoopClassC Looper;
    }

    public class BaseClass
    {
        [Inject] private string m_baseValue;
        public string BaseValue => m_baseValue;
    }
    
    public class SubClass : BaseClass
    {

    }
    
    public class MethodClass
    {
        public int InjectPrivateValue;
        
        [Inject]
        private void InjectPrivate(int value)
        {
            InjectPrivateValue = value;
        }
    }
    
    public class InjectionContainerTests
    {
        [Test]
        public void SimpleInjectorTest()
        {
            const string string1 = "STRING_1";
            const string string2 = "STRING_2";
            
            var ints = new List<int>();
            var testInterface = new TestClass();
            
            var container = new InjectorContainer();
            container.Register("Steve");
            container.Register(string1, "String1");
            container.Register(string2, "String2");
            
            container.Register(ints);
            container.Register<TestClass, ITestInterface>(testInterface);

            var sampleInjectClass = new SampleInjectClass();
            container.Inject(sampleInjectClass);
            
            Assert.That(sampleInjectClass.StringValue, Is.EqualTo("Steve"));
            Assert.That(sampleInjectClass.String1, Is.EqualTo(string1));
            Assert.That(sampleInjectClass.String2, Is.EqualTo(string2));
            
            Assert.That(sampleInjectClass.Ints, Is.EqualTo(ints));
            Assert.That(sampleInjectClass.TestInterface, Is.EqualTo(testInterface));
            Assert.That(sampleInjectClass.TestClass, Is.EqualTo(testInterface));
        }
        
        [Test]
        public void ParentTest()
        {
            const string string1 = "STRING_1";
            const string string2 = "STRING_2";

            var parentContainer = new InjectorContainer();
            var container = new InjectorContainer(parentContainer);
            
            parentContainer.Register(string1);
            
            Assert.That(container.Get<string>(), Is.EqualTo(string1));
            
            container.Register(string2);
            
            Assert.That(container.Get<string>(), Is.EqualTo(string2));
        }

        [Test]
        public void SingletonConstructor()
        {
            var container = new InjectorContainer();
            
            container.Singleton<SingletonClass>();
            container.Factory<SingletonConsumer>();

            var singletonUser = container.Get<SingletonConsumer>();
            var singleton = container.Get<SingletonClass>();
            
            Assert.That(singletonUser, Is.Not.Null);
            Assert.That(singletonUser.Singleton, Is.EqualTo(singleton));
        }

        [Test]
        public void LoopTester()
        {
            Assert.Throws<DependencyCycleException>(() => {
                var container = new InjectorContainer();

                container.Factory<LoopClassA>();
                container.Factory<LoopClassB>();
                container.Get<LoopClassA>();
            });
            
            Assert.Throws<DependencyCycleException>(() => {
                var container = new InjectorContainer();

                container.Factory<LoopClassC>();
                container.Get<LoopClassC>();
            });
        }
        
        [Test]
        public void MethodTester()
        {
            var container = new InjectorContainer();
            var instance = new MethodClass();
            
            container.Register(3);
            container.Inject(instance);
            
            Assert.That(instance.InjectPrivateValue, Is.EqualTo(3));
        }
        
        [Test]
        public void InheritanceTest()
        {
            var container = new InjectorContainer();
            var instance = new SubClass();
            
            container.Register("test");
            container.Inject(instance);
            
            Assert.That(instance.BaseValue, Is.EqualTo("test"));
        }
    }
}