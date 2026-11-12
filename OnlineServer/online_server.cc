
#include <sys/socket.h>
#include <netinet/in.h>

#include <unistd.h>
#include <cstring>
#include <sstream>


#include "online_server.h"
#include "online_connection.h"


namespace online
{

	net::Connection * OnlineServer::accept(int socket)
	{
		return reinterpret_cast<net::Connection *>(new OnlineConnection(this, m_pPacker, socket));
	}

	void OnlineServer::disconnect(net::Connection *pConnection)
	{
		//assert(pConnection);
		Member *pMember = dynamic_cast<OnlineConnection *>(pConnection)->getMember();
		if (pMember != NULL)
		{
			m_db.logout(pMember->getId());
			m_members.erase(pMember->getId());

			std::ostringstream ossPacket;
			ossPacket << "<logged-out id=\"" << pMember->getId() << "\" />";
			std::string sPacket(ossPacket.str());

			for(t_member_iter i = m_members.begin(); i != m_members.end(); i++)
				if (static_cast<int>(i->first) != pMember->getId())
					static_cast<Member *>(i->second)->getConnection()->sendPacket(sPacket);

		}
		net::Server::disconnect(pConnection);
	}

	bool OnlineServer::isLogged(size_t id)
	{
		return m_members.find(id) != m_members.end();
	}
	Member* OnlineServer::getLoggedMember(size_t id)
	{
		t_member_iter i = m_members.find(id);
		if (i != m_members.end())
			return i->second;
		return NULL;
	}
	void OnlineServer::onLogged(Member *pMember)
	{
		//assert(pMember);
		if (isLogged(pMember->getId()))
			return;
		m_members[pMember->getId()] = pMember;

		std::ostringstream ossLoggedInPacket;
		ossLoggedInPacket << "<logged-in id=\"" << pMember->getId() << "\" login=\"" << pMember->getLogin() << "\" />";
		std::string sLoggedInPacket(ossLoggedInPacket.str());

		std::ostringstream ossUsersPacket;
		ossUsersPacket << "<users>";
		for(t_member_iter i = m_members.begin(); i != m_members.end(); i++)
		{
			Member *pM = static_cast<Member *>(i->second);
			if (static_cast<int>(i->first) != pMember->getId())
			{
				pM->getConnection()->sendPacket(sLoggedInPacket);
			}
			ossUsersPacket << "<user id=\"" << pM->getId() << "\" login=\"" << pM->getLogin() << "\" />";
		}
		ossUsersPacket << "</users>";
		pMember->getConnection()->sendPacket(ossUsersPacket.str());
	}

	void OnlineServer::onMessage(const char *sMsg, size_t fromUserId, size_t toUserId)
	{
		if ((! sMsg) || strlen(sMsg) == 0)
			return;

		std::ostringstream ossPacket;
		ossPacket << "<msg from=\"" << fromUserId << "\"";
		if (toUserId > 0)
			ossPacket << " to=\"" << toUserId << "\"";
		ossPacket << "><![CDATA[" << sMsg << "]]></msg>";

		if (toUserId > 0)
		{
			if (m_members.find(toUserId) != m_members.end())
			{
				Member *pMember = m_members[toUserId];
				pMember->getConnection()->sendPacket(ossPacket.str());
			}
		} else {
			for(t_member_iter i = m_members.begin(); i != m_members.end(); i++)
			{
				Member *pM = static_cast<Member *>(i->second);
				pM->getConnection()->sendPacket(ossPacket.str());
			}
		}
	}

}

