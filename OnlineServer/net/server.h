#ifndef __NET__SERVER__H__
#define __NET__SERVER__H__

#include <sys/signal.h>

#include <map>

#include "../common/logger.h"
#include "connection.h"
#include "packet.h"

namespace net
{

	class Server
	{

	public:
		static void init();

		Server(IPacker *pPacker)
		:
		m_logger(),
		m_socket(0),
		m_pPacker(pPacker)
		{
			FD_ZERO(&allset);
		}

		virtual ~Server()
		{
			close();
		}


		void listen(int port, int nListen);

		virtual void close();
		virtual Connection * accept(int socket);
		virtual void disconnect(Connection *pConnection);

		void sendTo(const Packet packet, Connection *pToConnection);
		void sendAll(const Packet packet, Connection *pExcludeConnection);

		void log(const char *msg) {m_logger.log(msg);}
		void debug(const char *msg) {m_logger.debug(msg);}
	private:
		common::Logger m_logger;
		int m_socket;
	protected:
		IPacker *m_pPacker;
	private:
		fd_set allset;

		typedef std::map<int, Connection*> t_connections;
		typedef t_connections::iterator t_conn_iter;
		t_connections m_connections;

	};

}

#endif
