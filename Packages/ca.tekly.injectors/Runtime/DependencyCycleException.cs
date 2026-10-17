using System;

namespace Tekly.Injectors
{
	/// <summary>
	/// Thrown when instantiating a type ends up needing an instance of itself. The message's chain shows the loop.
	/// </summary>
	public class DependencyCycleException : InjectionException
	{
		public DependencyCycleException(Type type) : base("Type dependency cycle detected in " + type.FullName) { }
	}
}
