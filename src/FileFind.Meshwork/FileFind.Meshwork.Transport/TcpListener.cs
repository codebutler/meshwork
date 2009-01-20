//
// TcpListener.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FileFind.Meshwork.Transport
{
	public class TcpTransportListener : ITransportListener
	{
		int port;
		TcpListener listener;
		Thread listenThread;
		
		public TcpTransportListener (int port)
		{
			this.port = port;
		}

		public void StartListening ()
		{
			if (Common.SupportsIPv6) {
				listener = new TcpListener(IPAddress.IPv6Any, port);
			} else {
				listener = new TcpListener(IPAddress.Any, port);
			}

			listener.Start ();

			listenThread = new Thread(new ThreadStart(Listen));
			listenThread.Start();
		}

		public void StopListening ()
		{
			if (listener != null) {
				listener.Stop();
				listenThread.Abort ();
				listenThread = null;
			}
		}

		public int Port {
			get {
				return port;
			}
			set {
				port = value;
				StopListening();
				StartListening();
			}
		}
		
		private void Listen ()
		{
			try {
				while (true) {
					Socket socket = listener.AcceptSocket();
					try {
						ITransport transport = new TcpTransport(socket);
						LogManager.Current.WriteToLog(String.Format("New incoming transport: {0}.", transport.ToString()));
						Core.TransportManager.Add(transport);
						// TransportManager will take care of this 
						// connection now
					} catch (Exception ex) {
						LogManager.Current.WriteToLog(ex.ToString());
					}
				}
			} catch (Exception) {
				// Someone called StopListening(), that's OK...
				// (There's PROBABLY nothing else that can go wrong here.)
			}
		}

		public override string ToString ()
		{
			return String.Format("TCP listener on port {0}", port);
		}
	}
}
