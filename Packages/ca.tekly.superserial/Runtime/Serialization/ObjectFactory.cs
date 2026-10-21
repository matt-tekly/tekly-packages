using System;
using System.Runtime.CompilerServices;

namespace Tekly.SuperSerial.Serialization
{
	public class ObjectFactory
	{
		/// <summary>
		/// When true, objects are created through their parameterless constructor so field initializers run.
		/// When false (the default) they are created uninitialized, which is several times faster, but fields
		/// that aren't in the data are left at their zero value.
		/// </summary>
		public bool CallConstructors;

		public virtual object CreateInstance(Type type)
		{
			if (type.IsAbstract || type.IsInterface) {
				throw new SuperSerialException($"Cannot create an instance of [{type}]. The data holds no concrete type, or that type is not registered with the serializer.");
			}

			if (CallConstructors) {
				try {
					return Activator.CreateInstance(type, true);
				} catch (MissingMethodException) {
					// No parameterless constructor. Fall through.
				}
			}

			return RuntimeHelpers.GetUninitializedObject(type);
		}
	}
}
