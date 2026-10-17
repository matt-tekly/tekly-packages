using System.Collections.Generic;
using NUnit.Framework;

namespace Tekly.Injectors.Tests
{
	/// <summary>
	/// An object's injector is one level per type in its hierarchy. A type with a nested __Injector uses it for
	/// its own members; the others use reflection. Every mix of the two must inject everything, base first.
	/// </summary>
	public class InjectorLevelTests
	{
		public static readonly List<string> Calls = new List<string>();

		// Hand-written stand-in for what a generator would emit: it has the base's private field to itself
		public partial class GeneratedBase
		{
			[Inject] private string m_value;
			public string Value => m_value;

			private sealed class __Injector : IInjector
			{
				public void Inject(object instance, InjectorContainer container)
				{
					Calls.Add("base");
					((GeneratedBase) instance).m_value = container.Get<string>() + " (generated)";
				}

				public void Clear(object instance)
				{
					Calls.Add("clear base");
					((GeneratedBase) instance).m_value = null;
				}
			}
		}

		// Plain class under a generated base: its own level is reflection
		public class ReflectedDerived : GeneratedBase
		{
			[Inject] private int m_number;
			public int Number => m_number;

			[Inject]
			private void Ready()
			{
				Calls.Add("derived");
			}
		}

		// Generated class under a reflected base
		public class ReflectedBase
		{
			[Inject] private int m_number;
			public int Number => m_number;
		}

		public partial class GeneratedDerived : ReflectedBase
		{
			[Inject] private string m_value;
			public string Value => m_value;

			private sealed class __Injector : IInjector
			{
				public void Inject(object instance, InjectorContainer container)
				{
					((GeneratedDerived) instance).m_value = container.Get<string>();
				}

				public void Clear(object instance)
				{
					((GeneratedDerived) instance).m_value = null;
				}
			}
		}

		// Nothing of its own: inherits the generated base's level only
		public class Plain : GeneratedBase { }

		private InjectorContainer m_container;

		[SetUp]
		public void SetUp()
		{
			Calls.Clear();
			m_container = new InjectorContainer();
			m_container.Register("test");
			m_container.Register(7);
		}

		[Test]
		public void GeneratedLevelIsUsedInsteadOfReflection()
		{
			var instance = new GeneratedBase();
			m_container.Inject(instance);

			Assert.That(instance.Value, Is.EqualTo("test (generated)"));
			Assert.That(Calls, Is.EqualTo(new[] { "base" }));
		}

		[Test]
		public void ReflectedDerivedOverGeneratedBaseInjectsBothBaseFirst()
		{
			var instance = new ReflectedDerived();
			m_container.Inject(instance);

			Assert.That(instance.Value, Is.EqualTo("test (generated)"));
			Assert.That(instance.Number, Is.EqualTo(7));
			Assert.That(Calls, Is.EqualTo(new[] { "base", "derived" }));
		}

		[Test]
		public void GeneratedDerivedOverReflectedBaseInjectsBoth()
		{
			var instance = new GeneratedDerived();
			m_container.Inject(instance);

			Assert.That(instance.Value, Is.EqualTo("test"));
			Assert.That(instance.Number, Is.EqualTo(7));
		}

		[Test]
		public void BaseLevelRunsOnceForTypeWithNothingOfItsOwn()
		{
			var instance = new Plain();
			m_container.Inject(instance);

			Assert.That(instance.Value, Is.EqualTo("test (generated)"));
			Assert.That(Calls, Is.EqualTo(new[] { "base" }));
		}

		[Test]
		public void ClearRunsEveryLevelDerivedFirst()
		{
			var instance = new ReflectedDerived();
			m_container.Inject(instance);
			Calls.Clear();

			InjectorContainer.Clear(instance);

			Assert.That(instance.Value, Is.Null);
			Assert.That(instance.Number, Is.EqualTo(0));
			Assert.That(Calls, Is.EqualTo(new[] { "clear base" }));
		}

		[Test]
		public void InstantiateInjectsEveryLevel()
		{
			m_container.Factory<ReflectedDerived>();
			var instance = m_container.Get<ReflectedDerived>();

			Assert.That(instance.Value, Is.EqualTo("test (generated)"));
			Assert.That(instance.Number, Is.EqualTo(7));
			Assert.That(Calls, Is.EqualTo(new[] { "base", "derived" }));
		}
	}
}
