using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace AsyncSocket
{
	class Server : BaseDisposable, IServer
	{
		private Socket m_socket = null;
		public Socket Socket
		{
			get { return m_socket; }
		}

		private List<ISocketConnection> m_connections;



		public Server()
		{
		}

		public Server(IServerService service)
		{
			Connected += new ConnectionHandler(service.OnConnected);
			Received += new MessageHandler(service.OnReceived);
			Sent += new MessageHandler(service.OnSent);
			Disconnected += new ConnectionHandler(service.OnDisconnected);
			Error += new ErrorHandler(service.OnError);

			BeforeListen += new ServerHandler(service.OnBeforeListen);
			Listening += new ServerHandler(service.OnListen);
			BeforeClose += new ServerHandler(service.OnBeforeClose);
			Closed += new ServerHandler(service.OnClose);
			Accepted += new ServerConnectionHandler(service.OnAccepted);
		}

		public event ConnectionHandler Connected;
		public event MessageHandler Received;
		public event MessageHandler Sent;
		public event ConnectionHandler Disconnected;
		public event ErrorHandler Error;

		public event ServerHandler BeforeListen;
		public event ServerHandler Listening;
		public event ServerHandler BeforeClose;
		public event ServerHandler Closed;
		public event ServerConnectionHandler Accepted;



		#region IServer members

		public void Listen(IPEndPoint ep)
		{
		}

		public void Close()
		{
		}

		public void Disconnect(ISocketConnection connection)
		{
		}
		public void DisconnectAll()
		{
		}

		public void SendToAll(byte[] buffer, bool includeMe)
		{
		}

		public void SendTo(ISocketConnection connection, byte[] buffer)
		{
		}

		#endregion


	}
}
