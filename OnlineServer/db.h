#ifndef __ONLINE__DB__H__
#define __ONLINE__DB__H__


#include <libpq-fe.h>
#include <cstdlib>

#include "common/logger.h"


namespace online
{
	class OnlineServer;

	class OnlineDB
	{

	public:
		OnlineDB()
		:
		m_pConn(NULL)
		{
		}
		virtual ~OnlineDB()
		{
			close();
		}


		bool connect()
		{
			if (m_pConn != NULL)
				close();

			// Connection parameters are taken from the standard libpq
			// environment variables:
			//   PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD
			// The password is no longer hardcoded in the source code.
			if (::getenv("PGPASSWORD") == NULL)
			{
				common::Logger logger;
				logger.log("OnlineDB: PGPASSWORD environment variable is not set");
				return false;
			}

			m_pConn = ::PQconnectdb("");
			if (::PQstatus(m_pConn) != CONNECTION_OK)
			{
				m_pConn = NULL;
				return false;
			}

			initServerDB();

			return true;
		}
		bool isConnected() {return m_pConn != NULL;}
		void initServerDB()
		{
			if (isConnected())
			{
				PGresult *res;
				res = ::PQexec(m_pConn, "DELETE FROM cl_usrnow");
				::PQclear(res);
			}
		}
		void close()
		{
			if (isConnected())
			{
				initServerDB();

				::PQfinish(m_pConn);
				m_pConn = NULL;
			}
		}
		
		/**
			@return
				0 for corect login
				1 already logged in
				2 incorrect login/passwd
				3 error or can't connected to db server
		*/
		int checkLogin(size_t *pId, const char *login, const char *passwd, const char *sIP);
		void logout(size_t id);

	private:
		PGconn     *m_pConn;

	};

}

#endif
