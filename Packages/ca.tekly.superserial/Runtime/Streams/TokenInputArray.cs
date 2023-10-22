namespace Tekly.SuperSerial.Streams
{
	// TODO: Test this
	public class TokenInputArray
	{
		private const int BufferSize = 4096;

		private readonly byte[] m_input;
		private int m_inputPosition;

		private readonly byte[] m_buffer;

		public TokenInputArray(byte[] input)
		{
			m_input = input;
			m_buffer = new byte[BufferSize];
		}

		public int ReadInt()
		{
			var result = SuperBitConverter.ReadInt(m_buffer, m_inputPosition);
			m_inputPosition += sizeof(int);

			return result;
		}

		public long ReadLong()
		{
			var result = SuperBitConverter.ReadLong(m_buffer, m_inputPosition);
			m_inputPosition += sizeof(long);

			return result;
		}

		public float ReadFloat()
		{
			var result = SuperBitConverter.ReadFloat(m_buffer, m_inputPosition);
			m_inputPosition += sizeof(float);

			return result;
		}

		public double ReadDouble()
		{
			var result = SuperBitConverter.ReadDouble(m_buffer, m_inputPosition);
			m_inputPosition += sizeof(double);

			return result;
		}

		public bool ReadBoolean()
		{
			var result = SuperBitConverter.ReadBool(m_buffer, m_inputPosition);
			m_inputPosition += 1;

			return result;
		}

		public string ReadString()
		{
			var length = ReadInt();

			var result = SuperBitConverter.ReadString(m_buffer, m_inputPosition, length);
			m_inputPosition += length;

			return result;
		}
	}
}