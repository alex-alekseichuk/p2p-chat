#ifndef __NET__CONNECTION__H__
#define __NET__CONNECTION__H__

#include "packet.h"

namespace net
{
	class Server;

	class Connection
	{
		private:
			static Connection *s_pSigpipeConnection;
		public:
			static void SIGPIPE_HANDLER(int signal);

		class Parser
		{
		public:
			Parser(IPacker *pPacker, Connection *pConnection)
			:
			m_pConnection(pConnection),
			m_pPacker(pPacker),
			m_current(0),
			m_offset(0),
			m_pPacket(NULL)
			{
			}

			size_t getRemaining() {return Parser::s_size - m_current;}

			void parse(size_t readed)
			{
				const Packet packetMark(m_pPacker->getHeaderMark());
				const unsigned char *mark = packetMark.getBuffer();
				//byte[] mark = packer.GetHeaderMark();

				// header means: mark + 3 bytes of packet length
				int headerLen = packetMark.getSize() + 3;
				int i;

				// size := size (of data still in the temp. buffer) + all data ready to process
				m_current += readed;

				for (i = 0; i < m_current; i++)
				{
					if (m_offset < headerLen) // searching/saving the header (mark + length)
					{
						if (m_offset < packetMark.getSize()) // searching for the mark
						{
							if (mark[m_offset] == m_buffer[i]) // if it's correct 
							{
								m_offset++;
							} else
							{
								m_offset = 0;
							}
						} else // saving length
						{
							m_offset++;
							if (m_offset >= headerLen) // whole header is found and saved
							{
								// get length of the packet from the header
								int packetLen = m_pPacker->getPacketLen(m_buffer, i - 2);

								// crete buffer for the packet
								unsigned char *packetBuffer = new unsigned char[packetLen];
								//byte[] packetBuffer = new byte[packetLen];

								// copy already processed and saved header to the packet buffer
								::memcpy(packetBuffer, m_buffer + (i - headerLen + 1), headerLen);
								//Buffer.BlockCopy(m_buffer, i - headerLen + 1, packetBuffer, 0, headerLen);

								// create packet instance
								//packet = new Packet(packetBuffer, headerLen);
								m_pPacket = new Packet(packetBuffer, packetLen, headerLen);
								
							}
						}
					} else
					{
						// packet body reading
						if (m_pPacket->remaining() > 0)
						{
							m_pPacket->add(m_buffer[i]);
							if (m_pPacket->remaining() == 0)
							{
								// packet is completed

								m_offset = 0; // try to find next packet

								// process this packet
								m_pConnection->processPacket(m_pPacket);
								delete m_pPacket;
								m_pPacket = NULL;
							}
						}
					}
				} // for()

				// if we already readed part of the header(marker + len)
				// then save it for processing next time with another block of incomming data
				if (m_offset > 0)
				{
					for (i = 0; i < m_offset; i++)
						m_buffer[i] = m_buffer[m_current - m_offset + i];
					m_current = m_offset;
				} else
				{
					m_current = 0;
				}


			}

		private:
			Connection *m_pConnection;
			IPacker *m_pPacker;
			unsigned char m_buffer[1024];
			static const size_t s_size = 1024;
			size_t m_current;
			size_t m_offset;
			Packet *m_pPacket;

		};

	public:
		Connection(Server *pServer, IPacker *pPacker, int socket);

		virtual void disconnect();
		virtual bool receive();

		bool send(const Packet packet);

		int getSocket() {return m_socket;}
		Server *getServer() {return m_pServer;}

		typedef enum e_timeout
		{
			TIMEOUT_NONE = 0,	// don't need to check for timeout anymore
			TIMEOUT_DONE = 1,	// should be closed by timeout
			TIMEOUT_WAIT = 2	// wait for timeout
		} timeout;
		virtual timeout checkDisconnectTimeout()
		{
			return TIMEOUT_NONE;
		}

		const char *getAddress() {return m_sAddress.c_str();}

	protected:
		virtual void processPacket(const Packet *pPacket)
		{
		}

	private:
		Server *m_pServer;
		int m_socket;

		IPacker *m_pPacker;
		Parser m_parser;

		std::string m_sAddress;

	};

}

#endif

