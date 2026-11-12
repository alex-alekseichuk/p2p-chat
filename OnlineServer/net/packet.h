#ifndef __NET__PACKET__H__
#define __NET__PACKET__H__

#include <cstring>


namespace net
{

	class Packet
	{
	public:
		Packet()
			:
			m_buffer(NULL),
			m_size(0),
			m_offset(0)
		{
		}
		inline Packet(size_t size)
			:
			m_size(size),
			m_offset(0)
		{
			m_buffer = new unsigned char[size];
		}
		inline Packet(unsigned char* buffer, size_t size)
			:
			m_buffer(NULL),
			m_size(0),
			m_offset(0)
		{
			attachBuffer(buffer, size);
		}
		inline Packet(unsigned char* buffer, size_t size, size_t offset)
			:
			m_buffer(NULL),
			m_size(0),
			m_offset(offset)
		{
			attachBuffer(buffer, size, offset);
		}
		inline Packet(const Packet& packet)
			:
			m_buffer(NULL),
			m_size(0),
			m_offset(0)
		{
			copyBuffer(packet.getBuffer(), packet.getSize(), packet.getOffset());
		}

		inline ~Packet()
		{
			if (m_buffer != NULL)
			{
				delete[] m_buffer;
				m_buffer = NULL;
			}
		}

		Packet& operator=(const Packet &packet)
		{
			if (this == &packet)
				return *this;
			copyBuffer(packet.getBuffer(), packet.getSize(), packet.getOffset());
		}

		inline unsigned char *getBuffer() {return m_buffer;};
		inline const unsigned char *getBuffer() const {return m_buffer;};
		inline size_t getSize() const {return m_size;};
		inline size_t getOffset() const {return m_offset;};

		inline void copyBuffer(const unsigned char *buffer, size_t size, size_t offset = 0)
		{
			if (m_buffer != NULL)
			{
				delete[] m_buffer;
			}
			m_buffer = new unsigned char[size];
			::memcpy(m_buffer, buffer, size);
			m_size = size;
			m_offset = offset;
		}
		inline void attachBuffer(unsigned char *buffer, size_t size, size_t offset = 0)
		{
			if (m_buffer != NULL)
			{
				delete[] m_buffer;
			}
			m_buffer = buffer;
			m_size = size;
			m_offset = offset;
		}

		inline size_t remaining()
		{
			if (m_size > m_offset)
				return m_size - m_offset;
			return 0;
		}
		inline void add(const unsigned char uch)
		{
			if (m_size > m_offset)
			{
				m_buffer[m_offset++] = uch;
			}
		}

		/*
		inline void add(const unsigned char *buffer, size_t size)
		{
			if (remaining() >= size)
			{
				

			}
		}
		*/


	private:
		unsigned char *m_buffer;
		size_t m_size;
		size_t m_offset;
	};


	class IPacker
	{
	public:
		virtual Packet pack(const Packet packet) = 0;
		virtual Packet unpack(const Packet packet) = 0;
		virtual const Packet getHeaderMark() = 0;
		virtual size_t getPacketLen(const Packet packet, size_t offset) = 0;
		virtual size_t getPacketLen(const unsigned char *buffer, size_t offset) = 0;
		virtual void setPacketLen(Packet& packet, size_t offset, size_t size) = 0;
	};

	class BasePacker : public IPacker
	{
	public:
		BasePacker(const Packet packetHeaderMark)
		:
		m_packetHeaderMark(packetHeaderMark)
		{
		}

		const Packet getHeaderMark()
		{
			return m_packetHeaderMark;
		}
		size_t getPacketLen(const Packet packet, size_t offset)
		{
			const unsigned char *p = packet.getBuffer();
			return getPacketLen(p, offset);
		}
		size_t getPacketLen(const unsigned char *p, size_t offset)
		{
			return
				(((size_t)p[offset]) << 16)
				+
				(((size_t)p[offset + 1]) << 8)
				+
				((size_t)p[offset + 2]);
		}
		void setPacketLen(Packet& packet, size_t offset, size_t size)
		{
			unsigned char *buffer = packet.getBuffer();
			buffer[offset] = ((size & 0xFF0000) >> 16);
			buffer[offset + 1] = ((size & 0xFF00) >> 8);
			buffer[offset + 2] = (size & 0xFF);
		}

		Packet pack(const Packet packet)
		{
			int headerSize = m_packetHeaderMark.getSize() + 3;

			int messageLength = packet.getSize() + headerSize;

			unsigned char *result = new unsigned char[messageLength];
			//byte[] result = new byte[buffer.Length + headerSize];


			const unsigned char *headerMark = m_packetHeaderMark.getBuffer();

			// header mark
			for (int i = 0; i < m_packetHeaderMark.getSize(); i++)
			{
				result[i] = headerMark[i];
			}

			Packet packet2(result, messageLength);

			// length
			setPacketLen(packet2, m_packetHeaderMark.getSize(), messageLength);
			//SetPacketLength(ref result, m_headerMark.Length, messageLength);

			// body
			::memcpy(result + headerSize, packet.getBuffer(), packet.getSize());
			//Buffer.BlockCopy(buffer, 0, result, headerSize, buffer.Length);

			//Packet packet = new Packet(result);
			return packet2;		
		}
		Packet unpack(const Packet packet)
		{
			int headLen = 3 + m_packetHeaderMark.getSize();

			// check: length of packet is not less than supposed packet header
			if (packet.getSize() < headLen)
				return Packet();
				//throw new PackerException();

			// length saved in the header of the packed
			int len = getPacketLen(packet, m_packetHeaderMark.getSize());

			// check: saved length is the same as actual packet length
			if (len != packet.getSize())
				return Packet();
				//throw new PackerException();

			Packet packet2(len);
			//byte[] buffer = new byte[len];
			
			//Buffer.BlockCopy(packet.Buffer, headLen, buffer, 0, packet.Length - headLen);
			::memcpy(packet2.getBuffer(), packet.getBuffer() + headLen, packet.getSize() - headLen);

			return packet2;
		}

	private:
		const Packet m_packetHeaderMark;

	};

}

#endif

