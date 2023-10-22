using System;
using System.IO;
using System.Runtime.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.SuperSerial.Serialization
{
	public class SuperSerializer
	{
		private readonly SuperSerializerSettings m_settings;
		
		public SuperSerializer()
		{
			m_settings = new SuperSerializerSettings();
		}
		
		public void Write(TokenOutputStream output, object obj)
		{
			if (obj is ISuperSerialize superSerialize) {
				superSerialize.Write(output, this);
			} else {
				var converter = m_settings.Converters.Get(obj.GetType());
				converter.Write(this, output, obj);	
			}
		}
		
		public void Write(string filePath, object obj)
		{
			using var stream = File.OpenWrite(filePath);
			using var output = new TokenOutputStream(stream);
			
			Write(output, obj);
		}
		
		public T Read<T>(TokenInputStream input, T existing = default)
		{
			return (T) Read(input, typeof(T), existing);
		}
		
		public object Read(TokenInputStream input, Type type, object existing)
		{
			if (typeof(ISuperSerialize).IsAssignableFrom(type)) {
				var superSerializer = (ISuperSerialize) existing ?? (ISuperSerialize) CreateInstance(type);
				superSerializer.Read(input, this);
				return superSerializer;
			}

			var converter = m_settings.Converters.Get(type);
			return converter.Read(this, input, existing);
		}
		
		public T Read<T>(string filePath, T existing = default)
		{
			using var stream = File.OpenRead(filePath);
			using var input = new TokenInputStream(stream);
			
			return (T) Read(input, typeof(T), existing);
		}

		public object CreateInstance(Type type)
		{
			return m_settings.Factory.CreateInstance(type);
		}
	}
}