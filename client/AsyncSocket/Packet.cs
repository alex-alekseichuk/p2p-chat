using System;
using System.Collections.Generic;
using System.Text;

namespace AsyncSocket
{
	public class Packet
	{
		/// <summary>
		/// current buffer of the packet
		/// </summary>
		private byte[] m_buffer = null;

		/// <summary>
		/// offset of free space in the buffer
		/// </summary>
		private int m_offset = 0;

		public Packet(byte[] buffer, int offset)
		{
			m_buffer = buffer;
			m_offset = offset;
		}
		public Packet(byte[] buffer)
		{
			m_buffer = buffer;
			m_offset = 0;
		}

		/// <summary>
		/// current buffer of the packet
		/// </summary>
		public byte[] Buffer
		{
			get { return m_buffer; }
			set { m_buffer = value; }
		}

		/// <summary>
		/// offset of free space in the buffer
		/// </summary>
		public int Offset
		{
			get { return m_offset; }
			set { m_offset = value; }
		}

		/// <summary>
		/// size of free space in the buffer
		/// </summary>
		public int Remaining
		{
			get { return m_buffer.Length - m_offset; }
		}

		/// <summary>
		/// length of the whole packet buffer
		/// </summary>
		public int Length
		{
			get { return m_buffer.Length; }
		}

		/// <summary>
		/// Add several bytes to the buffer. If it needs the buffer would be increased.
		/// </summary>
		/// <param name="buffer">buffer to add to the packet</param>
		/// <param name="offset">offset of the data in incomming buffer</param>
		/// <returns>number of added bytes</returns>
		public int Add(byte[] buffer, int offset)
		{
			int toAdd = buffer.Length - offset;
			if (toAdd > Remaining)
				toAdd = Remaining;
			System.Buffer.BlockCopy(buffer, offset, Buffer, Offset, toAdd);
			Offset += toAdd;
			return toAdd;
		}

		/// <summary>
		/// Add one byte to the buffer. Packet buffer would not be increased.
		/// </summary>
		/// <param name="b">byte to add</param>
		public void Add(byte b)
		{
			if (Remaining > 0)
			{
				Buffer[Offset] = b;
				Offset++;
			}
		}

	}


	/// <summary>
	/// Interface to pack/unpack packets to transfer via net
	/// </summary>
	public interface IPacker
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer">body of the packet</param>
		/// <returns>packet ready to send via net</returns>
		Packet Pack(byte[] buffer);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		byte[] Unpack(Packet packet);

		/// <summary>
		/// the mark of the begin of the packet
		/// </summary>
		/// <returns>the bytes of the mark</returns>
		byte[] GetHeaderMark();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		int GetPacketLength(byte[] buffer, int offset);

		void SetPacketLength(ref byte[] buffer, int offset, int len);
	}
	public class BasePacker : IPacker
	{
		private readonly byte[] m_headerMark = null;
		public byte[] GetHeaderMark()
		{
			return m_headerMark;
		}

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
			SetPacketLength(ref result, m_headerMark.Length, messageLength);

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
			int len = GetPacketLength(packet.Buffer, m_headerMark.Length);

			// check: saved length is the same as actual packet length
			if (len != packet.Length)
				throw new PackerException();

			byte[] buffer = new byte[len];
			Buffer.BlockCopy(packet.Buffer, headLen, buffer, 0, packet.Length - headLen);

			return buffer;
		}

		/// <summary>
		/// get saved length (3 bytes)
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset">offset of the place where the length saved</param>
		/// <returns></returns>
		public int GetPacketLength(byte[] buffer, int offset)
		{
			return
				(((int)buffer[offset]) << 16)
				+
				(((int)buffer[offset + 1]) << 8)
				+
				((int)buffer[offset + 2]);
		}

		/// <summary>
		/// set length (3 bytes) of the packet in the buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset">offset of the place to put specified length of the packet</param>
		/// <param name="len"></param>
		public void SetPacketLength(ref byte[] buffer, int offset, int len)
		{
			buffer[offset] = Convert.ToByte((len & 0xFF0000) >> 16);
			buffer[offset + 1] = Convert.ToByte((len & 0xFF00) >> 8);
			buffer[offset + 2] = Convert.ToByte(len & 0xFF);
		}
	}



}
