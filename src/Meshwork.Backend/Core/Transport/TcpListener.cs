//
// TcpListener.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Meshwork.Backend.Core.Transport
{
	public class TcpTransportListener : ITransportListener
	{
		int port;
		TcpListener listener;
		Thread listenThread;

	    private readonly Core core;

		public TcpTransportListener (Core core, int port)
		{
		    this.core = core;
			this.port = port;
		}

		public void StartListening ()
		{
			if (listener != null || listenThread != null)
				throw new InvalidOperationException("Already started");
			
			if (Common.Utils.SupportsIPv6) {
				listener = new TcpListener(IPAddress.IPv6Any, port);
			} else {
				listener = new TcpListener(IPAddress.Any, port);
			}

			listener.Start ();

			listenThread = new Thread(Listen);
			listenThread.Start();
		}

		public void StopListening ()
		{
			listener.Stop();
			if (listenThread != null) {
				listenThread.Abort ();
				listenThread = null;
			}
			if (listener != null) {
				listener.Stop();
				listener = null;
			}
		}

		public int Port {
			get {
				return port;
			}
			set {
				port = value;				
				if (Listening) {
					StopListening();
					StartListening();
				}
			}
		}
		
		public bool Listening {
			get {
				return (listener != null  || listenThread != null);
			}
		}
		
		private void Listen ()
		{
			try {
				while (true) {
					var socket = listener.AcceptSocket();
					try {
						ITransport transport = new TcpTransport(core, socket);
						LoggingService.LogInfo("New incoming transport: {0}.", transport.ToString());
						core.TransportManager.Add(transport);
						// TransportManager will take care of this 
						// connection now
					} catch (Exception ex) {
						LoggingService.LogError(ex.ToString());
					}
				}
			} catch (ThreadAbortException) {
				// Someone called StopListening(), that's OK...
			}  catch (Exception ex) {
				LoggingService.LogError("Error in TcpListener.Listen()", ex);
			}
		}

		public override string ToString ()
		{
			return $"TCP listener on port {port}";
		}
	}
}
