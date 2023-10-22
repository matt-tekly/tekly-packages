using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Interface for an object to implement its own serialization
	/// </summary>
	public interface ISuperSerialize
	{
		void Write(TokenOutputStream output, SuperSerializer superSerializer);
		void Read(TokenInputStream input, SuperSerializer superSerializer);
	}
}