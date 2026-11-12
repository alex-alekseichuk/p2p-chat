using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AsyncSocket
{

	#region Exceptions

	/// <summary>
	/// raised in Unpack method
	/// </summary>
    public class PackerException : Exception { }

	#endregion



	#region EventArgs

	/// <summary>
    /// Base event arguments for connection events.
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {

        #region Fields

        private readonly ISocketConnection m_connection;

        #endregion

        #region Constructor

        public ConnectionEventArgs(ISocketConnection connection)
        {
            m_connection = connection;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ISocketConnection from event.
        /// </summary>
        public ISocketConnection Connection
        {
            get { return m_connection; }
        }

        #endregion

    }

    /// <summary>
    /// Message event arguments for message events.
    /// </summary>
    public class MessageEventArgs : ConnectionEventArgs
    {

        #region Fields

        private readonly byte[] FBuffer;

        #endregion

        #region Constructor

        public MessageEventArgs(ISocketConnection connection, byte[] buffer)
            : base(connection)
        {
            FBuffer = buffer;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets sent or received buffer.
        /// </summary>
        public byte[] Buffer
        {
            get { return FBuffer; }
        }

        #endregion

    }

    /// <summary>
    /// Exception event arguments for exception event.
    /// </summary>
    public class ErrorEventArgs : ConnectionEventArgs
    {

        #region Fields

        private readonly Exception m_exception;

        #endregion

        #region Constructor

        public ErrorEventArgs(ISocketConnection connection, Exception exception)
            : base(connection)
        {
            m_exception = exception;
        }

        #endregion

        #region Properties

        public Exception Exception
        {
            get { return m_exception; }
        }

        #endregion

    }

    /// <summary>
    /// Exception event arguments for exception event.
    /// </summary>
    public class ServerEventArgs : EventArgs
    {

        #region Fields

        private readonly IServer m_server;

        #endregion

        #region Constructor

        public ServerEventArgs(IServer server)
        {
            m_server = server;
        }

        #endregion

        #region Properties

        public IServer Server
        {
            get { return m_server; }
        }

        #endregion

    }

    /// <summary>
    /// Exception event arguments for exception event.
    /// </summary>
    public class ServerConnectionEventArgs : ServerEventArgs
    {

        #region Fields

        private readonly ISocketConnection m_connection;

        #endregion

        #region Constructor

        public ServerConnectionEventArgs(IServer server, ISocketConnection connection)
            : base(server)
        {
            m_connection = connection;
        }

        #endregion

        #region Properties

        public ISocketConnection Connection
        {
            get { return m_connection; }
        }

        #endregion

    }

	#endregion



    #region Interfaces

    /// <summary>
    /// Common connection properties and methods.
    /// </summary>
    public interface ISocketConnection : IDisposable
    {
        /// <summary>
        /// Begin send data.
        /// </summary>
        /// <param name="buffer">
        /// Data to be sent.
        /// </param>
        void Send(byte[] buffer);

        /// <summary>
        /// Begin disconnect the connection.
        /// </summary>
        void Disconnect();
    }


	public interface IHost : IDisposable
	{
		void FireOnSent(MessageEventArgs e);
		void FireOnReceived(MessageEventArgs e);
		void FireOnDisconnected(ConnectionEventArgs e);
		void FireOnError(ErrorEventArgs e);
	}


    #region Host interfaces for application

    /// <summary>
    /// Client interface of the host for the application
    /// </summary>
    public interface IClient : IHost
    {
    	void Connect(IPEndPoint ep);
    	void Disconnect();
		void Send(byte[] buffer);
    }

    /// <summary>
	/// Server interface of the host for the application
    /// </summary>
	public interface IServer : IHost
    {
        void SendToAll(byte[] buffer, ISocketConnection excluded);

        void SendTo(ISocketConnection connection, byte[] buffer);

		void Listen(IPEndPoint ep, int listen);
    	void Close();
    	void Disconnect(ISocketConnection connection);
    	void DisconnectAll();

    }

    #endregion


    #region Service interfaces implemented by application for host

    public delegate void ConnectionHandler(ConnectionEventArgs e);
	public delegate void MessageHandler(MessageEventArgs e);
	public delegate void ErrorHandler(ErrorEventArgs e);
	public delegate void ServerHandler(ServerEventArgs e);
	public delegate void ServerConnectionHandler(ServerConnectionEventArgs e);
	

	/// <summary>
    /// Base Socket service methods. Should be implemented by both the server and the client services
    /// </summary>
    public interface ISocketService
    {
        /// <summary>
        /// Fired when connected (for Clients) or Accepted (for Server)
        /// </summary>
        /// <param name="e">
        /// Information about the connection.
        /// </param>
        void OnConnected(ConnectionEventArgs e);

        /// <summary>
        /// Fired when data arrives.
        /// </summary>
        /// <param name="e">
        /// Information about the Message.
        /// </param>
        void OnReceived(MessageEventArgs e);

        /// <summary>
        /// Fired when data is sent.
        /// </summary>
        /// <param name="e">
        /// Information about the Message.
        /// </param>
        void OnSent(MessageEventArgs e);

        /// <summary>
        /// Fired when disconnected.
        /// </summary>
        /// <param name="e">
        /// Information about the connection.
        /// </param>
        void OnDisconnected(ConnectionEventArgs e);

        /// <summary>
        /// Fired when exception occurs.
        /// </summary>
        /// <param name="e">
        /// Information about the exception and connection.
        /// </param>
        void OnError(ErrorEventArgs e);

    }

	public interface IClientService : ISocketService
	{
	}

	public interface IServerService : ISocketService
	{
		void OnBeforeListen(ServerEventArgs e);
		void OnListen(ServerEventArgs e);
		void OnBeforeClose(ServerEventArgs e);
		void OnClose(ServerEventArgs e);
	}

	#endregion




	#endregion



}
