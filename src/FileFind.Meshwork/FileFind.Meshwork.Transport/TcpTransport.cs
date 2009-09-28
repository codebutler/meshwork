//
// TcpTransport.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Net.Sockets;

namespace FileFind.Meshwork.Transport
{
	public class TcpTransport : TransportBase
	{	                                              
		public static readonly int DefaultPort = 7332;

		Socket            socket           = null;
		IPAddress         address          = IPAddress.Any;
		int               port             = 0;
		TransportCallback connectCallback  = null;

                object sendLock = new object();
                object receiveLock = new object();

		internal TcpTransport (Socket socket)
		{
			this.socket = socket;
			address = (socket.RemoteEndPoint as IPEndPoint).Address;
			port = (socket.RemoteEndPoint as IPEndPoint).Port;
			base.incoming = true;
			base.transportState = TransportState.Connected;
			base.RaiseConnected();
		}

		public TcpTransport (IPAddress address, int port, ulong connectionType)
		{
			this.address = address;
			this.port = port;
			base.connectionType = connectionType;
			base.incoming = false;
			base.transportState = TransportState.Waiting;
		}

		public override void Connect (TransportCallback callback)
		{
			if (socket != null)
				throw new InvalidOperationException ("This socket is already connected.");

			if (address.Equals (IPAddress.Any) || address.Equals (IPAddress.None) || port == 0)
				throw new Exception ("Invalid IP Address/Port");
			
			base.transportState = TransportState.Connecting;

			connectCallback = callback;
			
			if (address.IsIPv6LinkLocal) {
				address.ScopeId = Core.Settings.IPv6LinkLocalInterfaceIndex;
			}

			IPEndPoint remoteEndpoint = new IPEndPoint (address, port); 
			socket = new Socket (address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.BeginConnect (remoteEndpoint, new AsyncCallback (OnConnected), null);
		}
		
		public override int Send (byte[] buffer, int offset, int size)
		{
			lock (sendLock) {
				int totalSent = 0;
				while (totalSent < size) {
					int sent = socket.Send(buffer, offset + totalSent, size - totalSent, SocketFlags.None);
					if (sent == 0) {
						throw new Exception("No data was sent.");
					}
					totalSent += sent;
				}
				if (totalSent > size) {
					throw new Exception("Sent too much! " + totalSent + " " + size);
				}
				return totalSent;
			}
		}

		public override int Receive (byte[] buffer, int offset, int size)
		{
			if (size <= 0) {
				throw new ArgumentException("Cannot receive <= 0 bytes");
			}

			int totalReceived = 0;
			while (totalReceived < size) {
				int count = 0;
			    	if (socket == null) {
					// We were disconnected!
					return 0;
			    	}
                                lock (receiveLock) {
					count = socket.Receive(buffer, offset + totalReceived, size - totalReceived, SocketFlags.None);
                                }
				if (count == 0) {
					// This means the connection was closed.
					Disconnect();
					return 0;
				} else {
					totalReceived += count;

					if (totalReceived > size) {
						throw new Exception("Somehow received too much! This shouldn't ever happen!");
					}
				}
			}
			return totalReceived;
		}
		
		public override EndPoint RemoteEndPoint {
			get {
				if (socket != null && socket.Connected) {
					try {
						return (EndPoint)socket.RemoteEndPoint;
					} catch (SocketException ex) {
						LoggingService.LogError("Failed to get remote end point. I am pretty sure this is a bug in mono!", ex);
						return new IPEndPoint(address, port);
					}
				} else {
					return new IPEndPoint(address, port);
				}
			}
		}
		
		public override string ToString () 
		{
			if (socket != null) {
				//return String.Format("Local: {0}   Remote: {1}", socket.LocalEndPoint, socket.RemoteEndPoint);
				if (Incoming == true) {
					return String.Format("TCP/INCOMING/{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, port);
				} else {
					return String.Format("TCP/OUTGOING/{0}:{1}", (socket.RemoteEndPoint as IPEndPoint).Address, port);
				}
			} else {
				if (Incoming == true) {
					return String.Format("TCP/INCOMING/{0}:{1}", address, port);
				} else {
					return String.Format("TCP/OUTGOING/{0}:{1}", address, port);
				}
			}
		}

		public override void Disconnect ()
		{
			Disconnect (null);
		}
		
		public override void Disconnect (Exception ex)
		{
			if (base.transportState != TransportState.Disconnected) {
				base.transportState = TransportState.Disconnected;

				if (socket != null) {
					socket.Close ();
					socket = null;
				}
				
				if (ex != null)
					if (ex is SocketException)
						LoggingService.LogInfo("Transport {0} disconnected ({1}).", this.ToString(), ex.Message);
					else
						LoggingService.LogInfo("Transport {0} disconnected with error: {1}", this.ToString(), ex.ToString());
				else
					LoggingService.LogInfo("Transport {0} disconnected", this.ToString());

				base.RaiseDisconnected(ex);
			}
		}

		private void OnConnected (IAsyncResult result) 
		{
			try {
				socket.EndConnect (result);
				base.transportState = TransportState.Connected;
				base.RaiseConnected();
				connectCallback (this);
			} catch (Exception ex) {
				Disconnect (ex);
			}
		}
	}
}
