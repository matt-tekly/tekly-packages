using System;

namespace Tekly.SuperSerial
{
	public class SuperSerialException : Exception
	{
		public SuperSerialException(string message) : base(message) { }
		public SuperSerialException(string message, Exception inner) : base(message, inner) { }
	}
}
