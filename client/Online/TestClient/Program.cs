using System;
using System.Collections.Generic;
using System.Text;

using System.Net;

using Common;
using Online;

namespace TestClient
{
	class TestOnlineClient
	{
		static void Main(string[] args)
		{
			if (args.Length < 4)
			{
				Console.WriteLine("USAGE: TestClient.exe <server IP> <port> <login> <password>");
				return;
			}

			// login and password
			OnlineClient client = new OnlineClient();

			// IP and port of the server to connect
			IPAddress IPAddr = IPAddress.Parse(args[0]);
			//IPAddress IPAddr = IPAddress.Parse("192.168.40.2");

			client.ServerIPEndPoint = new IPEndPoint(IPAddr, Convert.ToInt32(args[1]));
			//client.ServerIPEndPoint = new IPEndPoint(IPAddr, 6667);
			//client.ServerIPEndPoint = new IPEndPoint(IPAddress.Loopback, 6667);

			// login and password
			client.Login = args[2];
			client.Password = args[3];
			//client.Login = "Алесандро";
			//client.Password = "12345";

			// events handlers
			client.Msg += new MsgHandler(client_Msg);
			client.Connected += new EventHandler(client_Connected);
			client.Disconnected += new EventHandler(client_Disconnected);
			client.AuthOk += new MemberHandler(client_AuthOk);
			client.AuthIncorrect += new EventHandler(client_AuthIncorrect);
			client.MembersAdded += new MembersHandler(client_MembersAdded);
			client.MemberLoggedIn += new MemberHandler(client_MemberLoggedIn);
			client.MemberLoggedOut += new MemberHandler(client_MemberLoggedOut);
			
			// connect
			client.Connect();

			// forever loop. to exit enter 'exit'.
			// to send to all just enter the message
			// to send to specified member: #{id} {message}
			string s = "";
			do
			{
				if (s != "" && client.Status == OnlineClient.OnlineStatus.Online)
				{
					if (s[0] == '#')
					{
						int i = s.IndexOf(' ');
						if (i > 0)
						{
							int iTo = Util.GetInt(s.Substring(1, i-1));
							client.SendTo(s, iTo);
						}
					}
					else
					{
						client.SendAll(s);
					}
				}
				s = Console.ReadLine();
			} while (s != "exit");

			client.Disconnect();
		}


		#region events handlers

		static void client_MemberLoggedOut(MemberEventArgs e)
		{
			Console.WriteLine(String.Format("Member leaves: {0}: {1}", e.Member.id, e.Member.login));
		}

		static void client_MemberLoggedIn(MemberEventArgs e)
		{
			Console.WriteLine(String.Format("New member: {0}: {1}", e.Member.id, e.Member.login));
		}

		static void client_MembersAdded(MembersEventArgs e)
		{
			Console.WriteLine(String.Format("Members already online: {0}", e.Members.Length));
			for (int i = 0; i < e.Members.Length; i++ )
			{
				Console.WriteLine(String.Format("\t{0}: {1}", e.Members[i].id, e.Members[i].login));
			}
		}

		static void client_AuthIncorrect(object sender, EventArgs e)
		{
			Console.WriteLine("Incorrect Login/Password");
		}

		static void client_AuthOk(MemberEventArgs e)
		{
			Console.WriteLine("Logged in successfully as " + e.Member.id + " : " + e.Member.login);
		}

		static void client_Disconnected(object sender, EventArgs e)
		{
			Console.WriteLine("Disconnected");
		}

		static void client_Connected(object sender, EventArgs e)
		{
			Console.WriteLine("Connected");
		}

		static void client_Msg(MsgEventArgs e)
		{
			Console.WriteLine(String.Format("{0}: {1}", e.Member.login, e.Text));
		}

		#endregion


	}
}
