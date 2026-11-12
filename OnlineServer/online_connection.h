#ifndef __ONLINE__CONNECTION__H__
#define __ONLINE__CONNECTION__H__

#include "net/packet.h"
#include "net/server.h"
#include "net/connection.h"

#include "expat/expatcc.h"

namespace online
{

	class OnlineServer;
	class OnlineConnection;


	class Member
	{
	public:
		Member(OnlineConnection *pConnection, size_t id, const char *login)
		:
		m_pConnection(pConnection),
		m_id(id),
		m_login(login)
		{

		}

		size_t getId() const {return m_id;}
		std::string getLogin() const {return m_login;}
		OnlineConnection *getConnection() {return m_pConnection;}
	private:
		OnlineConnection *m_pConnection;
		size_t m_id;
		std::string m_login;
	};

	class Parser : public expat::Expat
	{
	public:
		typedef enum e_PACKET
		{
			PACKET_NONE = 0,
			PACKET_AUTH = 1,
			PACKET_MSG = 2
		} PACKET;

		Parser(OnlineConnection *pOnlineConnection)
		:
		m_pOnlineConnection(pOnlineConnection),
		m_packet(PACKET_NONE),
		m_toUserId(0)
		{
		}

		PACKET getPacket() {return m_packet;}
		const char *getMessage() {return m_message.c_str();}
		const char *getLogin() {return m_login.c_str();}
		const char *getPasswd() {return m_passwd.c_str();}
		int getToUserId() {return m_toUserId;}
	private:
		virtual void startElement(const char* name, const char** atts);
		virtual void endElement(const char* name);
		virtual void charData(const char *s, int len);

		void sendPacket(const std::string str);

	protected:
		OnlineConnection *m_pOnlineConnection;
		PACKET m_packet;
		std::string m_login;
		std::string m_passwd;
		std::string m_message;
		size_t m_toUserId;
	};


	class OnlineConnection : public net::Connection
	{

	public:
		OnlineConnection(OnlineServer *pServer, net::IPacker *pPacker, int socket)
		:
		net::Connection(reinterpret_cast<net::Server *>(pServer), pPacker, socket),
		m_pMember(NULL),
		m_pServer(pServer),
		m_nLoginCounter(0)
		{
		}

		/*
		virtual ~OnlineConnection()
		{
		}
		*/

		virtual void disconnect();

		//virtual bool receive();


		void sendPacket(const std::string str);


		virtual timeout checkDisconnectTimeout()
		{
			if (m_pMember != NULL)
				return TIMEOUT_NONE;
			if (m_nLoginCounter > OnlineConnection::LOGIN_COUNTER_LIMIT)
				return TIMEOUT_DONE;
			++m_nLoginCounter;
			return TIMEOUT_WAIT;
		}

		Member *getMember() {return m_pMember;}
	protected:
		virtual void processPacket(const net::Packet *pPacket);

	private:
		Member *m_pMember;
		OnlineServer *m_pServer;

		size_t m_nLoginCounter;
	public:
		static const size_t LOGIN_COUNTER_LIMIT = 4;

	};

}

#endif

