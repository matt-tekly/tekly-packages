// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

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
    }
}