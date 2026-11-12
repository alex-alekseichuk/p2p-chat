
#include <unistd.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <arpa/inet.h>

#include <iostream>

#include "connection.h"
#include "server.h"

namespace net
{
	Connection *Connection::s_pSigpipeConnection = NULL;
	void Connection::SIGPIPE_HANDLER(int signal)
	{
		if (Connection::s_pSigpipeConnection != NULL)
			Connection::s_pSigpipeConnection->getServer()->disconnect(Connection::s_pSigpipeConnection);
	}



	Connection::Connection(Server *pServer, IPacker *pPacker, int socket)
	:
	m_pServer(pServer),
	m_socket(socket),
	m_pPacker(pPacker),
	m_parser(pPacker, this)
	{
		struct sockaddr_in cliaddr;
		socklen_t clilen = sizeof(cliaddr);
		if (! ::getpeername(socket, (struct sockaddr *)&cliaddr, &clilen))
		{
			const char *p = inet_ntoa(cliaddr.sin_addr);
			if (p)
			{
				m_sAddress = p;
			}
		}
	}

	void Connection::disconnect()
	{
		if (m_socket == 0)
			return;

		::shutdown(m_socket, SHUT_RDWR);
		::close(m_socket);

		m_socket = 0;
	}

	bool Connection::send(const Packet packet)
	{
		if (m_socket == 0)
			return false;

		Packet packetPacked = m_pPacker->pack(packet);

		Connection::s_pSigpipeConnection = this;
		::write(m_socket, packetPacked.getBuffer(), packetPacked.getSize());
		Connection::s_pSigpipeConnection = NULL;
		
		return true;
	}

	bool Connection::receive()
	{
		if (m_socket == 0)
			return false;

		const size_t SIZE = 4096;
		unsigned char buffer[SIZE];
		int n = ::read(m_socket, buffer, SIZE);
		if (n <= 0)
			return false;

		Packet packetPacked;
		packetPacked.copyBuffer(buffer, n);
		Packet packetUnpacked = m_pPacker->unpack(packetPacked);
		this->processPacket(&packetUnpacked);

		return true;
	}
}

