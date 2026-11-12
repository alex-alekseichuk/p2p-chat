
#include <unistd.h>
#include <sys/socket.h>

#include <iostream>
#include <sstream>

#include "net/server.h"
#include "expat/expatcc.h"
#include "openssl/openssl.h"

#include "online_connection.h"
#include "online_server.h"

namespace online
{
	const size_t OnlineConnection::LOGIN_COUNTER_LIMIT;

	void Parser::startElement(const char* name, const char** atts)
	{
		if (std::string("auth") == name)
		{
			for (int i = 0; atts[i]; i += 2)
			{
				if (std::string("login") == atts[i])
				{
					m_login = atts[i + 1];
				}
				if (std::string("passwd") == atts[i])
				{
					m_passwd = atts[i + 1];
				}
			}
			m_packet = PACKET_AUTH;
		}
		if (std::string("msg") == name)
		{
			for (int i = 0; atts[i]; i += 2)
			{
				if (std::string("to") == atts[i])
				{
					m_toUserId = atoi(atts[i + 1]);
				}
			}
			m_packet = PACKET_MSG;
		}

		/*
		for (int i = 0; i < getDepth(); ++i)
			std::cout << "\t";
		std::cout << "start tag: " << name;
		for (int i = 0; atts[i]; i += 2)
			std::cout << " " << atts[i] << "='" << atts[i + 1] << "'";
		std::cout << std::endl;
		*/
	}

	void Parser::endElement(const char* name)
	{
		/*
		for (int i = 0; i < getDepth(); ++i)
			std::cout << "\t";
		std::cout << "end tag: " << name << std::endl;
		*/
	}

	void Parser::charData(const char *s, int len)
	{
		if (m_packet == PACKET_MSG)
			m_message = std::string(s, len);

		/*
		for (int i = 0; i < getDepth(); ++i)
			std::cout << "\t";
		std::cout.write(s, len);
		std::cout << std::endl;
		*/
	}


	void OnlineConnection::sendPacket(const std::string str)
	{
		std::string s;
		s += "<online>";
		s += str;
		s += "</online>";
		net::Packet packet;
		packet.copyBuffer(reinterpret_cast<const unsigned char *>(s.c_str()), s.length());
		send(packet);
	}
	void OnlineConnection::processPacket(const net::Packet *pPacket)
	{
		online::Parser parser(this);

		parser.parse(reinterpret_cast<const char *>(pPacket->getBuffer()), pPacket->getSize(), 1);

		switch (parser.getPacket())
		{
		case online::Parser::PACKET_AUTH:
			{
				bool bLoggedOk(false);

				size_t memberId = 0;

				//if (std::string("12345") == parser.getPasswd())
				std::string sPasswdHash = openssl::Openssl::sha1(parser.getPasswd());
				int res = m_pServer->checkLogin(&memberId, parser.getLogin(), sPasswdHash.c_str(), getAddress());
				if (res == 0 || res == 1)
				{
					Member *pMember = m_pServer->getLoggedMember(memberId);
					if (pMember)
					{
						m_pServer->disconnect(pMember->getConnection());
					}

					m_pMember = new Member(this, memberId, parser.getLogin());
					bLoggedOk = true;

					std::ostringstream ossPacket;
					ossPacket << "<auth-ok id=\"" << memberId << "\" />";
					sendPacket(ossPacket.str());

					m_pServer->onLogged(m_pMember);
				}
				if (! bLoggedOk)
				{
					sendPacket(std::string("<auth-incorrect />"));
					m_pServer->disconnect(this);
				}
			}
			break;
		case online::Parser::PACKET_MSG:
			if (m_pMember != NULL && parser.getMessage() != NULL && strlen(parser.getMessage()) > 0)
			{
				m_pServer->onMessage(parser.getMessage(), m_pMember->getId(), parser.getToUserId());
			}
			break;
		}

	}

	void OnlineConnection::disconnect()
	{
		if (m_pMember != NULL)
		{
			delete m_pMember;
			m_pMember = NULL;
		}
		Connection::disconnect();
	}

}

