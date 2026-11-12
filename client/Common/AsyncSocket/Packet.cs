using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncSocket
{
	public interface IPacker
	{
		Packet Pack(byte[] buffer);
		byte[] Unpack(Packet packet);
	}
	public class BasePacker : IPacker
	{
		private readonly byte[] m_headerMark = null;

		public BasePacker(byte[] headerMark)
		{
			m_headerMark = headerMark;
		}
		public virtual Packet Pack(byte[] buffer)
		{
			int headerSize = m_headerMark.Length + 3;
			byte[] result = new byte[buffer.Length + headerSize];

			int messageLength = result.Length;

			// header mark
			for (int i = 0; i < m_headerMark.Length; i++)
			{
				result[i] = m_headerMark[i];
			}

			// length
			result[m_headerMark.Length] = Convert.ToByte((messageLength & 0xFF0000) >> 16);
			result[m_headerMark.Length + 1] = Convert.ToByte((messageLength & 0xFF00) >> 8);
			result[m_headerMark.Length + 2] = Convert.ToByte(messageLength & 0xFF);

			// body
			Buffer.BlockCopy(buffer, 0, result, headerSize, buffer.Length);

			Packet packet = new Packet(result);
			return packet;			
		}
		public virtual byte[] Unpack(Packet packet)
		{
			int headLen = 3 + m_headerMark.Length;

			// check: length of packet is not less than supposed packet header
			if (packet.Length < headLen)
				throw new PackerException();

			// length saved in the header of the packed
			int len =
				(packet.Buffer[m_headerMark.Length] << 16)
				+
				(packet.Buffer[m_headerMark.Length + 1] << 8)
				+
				packet.Buffer[m_headerMark.Length + 2];

			// check: saved length is the same as actual packet length
			if (len != packet.Length)
				throw new PackerException();

			byte[] buffer = new byte[len];
			Buffer.BlockCopy(packet.Buffer, headLen, buffer, 0, packet.Length - headLen);

			return buffer;
		}
	}

	public class Packet
	{
		private byte[] m_buffer = null;
		private int m_offset = 0;

		public Packet(byte[] buffer)
		{
			m_buffer = buffer;
			m_offset = 0;
		}

		public byte[] Buffer
		{
			get { return m_buffer; }
			set { m_buffer = value; }
		}

		public int Offset
		{
			get { return m_offset; }
			set { m_offset = value; }
		}

		public int Remaining
		{
			get { return m_buffer.Length - m_offset; }
		}

		public int Length
		{
			get { return m_buffer.Length; }
		}

	}
}
