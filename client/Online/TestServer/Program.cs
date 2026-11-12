using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Xml;

using System.Collections.Generic;

using Common;
using AsyncSocket;


namespace TestServer
{

	class Member
	{
		public ISocketConnection connection = null;
		public int id = 0;
		public string login = "";
		public enum OnlineStatus
		{
			Connected = 1
			,Online = 2
		}
		public OnlineStatus status = OnlineStatus.Connected;


		public Member(int id, ISocketConnection connection)
		{
			this.id = id;
			this.connection = connection;
		}
	}

	class TestOnlineServer
	{
		private static int id_inc = 1;
		static void Main(string[] args)
		{
			TestOnlineServer server = new TestOnlineServer();
			server.Run();
		}

		private Server m_server = null;

		//private Dictionary<ISocketConnection, 
		private Dictionary<int, Member> m_members = new Dictionary<int, Member>();


		public void Run()
		{
			m_server = new Server();

			m_server.Connected += new ConnectionHandler(OnConnected);
			m_server.Received += new MessageHandler(OnReceived);
			m_server.Sent += new MessageHandler(OnSent);
			m_server.Disconnected += new ConnectionHandler(OnDisconnected);
			m_server.Error += new ErrorHandler(OnError);

			IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 14);
			m_server.Listen(localEP, 500);
			Console.ReadLine();
			m_server.Close();
		}

		private Member GetMember(ISocketConnection connection)
		{
			foreach (Member member in m_members.Values)
			{
				if (member.connection == connection)
					return member;
			}
			return null;
		}
		private void DisconnectMember(Member member)
		{
			m_members.Remove(member.id);
		}

		void OnConnected(ConnectionEventArgs e)
		{
			Console.WriteLine("client accepted");

			TestOnlineServer.id_inc++;
			m_members.Add(TestOnlineServer.id_inc, new Member(TestOnlineServer.id_inc, e.Connection));
		}
		void OnDisconnected(ConnectionEventArgs e)
		{
			Console.WriteLine("client disconnected");

			Member member = null;
			foreach (Member m in m_members.Values)
			{
				if (m.connection == e.Connection)
				{
					member = m;
					break;
				}
			}
			if (member != null)
			{
				m_members.Remove(member.id);

				foreach (Member m in m_members.Values)
				{
					if (m.status == Member.OnlineStatus.Online)
						m.connection.Send(
							Encoding.UTF8.GetBytes(
								String.Format("<online><logged-out id=\"{0}\" login=\"{1}\" /></online>", member.id, member.login)));
				}
			}
		}
		void OnReceived(MessageEventArgs e)
		{
			string s = Encoding.UTF8.GetString(e.Buffer);
			Console.WriteLine("received: " + s);

			//s = "RE: " + s;
			//e.Connection.Send(Encoding.UTF8.GetBytes(s));

			Member member = GetMember(e.Connection);
			if (member == null)
				return;

			string strPacket = Encoding.UTF8.GetString(e.Buffer);
			XmlDocument doc = new XmlDocument();
			try
			{
				doc.LoadXml(strPacket);

				if (!doc.ChildNodes[0].Name.Equals("online"))
					return;

				XmlNode node = doc.ChildNodes[0].ChildNodes[0];
				string cmd = node.Name;

				if (member.status == Member.OnlineStatus.Connected)
				{
					if (cmd == "auth")
					{
						// The expected test password may be set via the
						// P2P_TEST_PASSWORD environment variable; when it is
						// not set the test server accepts any password.
						string sTestPassword = Environment.GetEnvironmentVariable("P2P_TEST_PASSWORD");
						if (sTestPassword == null || node.Attributes["passwd"].Value == sTestPassword)
						{
							member.login = node.Attributes["login"].Value;
							member.status = Member.OnlineStatus.Online;

							member.connection.Send(Encoding.UTF8.GetBytes("<online><auth-ok id=\"" + member.id + "\" /></online>"));

							Console.WriteLine("AUTH-OK: " + member.id + ":" + member.login);

							StringBuilder sbUsers = new StringBuilder();
							foreach (Member m in m_members.Values)
							{
								if (m.status == Member.OnlineStatus.Online && m != member)
								{
									sbUsers.AppendFormat("<user id=\"{0}\" login=\"{1}\" />", m.id, m.login);
									m.connection.Send(
										Encoding.UTF8.GetBytes(
											String.Format("<online><logged-in id=\"{0}\" login=\"{1}\" /></online>", member.id, member.login)));
								}
							}
							member.connection.Send(Encoding.UTF8.GetBytes("<online><users>" + sbUsers.ToString() + "</users></online>"));

						} else
						{
							member.connection.Send(Encoding.UTF8.GetBytes("<online><auth-incorrect /></online>"));

							Console.WriteLine("AUTH-INCORRECT: " + node.Attributes["login"].Value + ":" + node.Attributes["passwd"].Value);

							DisconnectMember(member);
						}
					}
					return;
				}

				switch (cmd)
				{
					case "msg":
						string text = node.InnerText;

						//int fromUserID = member.id;

						string sTo = "";

						if (node.Attributes["to"] != null)
						{
							sTo = node.Attributes["to"].Value;
						}
						int iTo = Util.GetInt(sTo);
						if (iTo > 0)
							if (!m_members.ContainsKey(iTo))
								iTo = 0;

						Console.WriteLine("MSG: from:" + member.id + " to:" + sTo + " : " + node.InnerText);

						if (iTo > 0)
						{
							string sMsg = String.Format("<online><msg from=\"{0}\" to=\"{1}\"><![CDATA[{2}]]></msg></online>", member.id, iTo, text);
							m_members[iTo].connection.Send(Encoding.UTF8.GetBytes(sMsg));
						}
						else
						{
							string sMsg = String.Format("<online><msg from=\"{0}\"><![CDATA[{1}]]></msg></online>", member.id, text);
							foreach (Member m in m_members.Values)
							{
								if (m.status == Member.OnlineStatus.Online)
									m.connection.Send(Encoding.UTF8.GetBytes(sMsg));
							}
						}
						break;
				}
			}
			catch (XmlException)
			{
				// TODO ?
				return;
			}
			
		}
		void OnSent(MessageEventArgs e)
		{
			Console.WriteLine("sent to client: " + Encoding.UTF8.GetString(e.Buffer));
		}
		void OnError(ErrorEventArgs e)
		{
			Console.WriteLine("error: " + e.Exception.Message);
		}

	}
}
