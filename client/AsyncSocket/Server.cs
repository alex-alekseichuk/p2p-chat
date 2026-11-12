using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace AsyncSocket
{
	public class Server : BaseDisposable, IServer
	{
		private Socket m_socket = null;
		public Socket Socket
		{
			get { return m_socket; }
		}

		private List<ISocketConnection> m_connections;
		public void addConnection(ISocketConnection connection)
		{
			m_connections.Add(connection);
		}

		// default packer for this host
		private IPacker m_packer = null;
		public IPacker Packer
		{
			get
			{
				// default packer
				if (m_packer == null)
					m_packer = new BasePacker(new byte[] { 1, 2, 3, 4, 5 });
				return m_packer;
			}
			set
			{
				m_packer = value;
			}
		}


		public Server()
		{
			m_connections = new List<ISocketConnection>();
		}

		public Server(IServerService service)
		{
			m_connections = new List<ISocketConnection>();

			Connected += new ConnectionHandler(service.OnConnected);
			Received += new MessageHandler(service.OnReceived);
			Sent += new MessageHandler(service.OnSent);
			Disconnected += new ConnectionHandler(service.OnDisconnected);
			Error += new ErrorHandler(service.OnError);

			BeforeListen += new ServerHandler(service.OnBeforeListen);
			Listening += new ServerHandler(service.OnListen);
			BeforeClose += new ServerHandler(service.OnBeforeClose);
			Closed += new ServerHandler(service.OnClose);
		}


		#region Destructor

		protected override void Free(bool canAccessFinalizable)
		{
			if (m_socket != null)
			{
				Close();
			}

			base.Free(canAccessFinalizable);
		}

		#endregion


		public event ConnectionHandler Connected;
		public event MessageHandler Received;
		public event MessageHandler Sent;
		public event ConnectionHandler Disconnected;
		public event ErrorHandler Error;

		public event ServerHandler BeforeListen;
		public event ServerHandler Listening;
		public event ServerHandler BeforeClose;
		public event ServerHandler Closed;



		#region IServer members

		public void Listen(IPEndPoint ep, int listen)
		{
			if (m_socket != null)
				Close();

			// open the socket
			try
			{
				m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				//IPEndPoint localEP = new IPEndPoint(IPAddress.Any, settings.port);
				m_socket.Bind(ep);
				m_socket.Listen(listen);

				m_socket.BeginAccept(new AsyncCallback(Server.AcceptedCallback), this);
			}
			catch (Exception exc)
			{
				Error(new ErrorEventArgs(null, exc));
			}

		}

		public void Close()
		{
			if (m_socket != null)
			{
				DisconnectAll();
				m_socket.Close();
				m_socket = null;
			}

		}

		public void Disconnect(ISocketConnection connection)
		{
			connection.Disconnect();
			m_connections.Remove(connection);
		}
		public void DisconnectAll()
		{
			foreach(ISocketConnection connection in m_connections)
			{
				connection.Disconnect();
			}
			m_connections.Clear();
		}

		public void SendToAll(byte[] buffer, ISocketConnection excluded)
		{
			foreach (ISocketConnection connection in m_connections)
			{
				if (connection != excluded)
					connection.Send(buffer);
			}
		}

		public void SendTo(ISocketConnection connection, byte[] buffer)
		{
			connection.Send(buffer);
		}

		#endregion


		#region IHost members

		public void FireOnSent(MessageEventArgs e)
		{
			Sent(e);
		}
		public void FireOnReceived(MessageEventArgs e)
		{
			Received(e);
		}
		public void FireOnDisconnected(ConnectionEventArgs e)
		{
			Disconnected(e);
		}
		public void FireOnError(ErrorEventArgs e)
		{
			Error(e);
		}

		#endregion


		/// <summary>
		/// callback for incoming connection accepting
		/// </summary>
		/// <param name="ar">has server as AsyncState property</param>
		public static void AcceptedCallback(IAsyncResult ar)
		{
			Server server = (Server)ar.AsyncState; // get server object
			if (server.m_socket != null)
			{
				try
				{
					Socket socket = server.m_socket.EndAccept(ar); // get new client socket

					if (socket != null)
					{
						//socket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 1048576);
						//socket.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 1048576);
						socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 10000);
						//socket.SetSocketOption (SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
						socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, 1);
						socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
						//s2.SetSocketOption (SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption (true, 1));

						SocketConnection connection = new SocketConnection((IHost)server, socket, server.Packer);
						server.addConnection(connection);
						server.Connected(new ConnectionEventArgs(connection));
					}

					// listen for another client connection
					server.m_socket.BeginAccept(new AsyncCallback(Server.AcceptedCallback), server);
				}
				catch (Exception exc)
				{
					server.Error(new ErrorEventArgs(null, exc));
				}
			}
		}


	}
}
