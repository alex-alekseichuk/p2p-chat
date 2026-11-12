
#include <sys/socket.h>
#include <netinet/in.h>

#include <unistd.h>
#include <cstring>


#include "server.h"


namespace net
{
	void Server::init()
	{
		::signal(SIGPIPE, Connection::SIGPIPE_HANDLER);
	}


	void Server::listen(int port, int nListen)
	{
		if (m_socket > 0)
		{
			close();
		}

		m_socket = ::socket(AF_INET, SOCK_STREAM, 0);

		if (m_socket < 0)
		{
			m_socket = 0;
			m_logger.debug("socket() failed");
			return;
		}

		int optval = 1;
		::setsockopt(m_socket, SOL_SOCKET, SO_REUSEADDR, &optval, sizeof(optval));

		struct sockaddr_in servaddr;

		::bzero(&servaddr, sizeof(servaddr));
		servaddr.sin_family = AF_INET;
		servaddr.sin_addr.s_addr = ::htonl(INADDR_ANY);
		servaddr.sin_port = htons(port);

		if (::bind(m_socket, (struct sockaddr *)&servaddr, sizeof(servaddr)) != 0)
		{
			m_logger.debug("bind() failed");
			::close(m_socket);
			m_socket = 0;
			return;
		}

		if (::listen(m_socket, nListen) < 0)
		{
			m_logger.debug("listen() failed");
			::close(m_socket);
			m_socket = 0;
			return;
		}


		fd_set rset;
		FD_ZERO(&allset);
		FD_SET(m_socket, &allset);

		int maxfd = m_socket;
		int nready;
		bool bActivity;

		struct sockaddr_in cliaddr;
		int clilen;
		int socket;

		struct timeval tv;
		int timeout = 2; // initial select() timeot 2 sec.

		for (;;)
		{
			rset = allset;

			tv.tv_sec = timeout;
			tv.tv_usec = 0;
			
			bool bActivity = false;
			nready = ::select(maxfd + 1, &rset, NULL, NULL, &tv);

			// accept new incomming connections
			if (nready > 0 && FD_ISSET(m_socket, &rset))
			{
				clilen = sizeof(cliaddr);
				socket = ::accept(m_socket, (struct sockaddr *)&cliaddr, (socklen_t *)&clilen);
				if (socket > 0)
				{
					bActivity = true;

					if (socket > maxfd)
						maxfd = socket;
					FD_SET(socket, &allset);

					if (m_connections.find(socket) == m_connections.end())
					{
						m_connections[socket] = accept(socket);

						m_logger.debug("New connection accepted");
					} else {
						m_logger.log("Such connection already exists");
						::close(socket);
					}
				}
				--nready;
			}

			// check for incoming packets from connected users
			if (nready > 0)
			{
				t_conn_iter i = m_connections.begin();
				while (i != m_connections.end())
				{
					socket = static_cast<int>(i->first);
					Connection *pConnection = static_cast<Connection *>(i->second);
					//assert(socket > 0);
					//assert(pConnection != NULL);
					if (FD_ISSET(socket, &rset))
					{
						bActivity = true;

						if (! pConnection->receive())
						{
							i++;
							disconnect(pConnection);
						} else {
							i++;
						}

						if (--nready <= 0)
							break;
					} else {
						i++;
					}

				}
			}

			// check for connections should be disconnect by timeout
			for(t_connections::const_iterator it = m_connections.begin(); it != m_connections.end(); )
			{
				Connection *pConnection = static_cast<Connection *>(it->second);
				it++;

				Connection::timeout t = pConnection->checkDisconnectTimeout();
				if (t == Connection::TIMEOUT_DONE)
				{
					disconnect(pConnection);
				}
				if (t == Connection::TIMEOUT_DONE || t == Connection::TIMEOUT_WAIT)
				{
					bActivity = true;
				}

			}

			// correct timeout for next select()
			if (bActivity)
			{
				timeout = 2; // 2 sec. becuase there is some activity
			} else {
				if (timeout < 30)
					timeout *= 3;
			}

		}
	}



	void Server::close()
	{
		if (m_socket == 0)
			return;

		::close(m_socket);
		m_socket = 0;

		for(t_connections::const_iterator it = m_connections.begin(); it != m_connections.end(); ++it)
		{
			static_cast<Connection *>(it->second)->disconnect();
		}
		m_connections.clear();

	}

	Connection * Server::accept(int socket)
	{
		return new Connection(this, m_pPacker, socket);
	}

	void Server::disconnect(Connection *pConnection)
	{
		//assert(pConnection);

		int socket = pConnection->getSocket();

		pConnection->disconnect();

		if (socket > 0)
			FD_CLR(socket, &allset);

		delete pConnection;

		if (socket > 0)
			m_connections.erase(socket);

		m_logger.debug("Connection closed");
	}



	void Server::sendTo(const Packet packet, Connection *pToConnection)
	{
		if (! pToConnection->send(packet))
		{
			disconnect(pToConnection);
		}
	}
	void Server::sendAll(const Packet packet, Connection *pExcludeConnection)
	{
		for(t_connections::const_iterator it = m_connections.begin(); it != m_connections.end(); )
		{
			Connection *pConn = static_cast<Connection *>(it->second);
			if (pExcludeConnection == NULL || pExcludeConnection != pConn)
			{
				if (! pConn->send(packet))
				{
					++it;
					disconnect(pConn);
				} else {
					++it;
				}
			} else {
				++it;
			}
		}
	}

}

