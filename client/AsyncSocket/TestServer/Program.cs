using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;

using AsyncSocket;

namespace TestServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Server server = new Server();

			server.Connected += new ConnectionHandler(OnConnected);
			server.Received += new MessageHandler(OnReceived);
			server.Sent += new MessageHandler(OnSent);
			server.Disconnected += new ConnectionHandler(OnDisconnected);
			server.Error += new ErrorHandler(OnError);

			IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 14);
			server.Listen(localEP, 500);

			Console.ReadLine();

			server.Close();

			Console.ReadLine();
		}

		static void OnConnected(ConnectionEventArgs e)
		{
			Console.WriteLine("client accepted");
		}
		static void OnDisconnected(ConnectionEventArgs e)
		{
			Console.WriteLine("client disconnected");
		}
		static void OnReceived(MessageEventArgs e)
		{
			string s = Encoding.UTF8.GetString(e.Buffer);
			Console.WriteLine("received: " + s);
			s = "RE: " + s;
			e.Connection.Send(Encoding.UTF8.GetBytes(s));
		}
		static void OnSent(MessageEventArgs e)
		{
			Console.WriteLine("sent to client: " + Encoding.UTF8.GetString(e.Buffer));
		}
		static void OnError(ErrorEventArgs e)
		{
			Console.WriteLine("error: " + e.Exception.Message);
		}

	}
}
