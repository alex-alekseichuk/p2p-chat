using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;

using Common;
using AsyncSocket;

namespace Online
{
	public class Member
	{
		protected readonly int m_id = 0;
		public int id {
			get { return m_id; }
		}
		protected readonly string m_login = "";
		public string login
		{
			get { return m_login; }
		}

		public Member(int id, string login)
		{
			this.m_id = id;
			this.m_login = login;
		}
	}

	public class MsgEventArgs : EventArgs
	{
		private readonly string m_text;
		public string Text
		{
			get { return m_text; }
		}
		private readonly Member m_member;
		public Member Member
		{
			get { return m_member; }
		}
		private readonly bool m_bPrivate;
		public bool Private
		{
			get { return m_bPrivate; }
		}
		public MsgEventArgs(string text, Member member, bool bPrivate)
		{
			m_text = text;
			m_member = member;
			m_bPrivate = bPrivate;
		}
	}
	public class MemberEventArgs : EventArgs
	{
		private readonly Member m_member;
		public Member Member
		{
			get { return m_member; }
		}
		public MemberEventArgs(Member member)
		{
			m_member = member;
		}
	}
	public class MembersEventArgs : EventArgs
	{
		private readonly Member[] m_members;
		public Member[] Members
		{
			get { return m_members; }
		}
		public MembersEventArgs(Member[] members)
		{
			m_members = members;
		}
	}

	public delegate void MsgHandler(MsgEventArgs e);
	public delegate void MemberHandler(MemberEventArgs e);
	public delegate void MembersHandler(MembersEventArgs e);


	public class OnlineClient : BaseDisposable
	{
		private Client m_client = null;

		public enum OnlineStatus
		{
			Offline = 0
			,Connected = 1
			,Online = 2
		}
		private OnlineStatus m_status = OnlineStatus.Offline;
		public OnlineStatus Status
		{
			get { return m_status; }
		}

		private IPEndPoint m_epServer = null;
		public IPEndPoint ServerIPEndPoint
		{
			set { m_epServer = value; }
		}
		private string m_login = "";
		public string Login
		{
			set { m_login = value; }
		}
		private string m_passwd = "";
		public string Password
		{
			set { m_passwd = value; }
		}

		private Member m_member = null;


		private Dictionary<int, Member> m_members = new Dictionary<int, Member>();


		public OnlineClient(IPEndPoint epServer, string login, string passwd)
		{
			ServerIPEndPoint = epServer;
			Login = login;
			Password = passwd;
			Init();
		}
		public OnlineClient()
		{
			Init();
		}

		private void Init()
		{
			if (m_client != null)
				return;
			m_client = new Client();
			m_client.Connected += new ConnectionHandler(OnConnected);
			m_client.Received += new MessageHandler(OnReceived);
			m_client.Sent += new MessageHandler(OnSent);
			m_client.Disconnected += new ConnectionHandler(OnDisconnected);
			m_client.Error += new ErrorHandler(OnError);
		}


		#region Destructor

		protected override void Free(bool canAccessFinalizable)
		{
			if (m_client != null)
			{
				m_client.Disconnect();
				m_client.Dispose();
				m_client = null;
			}

			base.Free(canAccessFinalizable);
		}

		#endregion


		protected void SendMsg(string sMsg)
		{
			m_client.Send(Encoding.UTF8.GetBytes("<online>" + sMsg + "</online>"));
		}
		public void Connect()
		{
			m_client.Connect(m_epServer);
		}
		public void Connect(IPEndPoint epServer)
		{
			m_epServer = epServer;
			m_client.Connect(m_epServer);
		}
		public void Disconnect()
		{
			m_client.Disconnect();
		}
		public void SendAll(string text)
		{
			if (Status != OnlineStatus.Online)
				return;
			string s = String.Format("<msg><![CDATA[{0}]]></msg>", text);
			//m_client.Send(Encoding.UTF8.GetBytes(s));
			SendMsg(s);
		}
		public void SendTo(string text, int to)
		{
			if (Status != OnlineStatus.Online)
				return;
			if (m_member.id == to || (! m_members.ContainsKey(to)))
				return;
			string s = String.Format("<msg to=\"{0}\"><![CDATA[{1}]]></msg>", to, text);
			//m_client.Send(Encoding.UTF8.GetBytes(s));
			SendMsg(s);
		}

		public event MsgHandler Msg;

		public event MembersHandler MembersAdded;
		public event MemberHandler MemberLoggedIn;
		public event MemberHandler MemberLoggedOut;

		public event EventHandler Connected;
		public event MemberHandler AuthOk;
		public event EventHandler AuthIncorrect;
		public event EventHandler Disconnected;



		#region IClientService implementation

		protected void OnConnected(ConnectionEventArgs e)
		{
			m_status = OnlineStatus.Connected;
			Connected(this, null);
			string s = String.Format("<auth login=\"{0}\" passwd=\"{1}\" />", m_login, m_passwd);
			//m_client.Send(Encoding.UTF8.GetBytes(s));
			SendMsg(s);
		}
		protected void OnDisconnected(ConnectionEventArgs e)
		{
			m_status = OnlineStatus.Offline;
			Disconnected(this, null);
		}
		protected void OnReceived(MessageEventArgs e)
		{
			if (Status == OnlineStatus.Offline)
				return;

			string strPacket = Encoding.UTF8.GetString(e.Buffer);

			//Console.WriteLine("received: " + s);
			//s = "RE: " + s;
			//e.Connection.Send(Encoding.UTF8.GetBytes(s));


			XmlDocument doc = new XmlDocument();

			// load xml
			try
			{
				doc.LoadXml(strPacket);

				if (!doc.ChildNodes[0].Name.Equals("online"))
					return;

				XmlNode node = doc.ChildNodes[0].ChildNodes[0];
				string cmd = node.Name;

				//SqlConnection conn = server.getDBConn();
				//String name = node.Name;

				/*
				if (Status == OnlineStatus.Connected)
				{
					switch (cmd)
					{
					}
					return;
				}
				*/

			switch (cmd)
				{
					case "auth-ok":
						if (Status == OnlineStatus.Connected)
						{
							int id = Util.GetInt(node.Attributes["id"].Value);
							m_member = new Member(id, m_login);
							m_status = OnlineStatus.Online;
							AuthOk(new MemberEventArgs(m_member));
						}
					break;
					case "auth-incorrect":
						if (Status == OnlineStatus.Connected)
						{
							AuthIncorrect(this, null);
							Disconnect();
						}
						break;
					case "msg":
						string text = node.InnerText;
						string sFromUserId = node.Attributes["from"].Value;
						int fromUserID = Util.GetInt(sFromUserId);

						bool bPrivate = false;
						if (node.Attributes["to"] != null)
							if (Util.GetInt(node.Attributes["to"].Value) == m_member.id)
								bPrivate = true;
						if (m_members.ContainsKey(fromUserID))
							Msg(new MsgEventArgs(text, m_members[fromUserID], bPrivate));
						if (m_member.id == fromUserID)
							Msg(new MsgEventArgs(text, m_member, bPrivate));
						break;
					case "users":
						List<Member> members = new List<Member>();
						foreach (XmlNode nodeUser in node.ChildNodes)
						{
							int id = 0;
							string login = "";
							if (nodeUser.Attributes["id"] != null)
								id = Util.GetInt(nodeUser.Attributes["id"].Value);
							if (nodeUser.Attributes["login"] != null)
								login = nodeUser.Attributes["login"].Value;
							if (id > 0 && id != m_member.id && login != "")
							{
								if (! m_members.ContainsKey(id))
								{
									Member member = new Member(id, login);
									m_members.Add(id, member);
									members.Add(member);
								}
							}
						}
						if (members.Count > 0)
							MembersAdded(new MembersEventArgs(members.ToArray()));
						break;
					case "logged-in":
						{
							int id = 0;
							string login = "";
							if (node.Attributes["id"] != null)
								id = Util.GetInt(node.Attributes["id"].Value);
							if (node.Attributes["login"] != null)
								login = node.Attributes["login"].Value;

							int memberId = 0;
							if (m_member != null)
								memberId = m_member.id;

							if (id > 0 && id != memberId && login != "")
							{
								Member member = new Member(id, login);
								m_members.Add(id, member);
								MemberLoggedIn(new MemberEventArgs(member));
							}
						}
						break;
					case "logged-out":
						{
							int id = 0;
							if (node.Attributes["id"] != null)
								id = Util.GetInt(node.Attributes["id"].Value);
							if (id > 0 && id != m_member.id)
							{
								if (m_members.ContainsKey(id))
								{
									Member member = m_members[id];
									m_members.Remove(id);
									MemberLoggedOut(new MemberEventArgs(member));
								}
							}
						}
						break;
				}
			}
			catch (XmlException)
			{
				// TODO ?
				//Console.WriteLine("xml error");
				return;
			}
		}
		protected void OnSent(MessageEventArgs e)
		{
			//Console.WriteLine("sent to server: " + Encoding.UTF8.GetString(e.Buffer));
		}
		protected void OnError(ErrorEventArgs e)
		{
			//Console.WriteLine("error: " + e.Exception.Message);
		}

		#endregion



	}
}
