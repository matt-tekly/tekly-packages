using System;
using System.Runtime.Serialization;

namespace Tekly.SuperSerial.Serialization
{
	public class ObjectFactory
	{
		public object CreateInstance(Type type)
		{
			return FormatterServices.GetUninitializedObject(type);
		}
	}
}