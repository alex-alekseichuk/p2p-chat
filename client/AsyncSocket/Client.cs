using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace AsyncSocket
{
	public class Client : BaseDisposable, IClient
	{

		private struct ConnectCallback
		{
			public Client client;
			public Socket socket;
			public ConnectCallback(Client client, Socket socket)
			{
				this.client = client;
				this.socket = socket;
			}
		}

		private ISocketConnection m_connection = null;
        
		// default packer for this host
        private IPacker m_packer = null;
		public IPacker Packer
        {
            get
            {
                // default packer
                if (m_packer == null)
                    m_packer = new BasePacker(new byte[] {1, 2, 3, 4, 5});
                return m_packer;
            }
            set
            {
                m_packer = value;
            }
        }

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


        #region Destructor

        protected override void Free(bool canAccessFinalizable)
        {
            if (m_connection != null)
            {
                Disconnect();
            }

            base.Free(canAccessFinalizable);
        }

        #endregion


		# region IClient methods

		public void Connect(IPEndPoint ep)
		{
			if (m_connection == null)
			{
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.BeginConnect(ep, new AsyncCallback(Client.SocketConnected), new ConnectCallback(this, socket));
                }
				catch (Exception e)
                {
                    Error(new ErrorEventArgs(m_connection, e));
                }
				/*
                catch (Exception e)
                {
                    throw new AsyncSocketException();
                }
				*/
            }
		}
		protected static void SocketConnected(IAsyncResult ar)
		{
			ConnectCallback callback = (ConnectCallback)ar.AsyncState;
			try
			{
				callback.socket.EndConnect(ar);
				callback.client.m_connection = new SocketConnection((IHost)callback.client, callback.socket, callback.client.Packer);
				callback.client.Connected(new ConnectionEventArgs(callback.client.m_connection));
			}
			catch (Exception e)
			{
				callback.client.Error(new ErrorEventArgs(null, e));
			}
		}


		public void Disconnect()
		{
			if (m_connection != null)
			{
				m_connection.Disconnect();
				m_connection.Dispose();
                m_connection = null;
			}
		}

		public void Send(byte[] buffer)
		{
            if (m_connection != null)
            {
                m_connection.Send(buffer);
            }
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





	}
}
