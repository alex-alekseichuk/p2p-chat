using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Common;

namespace AsyncSocket
{
	/// <summary>
	/// class to parse received blocks of data from the net stream and compose it to the packets
	/// </summary>
	internal class Parser : BaseDisposable
	{
		/// <summary>
		/// size of temp. reader buffer
		/// </summary>
		const int max_size = 1024;

		/// <summary>
		/// temp. reader buffer
		/// </summary>
		public byte[] buffer = null;

		/// <summary>
		/// size of actual data in the buffer
		/// </summary>
		public int size = 0;

		/// <summary>
		/// free space in the buffer
		/// </summary>
 		public int remaining
		{
			get
			{
				return buffer.Length - size;
			}
		}

		/// <summary>
		/// current offset in the packer's header
		/// </summary>
		private int offset = 0;
		
		/// <summary>
		/// packer used in this parser
		/// </summary>
		public IPacker packer = null;

		/// <summary>
		/// collected packet
		/// </summary>
		private Packet packet = null;


		public Parser(IPacker packer)
		{
			buffer = new byte[max_size];
			this.packer = packer;
		}

		#region Destructor

		protected override void Free(bool canAccessFinalizable)
		{
			if (packet != null)
			{
				packet = null;
			}
			if (buffer != null)
			{
				buffer = null;
			}
			base.Free(canAccessFinalizable);
		}

		#endregion

		/// <summary>
		/// parsing of data received by incoming connection
		/// </summary>
		/// <param name="readed">number of incominmg bytes we can get to process</param>
		/// <param name="connection">connection to process</param>
		public void Parse(int readed, SocketConnection connection)
		{
			byte[] mark = packer.GetHeaderMark();

			// header means: mark + 3 bytes of packet length
			int headerLen = mark.Length + 3;
			int i;

			// size := size (of data still in the temp. buffer) + all data ready to process
			size += readed;

			for (i = 0; i < size; i++)
			{
				if (offset < headerLen) // searching/saving the header (mark + length)
				{
					if (offset < mark.Length) // searching for the mark
					{
						if (mark[offset] == buffer[i]) // if it's correct 
						{
							offset++;
						} else
						{
							offset = 0;
						}
					} else // saving length
					{
						offset++;
						if (offset >= headerLen) // whole header is found and saved
						{
							// get length of the packet from the header
							int packetLen = packer.GetPacketLength(buffer, i - 2);

							// crete buffer for the packet
							byte[] packetBuffer = new byte[packetLen];

							// copy already processed and saved header to the packet buffer
							Buffer.BlockCopy(buffer, i - headerLen + 1, packetBuffer, 0, headerLen);

							// create packet instance
							packet = new Packet(packetBuffer, headerLen);
						}
					}
				} else
				{
					// packet body reading
					if (packet.Remaining > 0)
					{
						packet.Add(buffer[i]);
						if (packet.Remaining == 0)
						{
							// packet is completed

							offset = 0; // try to find next packet

							// process this packet
							connection.PacketReceived(packet);
							packet = null;
						}
					}
				}
			} // for()

			// if we already readed part of the header(marker + len)
			// then save it for processing next time with another block of incomming data
			if (offset > 0)
			{
				for (i=0;i<offset;i++)
					buffer[i] = buffer[size - offset + i];
				size = offset;
			} else
			{
				size = 0;
			}
		}
	}



	public class SocketConnection : BaseDisposable, ISocketConnection
    {
        private IHost m_host;

        private Socket m_socket = null;
		public Socket Socket
		{
			get { return m_socket; }
		}

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


		// packer for this connection
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

		private Parser m_parser = null;


        internal SocketConnection(IHost host, Socket socket, IPacker packer)
        {
			m_host = host;
			m_socket = socket;
			Packer = packer;

			m_queue = new Queue<Packet>();
			m_parser = new Parser(packer);

			Active = true;
			BeginReceive();
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

			if (m_packer != null)
			{
				m_packer = null;
			}

			if (m_parser != null)
			{
				m_parser.Dispose();
				m_parser = null;
			}

			base.Free(canAccessFinalizable);
		}

		#endregion



		#region ISocketConnection

		public void Disconnect()
		{
			if (!Disposed)
			{
				if (Active)
				{
					Active = false;
					try
					{
						m_socket.Shutdown(SocketShutdown.Both);
						m_socket.Close();
						m_host.FireOnDisconnected(new ConnectionEventArgs(this));
					}
					catch (Exception e)
					{
						m_host.FireOnError(new ErrorEventArgs(this, e));
					}
				}
			}
		}

		public void Send(byte[] buffer)
		{
            if (!Disposed)
            {
				if (Active)
				{

					try
					{
						Packet packet = Packer.Pack(buffer);

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
												   new AsyncCallback(SocketConnection.SendCallback), new MessageCallback(this, packet));
							}
						}
					}
					catch (Exception ex)
					{
						Disconnect();
						m_host.FireOnError(new ErrorEventArgs(this, ex));
					}
				}
            }
		}

		#endregion



		private struct ConnectionCallback
		{
			internal SocketConnection connection;
			internal ConnectionCallback(SocketConnection connection)
			{
				this.connection = connection;
			}
		}
		private struct MessageCallback
		{
			internal SocketConnection connection;
			internal Packet packet;
			internal MessageCallback(SocketConnection connection, Packet packet)
			{
				this.connection = connection;
				this.packet = packet;
			}
		}


		private static void SendCallback(IAsyncResult ar)
		{
			MessageCallback callback = (MessageCallback)ar.AsyncState;
			if (! callback.connection.Disposed)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(SocketConnection.SendCallbackProcessing), ar);
			}

		}
		private static void SendCallbackProcessing(object state)
		{
			IAsyncResult ar = (IAsyncResult) state;
			MessageCallback callback = (MessageCallback)ar.AsyncState;

			if (! callback.connection.Disposed)
			{
				SocketConnection connection = callback.connection;
				Packet packet = callback.packet;
				bool CanReadQueue = false;

				try
				{
					if (connection.Active)
					{
						int writeBytes = connection.Socket.EndSend(ar);

						if (writeBytes < packet.Remaining)
						{
							//----- Continue to send until all bytes are sent!
							packet.Offset += writeBytes;
							connection.Socket.BeginSend(packet.Buffer, packet.Offset, packet.Remaining, SocketFlags.None, new AsyncCallback(SendCallback), new MessageCallback(connection, packet));
						}
						else
						{

							connection.m_host.FireOnSent(new MessageEventArgs(connection, packet.Buffer));
							CanReadQueue = true;
						}


						//----- Check Queue!
						if (CanReadQueue)
						{
							lock (connection.WriteQueue)
							{
								if (connection.WriteQueue.Count > 0)
								{

									//----- If has items, send it!
									Packet dequeueWriteMessage = connection.WriteQueue.Dequeue();

									connection.Socket.BeginSend(dequeueWriteMessage.Buffer, dequeueWriteMessage.Offset, dequeueWriteMessage.Remaining, SocketFlags.None, new AsyncCallback(SocketConnection.SendCallback), new MessageCallback(connection, dequeueWriteMessage));

								}
								else
								{
									connection.Sending = false;
								}

							}

						}

					}

				}
				catch (Exception ex)
				{
					connection.Disconnect();
					connection.m_host.FireOnError(new ErrorEventArgs(connection, ex));
				}

			}

		}




		public void BeginReceive()
        {
            if (!Disposed)
            {
				if (Active)
				{
					try
					{
						m_socket.BeginReceive(m_parser.buffer, m_parser.size, m_parser.remaining, SocketFlags.None,
						                      new AsyncCallback(ReadCallback), new ConnectionCallback(this));
					}
					catch (Exception ex)
					{
						Disconnect();
						m_host.FireOnError(new ErrorEventArgs(this, ex));
					}
				}
            }
        }





		private static void ReadCallback(IAsyncResult ar)
		{
			ConnectionCallback callback = (ConnectionCallback)ar.AsyncState;
			SocketConnection connection = callback.connection;

			if (! connection.Disposed)
			{
				if (connection.Active)
				{
					try
					{
						int readed = connection.m_socket.EndReceive(ar);

						if (readed > 0)
						{
							connection.m_parser.Parse(readed, connection);
							connection.BeginReceive();
						}
						else
						{
							connection.Disconnect();
						}
					}
					catch (Exception ex)
					{
						connection.Disconnect();
						connection.m_host.FireOnError(new ErrorEventArgs(connection, ex));
					}
				}
			}
		}
		public void PacketReceived(Packet packet)
		{
			if (! Disposed)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(PacketProcessing), packet);
			}
		}

		private void PacketProcessing(object state)
		{
			if (! Disposed)
			{
				Packet packet = (Packet)state;

				try
				{
                    byte[] buffer = m_packer.Unpack(packet);
                    m_host.FireOnReceived(new MessageEventArgs(this, buffer));
                }
				catch (Exception ex)
				{
					m_host.FireOnError(new ErrorEventArgs(this, ex));
				}
			}

		}




		#region Active

		private bool FActive = false;
		private object FSyncActive = new object();
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


	}

}
