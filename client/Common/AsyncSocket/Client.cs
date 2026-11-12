using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Common;

namespace AsyncSocket
{
	class Client : BaseDisposable, IClient
	{
		private ISocketConnection m_connection = null;


		public Client()
		{
		}
		public Client(IClientService clientService)
		{
			Connected += new ConnectionHandler(clientService.OnConnected);
			Received += new MessageHandler(clientService.OnReceived);
			Sent += new MessageHandler(clientService.OnSent);
			Disconnected += new ConnectionHandler(clientService.OnDisconnected);
			Error += new ErrorHandler(clientService.OnError);
		}

		public event ConnectionHandler Connected;
		public event MessageHandler Received;
		public event MessageHandler Sent;
		public event ConnectionHandler Disconnected;
		public event ErrorHandler Error;
		

		# region IClient methods

		public void Connect(IPEndPoint ep)
		{
		}

		public void Disconnect()
		{
		}

		public void Send(byte[] buffer)
		{
		}

		#endregion



	}
}
