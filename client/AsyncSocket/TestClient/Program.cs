using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;

using AsyncSocket;

using Common;

namespace TestClient
{
	class Program
	{
		static void Main(string[] args)
		{
			Client client = new Client();

			client.Connected += new ConnectionHandler(OnConnected);
			client.Received += new MessageHandler(OnReceived);
			client.Sent += new MessageHandler(OnSent);
			client.Disconnected += new ConnectionHandler(OnDisconnected);
			client.Error += new ErrorHandler(OnError);

			IPEndPoint localEP = new IPEndPoint(IPAddress.Loopback, 14);
			client.Connect(localEP);

			Console.ReadLine();

			client.Send(Encoding.UTF8.GetBytes("test"));

			Console.ReadLine();

			client.Disconnect();

			Console.ReadLine();
		}

		static void OnConnected(ConnectionEventArgs e)
		{
			Console.WriteLine("connected");
		}
		static void OnDisconnected(ConnectionEventArgs e)
		{
			Console.WriteLine("disconnected");
		}
		static void OnReceived(MessageEventArgs e)
		{
			string s = Encoding.UTF8.GetString(e.Buffer);
			Console.WriteLine("received: " + s);
			//s = "RE: " + s;
			//e.Connection.Send(Encoding.UTF8.GetBytes(s));
		}
		static void OnSent(MessageEventArgs e)
		{
			Console.WriteLine("sent to server: " + Encoding.UTF8.GetString(e.Buffer));
		}
		static void OnError(ErrorEventArgs e)
		{
			Console.WriteLine("error: " + e.Exception.Message);
		}

	}
}
