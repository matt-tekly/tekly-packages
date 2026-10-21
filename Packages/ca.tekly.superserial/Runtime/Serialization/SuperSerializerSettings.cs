namespace Tekly.SuperSerial.Serialization
{
	public class SuperSerializerSettings
	{
		public SerialConverterDatabase Converters = new SerialConverterDatabase();
		public ObjectFactory Factory = new ObjectFactory();

		/// <summary>
		/// When true (the default) a new serializer applies every generated registry that has announced itself
		/// through SuperSerialRegistries, and checks for late arrivals before failing on an unknown type.
		/// </summary>
		public bool AutoRegister = true;
	}
}
