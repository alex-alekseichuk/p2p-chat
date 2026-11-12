#ifndef __ONLINE__SERVER__H__
#define __ONLINE__SERVER__H__

#include <map>

#include "common/logger.h"
#include "net/connection.h"
#include "net/packet.h"
#include "net/server.h"

#include "online_connection.h"
#include "db.h"

namespace online
{

	class OnlineServer : public net::Server
	{

	public:
		OnlineServer(net::IPacker *pPacker)
		:
		net::Server(pPacker),
		m_db()
		{
		}

		virtual ~OnlineServer()
		{
			close();
		}

		
		virtual void close()
		{
			m_db.close();
			Server::close();
		}
		

		virtual net::Connection * accept(int socket);
		virtual void disconnect(net::Connection *pConnection);


		bool isLogged(size_t id);
		Member* getLoggedMember(size_t id);
		void onLogged(Member *pMember);
		void onMessage(const char *sMsg, size_t fromUserId, size_t toUserId);

		bool connectDB() {return m_db.connect();}


		/**
			@return
				0 for corect login
				1 already logged in
				2 incorrect login/passwd
				3 error or can't connected to db server
		*/
		int checkLogin(size_t *pId, const char *login, const char *passwd, const char *sIP)
		{
			return m_db.checkLogin(pId, login, passwd, sIP);
		}

	private:
		typedef std::map<int, Member*> t_members;
		typedef t_members::iterator t_member_iter;
		t_members m_members;

		OnlineDB m_db;
	};

}

#endif
