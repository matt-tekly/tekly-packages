using System;
using NUnit.Framework;

namespace Tekly.Injectors.Tests
{
	/// <summary>
	/// A failure anywhere in a graph names what was missing and every member that was waiting on it.
	/// </summary>
	public class InjectionErrorTests
	{
		public class NeedsString
		{
			[Inject] private string m_value;
		}

		public class NeedsNamedString
		{
			[Inject("missing")] private string m_value;
		}

		public class NeedsStringByMethod
		{
			[Inject]
			private void Init(int fine, string value) { }
		}

		public class Outer
		{
			public Outer(NeedsString inner) { }
		}

		public class Throws
		{
			public Throws()
			{
				throw new InvalidOperationException("boom");
			}
		}

		public class NoPublicConstructor
		{
			private NoPublicConstructor() { }
		}

		private InjectorContainer m_container;

		[SetUp]
		public void SetUp()
		{
			m_container = new InjectorContainer("Test");
			m_container.Register(3);
		}

		[Test]
		public void MissingFieldDependencyNamesTheField()
		{
			var exception = Assert.Throws<InjectionException>(() => m_container.Inject(new NeedsString()));

			Assert.That(exception.Message, Does.StartWith("No instance of [System.String] registered in container [Test]"));
			Assert.That(exception.Path, Is.EqualTo(new[] { "NeedsString.m_value" }));
		}

		[Test]
		public void MissingIdSaysSo()
		{
			var exception = Assert.Throws<InjectionException>(() => m_container.Inject(new NeedsNamedString()));

			Assert.That(exception.Message, Does.Contain("with id [missing]"));
			Assert.That(exception.Path, Is.EqualTo(new[] { "NeedsNamedString.m_value" }));
		}

		[Test]
		public void MissingMethodParameterNamesMethodAndParameter()
		{
			var exception = Assert.Throws<InjectionException>(() => m_container.Inject(new NeedsStringByMethod()));

			Assert.That(exception.Path, Is.EqualTo(new[] { "NeedsStringByMethod.Init(value)" }));
		}

		[Test]
		public void ChainRunsThroughFactoriesAndConstructors()
		{
			m_container.Factory<NeedsString>();
			m_container.Factory<Outer>();

			var exception = Assert.Throws<InjectionException>(() => m_container.Get<Outer>());

			Assert.That(exception.Path, Is.EqualTo(new[] { "NeedsString.m_value", "new Outer(inner)" }));
			Assert.That(exception.Message, Is.EqualTo(
				"No instance of [System.String] registered in container [Test]\n" +
				"  needed by NeedsString.m_value\n" +
				"  needed by new Outer(inner)"));
		}

		[Test]
		public void CycleKeepsItsTypeAndGainsTheChain()
		{
			m_container.Factory<LoopClassA>();
			m_container.Factory<LoopClassB>();

			var exception = Assert.Throws<DependencyCycleException>(() => m_container.Get<LoopClassA>());

			Assert.That(exception.Path, Is.EqualTo(new[] { "LoopClassB.Looper", "LoopClassA.Looper" }));
		}

		[Test]
		public void ExceptionFromConstructorIsWrappedWithItsName()
		{
			m_container.Factory<Throws>();

			var exception = Assert.Throws<InjectionException>(() => m_container.Get<Throws>());

			Assert.That(exception.Message, Is.EqualTo("new Throws threw InvalidOperationException: boom"));
			Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void NoPublicConstructorIsExplained()
		{
			m_container.Factory<NoPublicConstructor>();

			var exception = Assert.Throws<InjectionException>(() => m_container.Get<NoPublicConstructor>());

			Assert.That(exception.Message, Does.Contain("has no public constructor"));
		}
	}
}
