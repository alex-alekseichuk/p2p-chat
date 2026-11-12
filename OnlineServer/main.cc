
#include <sys/signal.h>

#include "net/packet.h"
#include "online_server.h"

static online::OnlineServer *g_pServer = NULL;
static void shutdown(int signal)
{
	if (g_pServer != NULL)
		g_pServer->close();
	g_pServer = NULL;

	::exit(0);
}

int main(int argc, char **argv)
{
	unsigned char sHeaderMark[5] = {1, 2, 3, 4, 5};
	net::Packet headerMark;
	headerMark.copyBuffer(sHeaderMark, 5);
	net::BasePacker packer(headerMark);
	online::OnlineServer server(&packer);

	::g_pServer = &server;
	::signal(SIGHUP, ::shutdown);
	::signal(SIGQUIT, ::shutdown);
	::signal(SIGTERM, ::shutdown);
	::signal(SIGKILL, ::shutdown);
	::signal(SIGINT, ::shutdown);

	online::OnlineServer::init();

	if (! server.connectDB())
	{
		server.log("Can't connect to DB");
		return 0;
	}
	server.listen(6667, 100);
	
	return 0;
}
