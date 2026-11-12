using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using Common;

namespace AsyncSocket
{

	/// <summary>
	/// Keeps connection event information between callbacks
	/// </summary>
	internal class CallbackData
	{

		#region Fields

		private readonly BaseSocketConnection m_connection;
		private readonly Packet m_packet;

		#endregion

		#region Constructor

		public CallbackData(BaseSocketConnection connection, Packet packet)
		{
			m_connection = connection;
			m_packet = packet;
		}

		#endregion

		#region Properties

		public BaseSocketConnection Connection
		{
			get { return m_connection; }
		}

		public Packet Packet
		{
			get { return m_packet; }
		}

		#endregion

	}

	public class SocketConnection : BaseDisposable, ISocketConnection
    {
		//const int SocketBufferSize = 1024; // ?

        private Socket m_socket = null;
		public Socket Socket
		{
			get { return m_socket; }
		}

		//private ISocketService m_service = null;

		private Queue<Packet> m_queue = null;
		internal Queue<Packet> WriteQueue
		{
			get { return m_queue; }
		}

		private bool m_sending = false;
		internal bool Sending
		{
			get { return m_sending; }
			set { m_sending = value; }
		}


		#region Destructor

		protected override void Free(bool canAccessFinalizable)
		{
			if (m_queue != null)
			{
				m_queue.Clear();
				m_queue = null;
			}

			if (m_socket != null)
			{
				m_socket.Close();
				m_socket = null;
			}

			base.Free(canAccessFinalizable);
		}

		#endregion


		public void BeginSend(byte[] buffer)
		{
            if (!Disposed)
            {

				try
				{
					if (Active)
					{
						Packet packet = new Packet(buffer);

						lock (WriteQueue)
						{
							if (Sending)
							{
								//----- If the connection is sending, enqueue the message!
								WriteQueue.Enqueue(packet);
							}
							else
							{
								Sending = true;
								m_socket.BeginSend(packet.Buffer, packet.Offset, packet.Remaining, SocketFlags.None,
								                   new AsyncCallback(BeginSendCallback), new CallbackData(this, packet));
							}
						}
					}
				}
				catch (Exception ex)
				{
					FireOnException(this, ex);
				}
            }
		}

        public void BeginReceive()
        {

            if (!Disposed)
            {

                try
                {

                    if (Active)
                    {

                        lock (SyncReadCount)
                        {

                            if (ReadCanEnqueue)
                            {

                                if (ReadCount == 0)
                                {

                                    //----- if the connection is not receiving, start the receive!
                                    Packet packet = new Packet(SocketBufferSize);

									Socket.BeginReceive(packet.Buffer, packet.Offset, packet.Remaining, SocketFlags.None, new AsyncCallback(BeginReadCallback), new CallbackData(this, packet));

                                }

                                //----- Increase the read count!
                                ReadCount++;

                            }

                        }

                    }

                }
                catch (Exception ex)
                {
                    FireOnException(this, ex);
                }

            }

        }



        public void BeginDisconnect()
        {
            throw new NotImplementedException();
		}



		#region BeginReadCallback

		private void BeginReadCallback(IAsyncResult ar)
		{

			if (!Disposed)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(BeginReadCallbackProcessing), ar);
			}

		}
		private void BeginReadCallbackProcessing(object state)
		{

			if (!Disposed)
			{

				IAsyncResult ar = (IAsyncResult)state;

				BaseSocketConnection connection = null;

				try
				{

					CallbackData callbackData = (CallbackData)ar.AsyncState;

					connection = callbackData.Connection;

					if (connection.Active)
					{

						int readBytes = 0;

						readBytes = connection.Socket.EndReceive(ar);

						if (readBytes > 0)
						{
							ReadBytesFromConnection(callbackData, readBytes);
						}
						else
						{
							//----- Is has no data to read then the connection has been terminated ?
							connection.BeginDisconnect();
						}

					}
				}
				catch (Exception ex)
				{
					FireOnException(connection, ex);
				}

			}

		}
		private void ReadBytesFromConnection(CallbackData callbackData, int readBytes)
		{

			BaseSocketConnection connection = callbackData.Connection;
			Packet readMessage = callbackData.Packet;

			byte[] rawBuffer = null;
			bool socketWasRead = false;

			readMessage.Offset += readBytes;

			rawBuffer = readMessage.GetBuffer(readBytes);

			if (rawBuffer != null)
			{
				//----- Fire Event!
				FireOnReceived(connection, rawBuffer, true);
			}

			readMessage = null;
			callbackData = null;

			if (!socketWasRead)
			{

				//----- Check Queue!
				lock (SyncReadCount)
				{

					ReadCount--;

					if (ReadCount > 0)
					{

						//----- if it need more receiving, start the receive!
						Packet continueReadMessage = new Packet(SocketBufferSize);

						Socket.BeginReceive(continueReadMessage.Buffer, continueReadMessage.Offset, continueReadMessage.Remaining, SocketFlags.None, new AsyncCallback(BeginReadCallback), new CallbackData(connection, continueReadMessage));

					}

				}

			}

		}

		#endregion



		#region BeginSendCallback

		/// <summary>
        /// Send Callback.
        /// </summary>
        private void BeginSendCallback(IAsyncResult ar)
        {
            if (!Disposed)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(BeginSendCallbackProcessing), ar);
            }

        }

        /// <summary>
        /// Send Callback Processing.
        /// </summary>
        private void BeginSendCallbackProcessing(object state)
        {

            if (!Disposed)
            {

                IAsyncResult ar = (IAsyncResult) state;
                BaseSocketConnection connection = null;
                Packet packet = null;
                bool CanReadQueue = false;

                try
                {

                    CallbackData callbackData = (CallbackData) ar.AsyncState;

                    packet = callbackData.Packet;
                    connection = callbackData.Connection;

                    if (connection.Active)
                    {
                        int writeBytes = connection.Socket.EndSend(ar);

                        if (writeBytes < packet.Remaining)
                        {
                            //----- Continue to send until all bytes are sent!
                            packet.Offset += writeBytes;
                            connection.Socket.BeginSend(packet.Buffer, packet.Offset, packet.Remaining, SocketFlags.None, new AsyncCallback(BeginSendCallback), callbackData);
                        }
                        else
                        {
                            
                            FireOnSent(connection, packet.Buffer);
                            CanReadQueue = true;
                        }


                        //----- Check Queue!
                        if (CanReadQueue)
                        {

                            callbackData = null;
                            packet = null;

                            lock (connection.WriteQueue)
                            {
                                if (connection.WriteQueue.Count > 0)
                                {

                                    //----- If has items, send it!
                                    Packet dequeueWriteMessage = connection.WriteQueue.Dequeue();

									connection.Socket.BeginSend(dequeueWriteMessage.Buffer, dequeueWriteMessage.Offset, dequeueWriteMessage.Remaining, SocketFlags.None, new AsyncCallback(BeginSendCallback), new CallbackData(connection, dequeueWriteMessage));

                                } else
                                {
                                	Sending = false;
                                }

						    }

                        }

                    }

                }
                catch (Exception ex)
                {
                    FireOnException(connection, ex);
                }

            }

		}
		#endregion


		#region Fire Methods

		private void FireOnSent(BaseSocketConnection connection, byte[] buffer)
		{

			if (connection.Active)
			{
				m_service.OnSent(new MessageEventArgs(connection, buffer));
			}

		}
		internal void FireOnException(BaseSocketConnection connection, Exception ex)
		{

			if (connection.Active)
			{
				m_service.OnException(new ExceptionEventArgs(connection, ex));
			}

		}
		private void FireOnReceived(BaseSocketConnection connection, byte[] buffer, bool readCanEnqueue)
		{

			if (connection.Active)
			{

				if (!readCanEnqueue)
				{
					lock (connection.SyncReadCount)
					{
						connection.ReadCanEnqueue = false;
					}

				}

				m_service.OnReceived(new MessageEventArgs(connection, buffer));

				if (!readCanEnqueue)
				{

					lock (connection.SyncReadCount)
					{
						connection.ReadCanEnqueue = true;
					}

				}

			}

		}

		#endregion


		#region Active

		private bool FActive; 
		private object FSyncActive;
		internal bool Active
		{

			get
			{
				if (Disposed)
				{
					return false;
				}

				lock (FSyncActive)
				{
					return FActive;
				}
			}

			set
			{
				lock (FSyncActive)
				{
					FActive = value;
				}
			}

		}

		#endregion

		public void Send(byte[] buffer)
		{
			throw new NotImplementedException();
		}

		public void Disconnect()
		{
			throw new NotImplementedException();
		}
    }

}
