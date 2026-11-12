
#include <sys/socket.h>
#include <netinet/in.h>

#include <unistd.h>
#include <cstring>
#include <sstream>


#include "db.h"
#include "online_connection.h"


namespace online
{
	void OnlineDB::logout(size_t id)
	{
		if (! isConnected())
			return;

		PGresult   *res;
		std::ostringstream ossId;
		ossId << id;
		const char *params[1] = { ossId.str().c_str() };
		res = ::PQexecParams(m_pConn, "DELETE FROM cl_usrnow WHERE uid=$1",
			1, NULL, params, NULL, 0, 0);
		::PQclear(res);
	}
	int OnlineDB::checkLogin(size_t *pId, const char *login, const char *passwd, const char *sIP)
	{
		if (! isConnected())
			return 3;

		PGresult   *res;

		res = ::PQexec(m_pConn, "BEGIN");
		::PQclear(res);

		const char *params[2] = { login, passwd };
		int ret = 3;

		res = ::PQexecParams(m_pConn,
			"SELECT u.uid as id, l.uid as loggedId FROM cl_userlogin AS u"
			" LEFT JOIN cl_usrnow AS l ON u.uid=l.uid"
			" WHERE u.login=$1 AND u.pass=$2",
			2, NULL, params, NULL, 0, 0);

		if (res && ::PQresultStatus(res) == PGRES_TUPLES_OK)
		{
			ret = 2;

			if (::PQntuples(res) > 0)
			{
				*pId = ::atoi(::PQgetvalue(res, 0, 0));

				if (::PQgetisnull(res, 0, 1) != 1)
				{
					ret = 1;
				} else {
					ret = 0;
				}
			}
		}

		::PQclear(res);


		if (0 == ret || 1 == ret)
		{
			std::ostringstream ossId;
			ossId << (*pId);
			if (1 == ret)
			{
				const char *paramsDel[1] = { ossId.str().c_str() };
				res = ::PQexecParams(m_pConn, "DELETE FROM cl_usrnow WHERE uid=$1",
					1, NULL, paramsDel, NULL, 0, 0);
				::PQclear(res);
			}
			const char *paramsIns[2] = { ossId.str().c_str(), sIP };
			res = ::PQexecParams(m_pConn, "INSERT INTO cl_usrnow (uid, ip) VALUES ($1, $2)",
				2, NULL, paramsIns, NULL, 0, 0);
			::PQclear(res);
		}

		res = ::PQexec(m_pConn, "COMMIT");
		::PQclear(res);

		return ret;
	}

}

