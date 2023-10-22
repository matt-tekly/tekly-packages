using Tekly.SuperSerial.Streams;
using UnityEngine;

namespace Tekly.SuperSerial.Serialization
{
	public class SerialConverterVector2 : SerialConverter
	{
		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			stream.Write((Vector2) obj);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			return stream.ReadVector2();
		}
	}
	
	public class SerialConverterVector3 : SerialConverter
	{
		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			stream.Write((Vector3) obj);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			return stream.ReadVector3();
		}
	}
	
	public class SerialConverterQuaternion : SerialConverter
	{
		public override void Write(SuperSerializer serializer, TokenOutputStream stream, object obj)
		{
			stream.Write((Quaternion) obj);
		}

		public override object Read(SuperSerializer serializer, TokenInputStream stream, object existing)
		{
			return stream.ReadQuaternion();
		}
	}
}